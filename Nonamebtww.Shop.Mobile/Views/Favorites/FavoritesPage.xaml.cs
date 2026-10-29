using Nonamebtww.Shop.Mobile.ViewModels.Favorites;

namespace Nonamebtww.Shop.Mobile.Views.Favorites;

public partial class FavoritesPage : ContentPageBase {
  public FavoritesPage(FavoritesViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
