using GiantGroupMobileApp.ViewModels;

namespace GiantGroupMobileApp.Views;

public partial class StartupPage : ContentPage
{
    private readonly StartupViewModel _vm;
    private bool _routed;

    public StartupPage(StartupViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_routed) return;
        _routed = true;
        try { await _vm.RouteAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.GoToAsync("//login");
        }
    }
}
