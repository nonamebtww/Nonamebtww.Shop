using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class DeliveryDistributionPage : ContentPageBase<DeliveryDistributionViewModel> {
  public DeliveryDistributionPage(DeliveryDistributionViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
