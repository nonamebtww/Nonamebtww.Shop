using Nonamebtww.Shop.Mobile.ViewModels.Auth;

namespace Nonamebtww.Shop.Mobile.Views.Auth;

public partial class ForgotPasswordPage : ContentPageBase {
  public ForgotPasswordPage(ForgotPasswordViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
