using GiantGroupMobileApp.Views;

namespace GiantGroupMobileApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed (non-tab) pages
        Routing.RegisterRoute("requestdetail", typeof(RequestDetailPage));
        Routing.RegisterRoute("jobdetail", typeof(JobDetailPage));
        Routing.RegisterRoute("photos", typeof(PhotosPage));
        Routing.RegisterRoute("photoviewer", typeof(PhotoViewerPage));
        Routing.RegisterRoute("queue", typeof(UploadQueuePage));
    }
}
