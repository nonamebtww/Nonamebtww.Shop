using Nonamebtww.Shop.Mobile.ViewModels.Auth;

namespace Nonamebtww.Shop.Mobile.Views.Auth;

public partial class LoginPage : ContentPageBase {
  public LoginPage(LoginViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
