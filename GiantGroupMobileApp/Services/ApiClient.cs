using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GiantGroupMobileApp.Models;

namespace GiantGroupMobileApp.Services;

/// <summary>Talks to MobileApi.ashx on the Giant Group website.</summary>
public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly ISessionService _session;

    public ApiClient(HttpClient http, ISettingsService settings, ISessionService session)
    {
        _http = http;
        _settings = settings;
        _session = session;
    }

    // ---- auth
    public Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default) =>
        SendAsync<LoginResult>(HttpMethod.Post, "auth/login", new { username, password }, auth: false, ct: ct);

    public Task<UserSession> VerifyOtpAsync(string challengeToken, string code, CancellationToken ct = default) =>
        SendAsync<UserSession>(HttpMethod.Post, "auth/verify", new { challengeToken, code }, auth: false, ct: ct);

    public Task<UserSession> GetMeAsync(CancellationToken ct = default) =>
        SendAsync<UserSession>(HttpMethod.Get, "auth/me", ct: ct);

    // ---- dashboard
    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default) =>
        SendAsync<DashboardDto>(HttpMethod.Get, "dashboard", ct: ct);

    // ---- requests
    public Task<List<JobRequestSummary>> GetRequestsAsync(string? status, string? search, CancellationToken ct = default) =>
        SendAsync<List<JobRequestSummary>>(HttpMethod.Get, "requests" + Query(("status", status), ("search", search)), ct: ct);

    public Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default) =>
        SendAsync<JobRequestDetail>(HttpMethod.Get, $"requests/{id}", ct: ct);

    public Task<JobRequestDetail> SetRequestStatusAsync(int id, string status, CancellationToken ct = default) =>
        SendAsync<JobRequestDetail>(HttpMethod.Post, $"requests/{id}/status", new { status }, retry: false, ct: ct);

    public Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> values, CancellationToken ct = default) =>
        SendAsync<JobRequestDetail>(HttpMethod.Post, $"requests/{id}/details", new { values }, retry: false, ct: ct);

    // ---- jobs
    public Task<List<JobSummary>> GetJobsAsync(string? status, string? search, CancellationToken ct = default) =>
        SendAsync<List<JobSummary>>(HttpMethod.Get, "jobs" + Query(("status", status), ("search", search)), ct: ct);

    public Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default) =>
        SendAsync<JobDetail>(HttpMethod.Get, $"jobs/{id}", ct: ct);

    public Task<JobDetail> SetJobStatusAsync(int id, string status, decimal? fuelCost, decimal? dumpingCost, CancellationToken ct = default) =>
        SendAsync<JobDetail>(HttpMethod.Post, $"jobs/{id}/status", new { status, fuelCost, dumpingCost }, retry: false, ct: ct);

    // ---- photos
    public Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget target, CancellationToken ct = default) =>
        SendAsync<List<SitePhoto>>(HttpMethod.Get,
            "photos?" + (target.Kind == PhotoTargetKind.Job ? "jobId=" : "requestId=") + target.Id, ct: ct);

    public async Task<SitePhoto> UploadPhotoAsync(PhotoTarget target, string photoType, string filePath, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var file = new StreamContent(fs);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(photoType), "photoType");
        content.Add(new StringContent(target.Id.ToString()), target.Kind == PhotoTargetKind.Job ? "jobID" : "jobRequestID");

        using var req = Build(HttpMethod.Post, "photos", true);
        req.Content = content;
        return await ExecuteAsync<SitePhoto>(req, ct, TimeSpan.FromMinutes(3));
    }

    public Task<SitePhoto> UpdatePhotoTypeAsync(int photoId, string photoType, CancellationToken ct = default) =>
        SendAsync<SitePhoto>(HttpMethod.Put, $"photos/{photoId}", new { photoType }, retry: false, ct: ct);

    public async Task DeletePhotoAsync(int photoId, CancellationToken ct = default) =>
        await SendAsync<JsonElement>(HttpMethod.Delete, $"photos/{photoId}", retry: false, ct: ct);

    public async Task<byte[]?> DownloadPhotoAsync(int photoId, int? width, CancellationToken ct = default)
    {
        try
        {
            using var req = Build(HttpMethod.Get, $"photos/{photoId}/file" + (width.HasValue ? $"?w={width}" : ""), true);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DefaultTimeout);
            using var resp = await _http.SendAsync(req, cts.Token);
            if (resp.StatusCode == HttpStatusCode.Unauthorized) { _session.NotifyExpired(); throw new ApiException("Your session has expired. Please sign in again.", 401); }
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException) { return null; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return null; }
    }

    // ---- plumbing
    private static string Query(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs.Where(p => !string.IsNullOrWhiteSpace(p.Value))
                         .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!.Trim())}").ToList();
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private HttpRequestMessage Build(HttpMethod method, string path, bool auth)
    {
        var baseUrl = _settings.ServerUrl.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            throw new ApiException("The server address is not valid. Check it in Settings.");
        var req = new HttpRequestMessage(method, $"{baseUrl}/{path}");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (auth && _session.Token is { } t) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
        return req;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, bool auth = true, bool retry = true, CancellationToken ct = default)
    {
        // Only idempotent reads are retried automatically; writes surface the error so nothing is applied twice.
        var attempts = retry && method == HttpMethod.Get ? 3 : 1;
        for (var i = 1; ; i++)
        {
            try
            {
                using var req = Build(method, path, auth);
                if (body != null) req.Content = JsonContent.Create(body, options: Json);
                return await ExecuteAsync<T>(req, ct);
            }
            catch (ApiException ex) when (i < attempts && ex.IsTransient && !ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400 * i * i), ct);
            }
        }
    }

    private async Task<T> ExecuteAsync<T>(HttpRequestMessage req, CancellationToken ct, TimeSpan? timeout = null)
    {
        HttpResponseMessage resp;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout ?? DefaultTimeout);
        try
        {
            resp = await _http.SendAsync(req, cts.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("The server took too long to respond. Check your signal and try again.", 408, true, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("Can't reach the server. Check your internet connection.", 0, true, ex);
        }

        using (resp)
        {
            if (resp.IsSuccessStatusCode)
            {
                try
                {
                    var result = await resp.Content.ReadFromJsonAsync<T>(Json, ct);
                    return result ?? throw new ApiException("The server returned an empty response.", (int)resp.StatusCode);
                }
                catch (JsonException ex)
                {
                    throw new ApiException("The server sent a response the app could not read. Is the server URL pointing at MobileApi.ashx?", (int)resp.StatusCode, false, ex);
                }
            }

            string message = "Something went wrong (" + (int)resp.StatusCode + ").";
            try
            {
                var err = await resp.Content.ReadFromJsonAsync<ErrorBody>(Json, ct);
                if (!string.IsNullOrWhiteSpace(err?.Error)) message = err!.Error!;
            }
            catch (Exception) { /* non-JSON error page (e.g. IIS) – keep generic message */ }

            // A 401 on a signed-in call means the token expired/was revoked; login failures are 401 too but carry no token.
            if (resp.StatusCode == HttpStatusCode.Unauthorized && req.Headers.Authorization != null)
            {
                _session.NotifyExpired();
                message = "Your session has expired. Please sign in again.";
            }
            throw new ApiException(message, (int)resp.StatusCode);
        }
    }

    private sealed class ErrorBody { public string? Error { get; set; } }
}
