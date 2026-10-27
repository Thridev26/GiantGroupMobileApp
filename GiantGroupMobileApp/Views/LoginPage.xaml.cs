using GiantGroupMobileApp.ViewModels;

namespace GiantGroupMobileApp.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
