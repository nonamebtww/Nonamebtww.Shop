using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class CourierTasksPage : ContentPageBase {
  public CourierTasksPage(CourierTasksViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
