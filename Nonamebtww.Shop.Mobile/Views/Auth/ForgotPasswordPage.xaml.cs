using Nonamebtww.Shop.Mobile.ViewModels.Auth;

namespace Nonamebtww.Shop.Mobile.Views.Auth;

public partial class ForgotPasswordPage : ContentPageBase<ForgotPasswordViewModel> {
  public ForgotPasswordPage(ForgotPasswordViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
