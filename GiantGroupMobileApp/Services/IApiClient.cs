using GiantGroupMobileApp.Models;

namespace GiantGroupMobileApp.Services;

public interface IApiClient
{
    Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<UserSession> VerifyOtpAsync(string challengeToken, string code, CancellationToken ct = default);
    Task<UserSession> GetMeAsync(CancellationToken ct = default);

    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);

    Task<List<JobRequestSummary>> GetRequestsAsync(string? status, string? search, CancellationToken ct = default);
    Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default);
    Task<JobRequestDetail> SetRequestStatusAsync(int id, string status, CancellationToken ct = default);
    Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> values, CancellationToken ct = default);

    Task<List<JobSummary>> GetJobsAsync(string? status, string? search, CancellationToken ct = default);
    Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default);
    Task<JobDetail> SetJobStatusAsync(int id, string status, decimal? fuelCost, decimal? dumpingCost, CancellationToken ct = default);

    Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget target, CancellationToken ct = default);
    Task<SitePhoto> UploadPhotoAsync(PhotoTarget target, string photoType, string filePath, CancellationToken ct = default);
    Task<SitePhoto> UpdatePhotoTypeAsync(int photoId, string photoType, CancellationToken ct = default);
    Task DeletePhotoAsync(int photoId, CancellationToken ct = default);
    /// <summary>Returns null when the server (or demo mode) has no image bytes for this photo.</summary>
    Task<byte[]?> DownloadPhotoAsync(int photoId, int? width, CancellationToken ct = default);
}
