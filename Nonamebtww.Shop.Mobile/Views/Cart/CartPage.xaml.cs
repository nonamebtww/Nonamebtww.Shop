using Nonamebtww.Shop.Mobile.ViewModels.Cart;

namespace Nonamebtww.Shop.Mobile.Views.Cart;

public partial class CartPage : ContentPageBase {
  public CartPage(CartViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
