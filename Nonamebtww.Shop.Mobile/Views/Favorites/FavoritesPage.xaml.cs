using Nonamebtww.Shop.Mobile.ViewModels.Favorites;

namespace Nonamebtww.Shop.Mobile.Views.Favorites;

public partial class FavoritesPage : ContentPageBase<FavoritesViewModel> {
  public FavoritesPage(FavoritesViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
