using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class CatalogManagementPage : ContentPageBase {
  public CatalogManagementPage(CatalogManagementViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
