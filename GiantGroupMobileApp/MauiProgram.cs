using GiantGroupMobileApp.Services;
using GiantGroupMobileApp.ViewModels;
using GiantGroupMobileApp.Views;
using Microsoft.Extensions.Logging;

namespace GiantGroupMobileApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---- services
        var s = builder.Services;
        s.AddSingleton<ISettingsService, SettingsService>();
        s.AddSingleton<ISessionService, SessionService>();
        s.AddSingleton<IDialogService, DialogService>();
        s.AddSingleton<AppState>();

        // One long-lived HttpClient (avoids socket exhaustion). Timeouts are applied per request in ApiClient.
        s.AddSingleton(_ => new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
        s.AddSingleton<ApiClient>();
        s.AddSingleton<DemoApiClient>();
        s.AddSingleton<IApiClient, ApiClientRouter>();

        s.AddSingleton<IPhotoQueueService, PhotoQueueService>();
        s.AddSingleton<IPhotoCache, PhotoCache>();
        s.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();

        // ---- view models + pages (transient: a fresh instance per navigation)
        s.AddTransient<StartupViewModel>();
        s.AddTransient<LoginViewModel>();
        s.AddTransient<DashboardViewModel>();
        s.AddTransient<RequestsViewModel>();
        s.AddTransient<RequestDetailViewModel>();
        s.AddTransient<JobsViewModel>();
        s.AddTransient<JobDetailViewModel>();
        s.AddTransient<PhotosViewModel>();
        s.AddTransient<PhotoViewerViewModel>();
        s.AddTransient<UploadQueueViewModel>();
        s.AddTransient<SettingsViewModel>();

        s.AddTransient<StartupPage>();
        s.AddTransient<LoginPage>();
        s.AddTransient<DashboardPage>();
        s.AddTransient<RequestsPage>();
        s.AddTransient<RequestDetailPage>();
        s.AddTransient<JobsPage>();
        s.AddTransient<JobDetailPage>();
        s.AddTransient<PhotosPage>();
        s.AddTransient<PhotoViewerPage>();
        s.AddTransient<UploadQueuePage>();
        s.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
