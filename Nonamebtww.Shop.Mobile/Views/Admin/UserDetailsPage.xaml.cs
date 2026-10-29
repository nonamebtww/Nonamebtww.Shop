using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class UserDetailsPage : ContentPageBase<UserDetailsViewModel> {
  public UserDetailsPage(UserDetailsViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
