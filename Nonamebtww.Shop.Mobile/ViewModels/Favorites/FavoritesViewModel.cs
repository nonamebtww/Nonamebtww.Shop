using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Favorites;

/// <summary>Use case «Просмотр избранного»: list, remove, move to cart.</summary>
public sealed partial class FavoritesViewModel(
  IFavoritesService favorites,
  ICartService cart,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<ProductCardItem> Items { get; } = [];

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  public override Task OnAppearingAsync() => LoadAsync();

  [ReactiveCommand]
  private async Task RefreshAsync() {
    await LoadAsync();
    IsRefreshing = false;
  }

  [ReactiveCommand]
  private async Task RemoveAsync(ProductCardItem item) {
    if (await ExecuteAsync(() => favorites.RemoveAsync(item.Id), showBusy: false)) {
      cache.Invalidate();
      Items.Remove(item);
    }
  }

  [ReactiveCommand]
  private async Task AddToCartAsync(ProductCardItem item) {
    if (await ExecuteAsync(() => cart.AddAsync(item.Id), showBusy: false) is not null) {
      cache.Invalidate();
      await Dialogs.AlertAsync("Корзина", $"«{item.Name}» добавлен в корзину.");
    }
  }

  [ReactiveCommand]
  private Task OpenProductAsync(ProductCardItem item) =>
    Navigation.GoToAsync(Routes.Product, new Dictionary<string, object> { [Routes.Params.Id] = item.Id });

  private async Task LoadAsync() {
    var list = await ExecuteAsync(() => favorites.GetAsync());
    if (list is null) {
      return;
    }

    Items.Clear();
    foreach (var favorite in list.OrderByDescending(x => x.AddedAt)) {
      Items.Add(new ProductCardItem(favorite.Product) { IsFavorite = true });
    }
  }
}
