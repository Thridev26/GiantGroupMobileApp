using GiantGroupMobileApp.Services;

namespace GiantGroupMobileApp.ViewModels;

public partial class StartupViewModel : BaseViewModel
{
    private readonly ISessionService _session;
    private readonly IApiClient _api;
    private readonly IPhotoQueueService _queue;
    private readonly ISettingsService _settings;

    public StartupViewModel(ISessionService session, IApiClient api, IPhotoQueueService queue, ISettingsService settings)
    {
        _session = session;
        _api = api;
        _queue = queue;
        _settings = settings;
    }

    /// <summary>Decides where to go on launch: dashboard if a valid session exists, otherwise the login page.</summary>
    public async Task RouteAsync()
    {
        var restored = await _session.RestoreAsync();
        if (restored != null)
        {
            try
            {
                // Refresh role + allowed statuses; also proves the token is still accepted by the server.
                var fresh = await _api.GetMeAsync();
                await _session.SignInAsync(fresh);
            }
            catch (ApiException ex) when (ex.IsAuth)
            {
                await Shell.Current.GoToAsync("//login");
                return;
            }
            catch (ApiException)
            {
                // Offline at launch: stay signed in with the cached session so photos can still be captured.
            }

            await _queue.InitializeAsync();
            await Shell.Current.GoToAsync("//dashboard");
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
