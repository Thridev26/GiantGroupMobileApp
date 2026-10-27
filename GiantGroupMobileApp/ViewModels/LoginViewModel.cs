using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupMobileApp.Services;

namespace GiantGroupMobileApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private readonly ISessionService _session;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly IPhotoQueueService _queue;
    private string? _challengeToken;

    public LoginViewModel(IApiClient api, ISessionService session, ISettingsService settings, IDialogService dialogs, IPhotoQueueService queue)
    {
        _api = api;
        _session = session;
        _settings = settings;
        _dialogs = dialogs;
        _queue = queue;
        Username = settings.LastUsername;
        RefreshServerLabel();
    }

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _otpCode = "";
    [ObservableProperty] private string _maskedEmail = "";
    [ObservableProperty] private string _serverLabel = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCredentialsStage))]
    private bool _isOtpStage;

    public bool IsCredentialsStage => !IsOtpStage;

    private void RefreshServerLabel() =>
        ServerLabel = _settings.DemoMode ? "Demo mode – sample data, nothing is sent to a server" : _settings.ServerUrl;

    [RelayCommand]
    private async Task SignInAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Enter your username (or email) and password.";
            return;
        }

        await RunAsync(async () =>
        {
            var result = await _api.LoginAsync(Username.Trim(), Password);
            _settings.LastUsername = Username.Trim();
            if (result.MfaRequired && !string.IsNullOrEmpty(result.ChallengeToken))
            {
                _challengeToken = result.ChallengeToken;
                MaskedEmail = result.MaskedEmail ?? "your email";
                OtpCode = "";
                IsOtpStage = true;
            }
            else if (result.Session != null)
            {
                await CompleteAsync(result.Session);
            }
            else
            {
                ErrorMessage = "Unexpected response from the server.";
            }
        }, asLoad: true);
    }

    [RelayCommand]
    private async Task VerifyAsync()
    {
        ErrorMessage = null;
        var code = (OtpCode ?? "").Trim();
        if (code.Length != 6 || !code.All(char.IsDigit))
        {
            ErrorMessage = "Enter the 6-digit code from your email.";
            return;
        }

        await RunAsync(async () =>
        {
            var session = await _api.VerifyOtpAsync(_challengeToken ?? "", code);
            await CompleteAsync(session);
        }, asLoad: true);
    }

    [RelayCommand]
    private void Back()
    {
        IsOtpStage = false;
        OtpCode = "";
        Password = "";
        ErrorMessage = null;
        _challengeToken = null;
    }

    [RelayCommand]
    private async Task EditServerAsync()
    {
        var choice = await _dialogs.ChooseAsync("Connection", new[]
        {
            "Change server address",
            _settings.DemoMode ? "Turn demo mode off" : "Try demo mode (sample data)",
            "Reset to default server"
        });
        switch (choice)
        {
            case "Change server address":
                var url = await _dialogs.PromptAsync("Server address", "Full address of MobileApi.ashx", _settings.ServerUrl, "https://…/MobileApi.ashx", Keyboard.Url);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u) || (u.Scheme != "https" && u.Scheme != "http"))
                    {
                        await _dialogs.AlertAsync("Invalid address", "Enter a full web address starting with https://");
                        break;
                    }
                    _settings.ServerUrl = url;
                    _settings.DemoMode = false;
                }
                break;
            case "Reset to default server":
                _settings.ResetServerUrl();
                _settings.DemoMode = false;
                break;
            case not null:
                _settings.DemoMode = !_settings.DemoMode;
                break;
        }
        RefreshServerLabel();
    }

    private async Task CompleteAsync(Models.UserSession session)
    {
        await _session.SignInAsync(session);
        Password = "";
        OtpCode = "";
        IsOtpStage = false;
        await _queue.InitializeAsync();
        _ = _queue.ProcessAsync();                      // resume any photos captured while signed out / offline
        await Shell.Current.GoToAsync("//dashboard");
    }
}
