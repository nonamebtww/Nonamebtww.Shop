using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class UserDetailsPage : ContentPageBase {
  public UserDetailsPage(UserDetailsViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
