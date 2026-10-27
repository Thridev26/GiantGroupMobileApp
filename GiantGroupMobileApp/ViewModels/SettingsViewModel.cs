using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupMobileApp.Services;

namespace GiantGroupMobileApp.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISessionService _session;
    private readonly ISettingsService _settings;
    private readonly IApiClient _api;
    private readonly IPhotoQueueService _queue;
    private readonly IDialogService _dialogs;
    private bool _loading;

    public SettingsViewModel(ISessionService session, ISettingsService settings, IApiClient api, IPhotoQueueService queue, IDialogService dialogs)
    {
        _session = session;
        _settings = settings;
        _api = api;
        _queue = queue;
        _dialogs = dialogs;
        _queue.Changed += () => OnPropertyChanged(nameof(QueueText));
    }

    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _roleText = "";
    [ObservableProperty] private string _serverText = "";
    [ObservableProperty] private bool _wifiOnly;
    [ObservableProperty] private bool _demoMode;

    public string QueueText => _queue.OutstandingCount == 0 ? "Nothing waiting" : $"{_queue.OutstandingCount} waiting";
    public string VersionText => $"Version {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    public void OnAppearing()
    {
        _loading = true;
        UserName = _session.Current?.Name ?? "";
        RoleText = _session.Current?.RoleLabel ?? "";
        ServerText = _settings.DemoMode ? "Demo mode (sample data)" : _settings.ServerUrl;
        WifiOnly = _settings.WifiOnlyUploads;
        DemoMode = _settings.DemoMode;
        _loading = false;
        OnPropertyChanged(nameof(QueueText));
    }

    partial void OnWifiOnlyChanged(bool value)
    {
        if (_loading) return;
        _settings.WifiOnlyUploads = value;
        if (!value) _ = _queue.ProcessAsync();
    }

    partial void OnDemoModeChanged(bool value)
    {
        if (_loading) return;
        _ = SwitchDemoAsync(value);
    }

    private async Task SwitchDemoAsync(bool value)
    {
        var ok = await _dialogs.ConfirmAsync(value ? "Switch to demo mode?" : "Leave demo mode?",
            "You'll be signed out and returned to the sign-in screen.", "Continue");
        if (!ok)
        {
            _loading = true; DemoMode = !value; _loading = false;
            return;
        }
        _settings.DemoMode = value;
        await _session.SignOutAsync();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private Task OpenQueueAsync() => Shell.Current.GoToAsync("queue");

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await RunAsync(async () =>
        {
            var me = await _api.GetMeAsync();
            ShowBanner($"Connected – signed in as {me.Name}");
        });
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        var pending = _queue.OutstandingCount;
        var msg = pending > 0
            ? $"{pending} photo{(pending == 1 ? " is" : "s are")} still waiting to upload. They stay on this phone and upload after the next sign-in."
            : "You'll need to sign in again to use the app.";
        if (!await _dialogs.ConfirmAsync("Sign out?", msg, "Sign out")) return;
        await _session.SignOutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
