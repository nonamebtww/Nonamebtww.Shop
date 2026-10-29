using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class AdminHubPage : ContentPageBase<AdminHubViewModel> {
  public AdminHubPage(AdminHubViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
