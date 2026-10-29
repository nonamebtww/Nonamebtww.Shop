using Nonamebtww.Shop.Mobile.ViewModels.Auth;

namespace Nonamebtww.Shop.Mobile.Views.Auth;

public partial class RegisterPage : ContentPageBase {
  public RegisterPage(RegisterViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
