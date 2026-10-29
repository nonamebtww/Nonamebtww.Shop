using Nonamebtww.Shop.Mobile.ViewModels.Profile;

namespace Nonamebtww.Shop.Mobile.Views.Profile;

public partial class ChangePasswordPage : ContentPageBase {
  public ChangePasswordPage(ChangePasswordViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
