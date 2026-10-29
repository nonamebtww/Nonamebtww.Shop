using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class CourierTasksPage : ContentPageBase<CourierTasksViewModel> {
  public CourierTasksPage(CourierTasksViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
