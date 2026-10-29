using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Cart;

/// <summary>Use case «Просмотр корзины»: change quantity, remove items, go to checkout.</summary>
public sealed partial class CartViewModel(ICartService cart, RemoteDataCache cache, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  private CartDto _cart = CartDto.Empty;

  public ObservableCollection<CartLineItem> Lines { get; } = [];

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  [Reactive]
  public partial bool HasItems { get; set; }

  [Reactive]
  public partial string SummaryText { get; set; } = string.Empty;

  [Reactive]
  public partial string TotalText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => UpdateAsync(() => cart.GetAsync());

  [ReactiveCommand]
  private async Task RefreshAsync() {
    await UpdateAsync(() => cart.GetAsync());
    IsRefreshing = false;
  }

  [ReactiveCommand]
  private async Task IncreaseAsync(CartLineItem line) {
    // The buyer cannot take more than is in stock.
    if (!line.CanIncrease) {
      await Dialogs.AlertAsync("Количество", line.AvailableStock > 0
        ? $"«{line.Name}»: в наличии только {line.AvailableStock} шт."
        : $"«{line.Name}» сейчас нет в наличии.");
      return;
    }

    await UpdateAsync(() => cart.ChangeQuantityAsync(line.ProductId, line.Quantity + 1), showBusy: false);
  }

  [ReactiveCommand]
  private Task DecreaseAsync(CartLineItem line) => line.Quantity <= 1
    ? RemoveAsync(line)
    : UpdateAsync(() => cart.ChangeQuantityAsync(line.ProductId, line.Quantity - 1), showBusy: false);

  [ReactiveCommand]
  private async Task RemoveAsync(CartLineItem line) {
    if (await Dialogs.ConfirmAsync("Корзина", $"Удалить «{line.Name}» из корзины?")) {
      await UpdateAsync(() => cart.RemoveAsync(line.ProductId), showBusy: false);
    }
  }

  [ReactiveCommand]
  private async Task ClearAsync() {
    if (HasItems && await Dialogs.ConfirmAsync("Корзина", "Удалить все товары из корзины?")) {
      await UpdateAsync(() => cart.ClearAsync());
    }
  }

  [ReactiveCommand]
  private async Task CheckoutAsync() {
    // Re-read the cart: stock could change while the cart was open.
    await UpdateAsync(() => cart.GetAsync(), showBusy: false);
    var problems = _cart.Items
      .Where(x => !x.IsAvailable)
      .Select(x => !x.IsActive
        ? $"• {x.Name}: снят с продажи"
        : x.AvailableStock == 0
          ? $"• {x.Name}: нет в наличии"
          : $"• {x.Name}: в корзине {x.Quantity} шт., в наличии {x.AvailableStock} шт.")
      .ToList();

    if (problems.Count > 0) {
      await Dialogs.AlertAsync("Недостаточно товара",
        "Количество превышает остаток на складе:\n" + string.Join("\n", problems) + "\n\nУменьшите количество или удалите эти товары.");
      return;
    }

    await Navigation.GoToAsync(Routes.Checkout);
  }

  [ReactiveCommand]
  private Task OpenProductAsync(CartLineItem line) =>
    Navigation.GoToAsync(Routes.Product, new Dictionary<string, object> { [Routes.Params.Id] = line.ProductId });

  [ReactiveCommand]
  private Task GoToCatalogAsync() => Navigation.GoToRootAsync($"{Routes.Main}/{Routes.Catalog}");

  private async Task UpdateAsync(Func<Task<Nonamebtww.Shop.Application.Common.Results.Result<CartDto>>> useCase, bool showBusy = true) {
    var dto = await ExecuteAsync(useCase, showBusy);
    if (dto is not null) {
      cache.Invalidate(CacheKeys.Product);
      Apply(dto);
    }
  }

  private void Apply(CartDto dto) {
    _cart = dto;
    Lines.Clear();
    foreach (var item in dto.Items) {
      Lines.Add(CartLineItem.From(item));
    }

    HasItems = !dto.IsEmpty;
    SummaryText = $"Товаров: {dto.TotalQuantity}";
    TotalText = DisplayText.Money(dto.TotalAmount);
  }
}
