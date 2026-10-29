using Nonamebtww.Shop.Mobile.ViewModels.Catalog;

namespace Nonamebtww.Shop.Mobile.Views.Catalog;

public partial class CatalogPage : ContentPageBase<CatalogViewModel> {
  public CatalogPage(CatalogViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
