using GiantGroupMobileApp.ViewModels;

namespace GiantGroupMobileApp.Views;

public partial class JobDetailPage : ContentPage
{
    private readonly JobDetailViewModel _vm;

    public JobDetailPage(JobDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnDisappearing();
    }
}
