using Nonamebtww.Shop.Mobile.ViewModels.Profile;

namespace Nonamebtww.Shop.Mobile.Views.Profile;

public partial class DeleteAccountPage : ContentPageBase<DeleteAccountViewModel> {
  public DeleteAccountPage(DeleteAccountViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
