using Nonamebtww.Shop.Mobile.ViewModels.Catalog;

namespace Nonamebtww.Shop.Mobile.Views.Catalog;

public partial class ProductPage : ContentPageBase {
  public ProductPage(ProductViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
