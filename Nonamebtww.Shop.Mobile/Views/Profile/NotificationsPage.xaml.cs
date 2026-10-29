using Nonamebtww.Shop.Mobile.ViewModels.Profile;

namespace Nonamebtww.Shop.Mobile.Views.Profile;

public partial class NotificationsPage : ContentPageBase<NotificationsViewModel> {
  public NotificationsPage(NotificationsViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
