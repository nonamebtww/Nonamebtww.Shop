using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class ProductEditPage : ContentPageBase<ProductEditViewModel> {
  public ProductEditPage(ProductEditViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
