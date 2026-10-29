using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class AdminHubPage : ContentPageBase {
  public AdminHubPage(AdminHubViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
