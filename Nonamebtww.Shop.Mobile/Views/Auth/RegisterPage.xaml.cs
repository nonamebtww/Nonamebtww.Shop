using Nonamebtww.Shop.Mobile.ViewModels.Auth;

namespace Nonamebtww.Shop.Mobile.Views.Auth;

public partial class RegisterPage : ContentPageBase<RegisterViewModel> {
  public RegisterPage(RegisterViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
