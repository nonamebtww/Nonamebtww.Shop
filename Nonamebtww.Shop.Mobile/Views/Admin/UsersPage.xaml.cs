using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class UsersPage : ContentPageBase<UsersViewModel> {
  public UsersPage(UsersViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
