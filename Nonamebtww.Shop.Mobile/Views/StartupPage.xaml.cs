using Nonamebtww.Shop.Mobile.ViewModels;

namespace Nonamebtww.Shop.Mobile.Views;

public partial class StartupPage : ContentPageBase {
  public StartupPage(StartupViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
