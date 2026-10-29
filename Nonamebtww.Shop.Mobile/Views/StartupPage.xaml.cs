using Nonamebtww.Shop.Mobile.ViewModels;

namespace Nonamebtww.Shop.Mobile.Views;

public partial class StartupPage : ContentPageBase<StartupViewModel> {
  public StartupPage(StartupViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
