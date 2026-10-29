using Nonamebtww.Shop.Mobile.ViewModels.Profile;

namespace Nonamebtww.Shop.Mobile.Views.Profile;

public partial class PersonalDataPage : ContentPageBase<PersonalDataViewModel> {
  public PersonalDataPage(PersonalDataViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
