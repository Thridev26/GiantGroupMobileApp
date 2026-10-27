using GiantGroupMobileApp.Services;

namespace GiantGroupMobileApp;

public partial class App : Application
{
    private readonly IPhotoQueueService _queue;

    public App(ISessionService session, IPhotoQueueService queue)
    {
        InitializeComponent();
        _queue = queue;
        session.SessionExpired += OnSessionExpired;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        // Coming back to the app (e.g. after the camera or a dead zone) – flush any waiting photos.
        window.Resumed += (_, _) => _ = _queue.ProcessAsync();
        return window;
    }

    private static void OnSessionExpired()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync("//login");
                if (Shell.Current.CurrentPage is { } page)
                    await page.DisplayAlert("Session expired", "Please sign in again. Photos waiting to upload are kept and will upload after you sign in.", "OK");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        });
    }
}
