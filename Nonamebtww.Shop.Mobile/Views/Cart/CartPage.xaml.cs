using Nonamebtww.Shop.Mobile.ViewModels.Cart;

namespace Nonamebtww.Shop.Mobile.Views.Cart;

public partial class CartPage : ContentPageBase<CartViewModel> {
  public CartPage(CartViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
