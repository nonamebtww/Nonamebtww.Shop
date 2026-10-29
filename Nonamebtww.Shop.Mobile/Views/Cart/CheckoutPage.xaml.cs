using Nonamebtww.Shop.Mobile.ViewModels.Cart;

namespace Nonamebtww.Shop.Mobile.Views.Cart;

public partial class CheckoutPage : ContentPageBase {
  public CheckoutPage(CheckoutViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
