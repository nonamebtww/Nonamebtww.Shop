using Nonamebtww.Shop.Mobile.ViewModels.Orders;

namespace Nonamebtww.Shop.Mobile.Views.Orders;

public partial class OrdersPage : ContentPageBase<OrdersViewModel> {
  public OrdersPage(OrdersViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
