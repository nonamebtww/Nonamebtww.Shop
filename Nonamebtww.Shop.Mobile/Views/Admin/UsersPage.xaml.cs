using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class UsersPage : ContentPageBase {
  public UsersPage(UsersViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
