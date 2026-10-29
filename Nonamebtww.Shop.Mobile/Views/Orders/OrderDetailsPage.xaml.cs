using Nonamebtww.Shop.Mobile.ViewModels.Orders;

namespace Nonamebtww.Shop.Mobile.Views.Orders;

public partial class OrderDetailsPage : ContentPageBase {
  public OrderDetailsPage(OrderDetailsViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
