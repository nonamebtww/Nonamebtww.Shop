using Nonamebtww.Shop.Mobile.ViewModels.Catalog;

namespace Nonamebtww.Shop.Mobile.Views.Catalog;

public partial class CatalogPage : ContentPageBase {
  public CatalogPage(CatalogViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
