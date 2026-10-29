using Nonamebtww.Shop.Mobile.ViewModels.Orders;

namespace Nonamebtww.Shop.Mobile.Views.Orders;

public partial class OrdersPage : ContentPageBase {
  public OrdersPage(OrdersViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
