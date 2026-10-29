using Nonamebtww.Shop.Mobile.ViewModels.Cart;

namespace Nonamebtww.Shop.Mobile.Views.Cart;

public partial class CheckoutPage : ContentPageBase<CheckoutViewModel> {
  public CheckoutPage(CheckoutViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
