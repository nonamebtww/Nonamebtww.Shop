using Nonamebtww.Shop.Mobile.ViewModels.Orders;

namespace Nonamebtww.Shop.Mobile.Views.Orders;

public partial class OrderDetailsPage : ContentPageBase<OrderDetailsViewModel> {
  public OrderDetailsPage(OrderDetailsViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
