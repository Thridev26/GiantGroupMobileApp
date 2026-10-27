using GiantGroupMobileApp.Models;

namespace GiantGroupMobileApp.Services;

/// <summary>Registered as IApiClient: routes each call to the live server or the built-in demo data, depending on Settings.</summary>
public class ApiClientRouter : IApiClient
{
    private readonly ApiClient _live;
    private readonly DemoApiClient _demo;
    private readonly ISettingsService _settings;

    public ApiClientRouter(ApiClient live, DemoApiClient demo, ISettingsService settings)
    {
        _live = live; _demo = demo; _settings = settings;
    }

    private IApiClient C => _settings.DemoMode ? _demo : _live;

    public Task<LoginResult> LoginAsync(string u, string p, CancellationToken ct = default) => C.LoginAsync(u, p, ct);
    public Task<UserSession> VerifyOtpAsync(string t, string c, CancellationToken ct = default) => C.VerifyOtpAsync(t, c, ct);
    public Task<UserSession> GetMeAsync(CancellationToken ct = default) => C.GetMeAsync(ct);
    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default) => C.GetDashboardAsync(ct);
    public Task<List<JobRequestSummary>> GetRequestsAsync(string? s, string? q, CancellationToken ct = default) => C.GetRequestsAsync(s, q, ct);
    public Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default) => C.GetRequestAsync(id, ct);
    public Task<JobRequestDetail> SetRequestStatusAsync(int id, string s, CancellationToken ct = default) => C.SetRequestStatusAsync(id, s, ct);
    public Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> v, CancellationToken ct = default) => C.SaveDetailsAsync(id, v, ct);
    public Task<List<JobSummary>> GetJobsAsync(string? s, string? q, CancellationToken ct = default) => C.GetJobsAsync(s, q, ct);
    public Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default) => C.GetJobAsync(id, ct);
    public Task<JobDetail> SetJobStatusAsync(int id, string s, decimal? f, decimal? d, CancellationToken ct = default) => C.SetJobStatusAsync(id, s, f, d, ct);
    public Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget t, CancellationToken ct = default) => C.GetPhotosAsync(t, ct);
    public Task<SitePhoto> UploadPhotoAsync(PhotoTarget t, string type, string path, CancellationToken ct = default) => C.UploadPhotoAsync(t, type, path, ct);
    public Task<SitePhoto> UpdatePhotoTypeAsync(int id, string type, CancellationToken ct = default) => C.UpdatePhotoTypeAsync(id, type, ct);
    public Task DeletePhotoAsync(int id, CancellationToken ct = default) => C.DeletePhotoAsync(id, ct);
    public Task<byte[]?> DownloadPhotoAsync(int id, int? w, CancellationToken ct = default) => C.DownloadPhotoAsync(id, w, ct);
}
