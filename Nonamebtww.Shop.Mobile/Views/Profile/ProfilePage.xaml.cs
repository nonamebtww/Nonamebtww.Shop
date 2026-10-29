using Nonamebtww.Shop.Mobile.ViewModels.Profile;

namespace Nonamebtww.Shop.Mobile.Views.Profile;

public partial class ProfilePage : ContentPageBase {
  public ProfilePage(ProfileViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
