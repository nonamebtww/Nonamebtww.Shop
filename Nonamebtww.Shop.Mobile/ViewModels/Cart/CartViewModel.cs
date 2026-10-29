using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Cart;

/// <summary>Use case «Просмотр корзины»: change quantity, remove items, go to checkout.</summary>
public sealed partial class CartViewModel(ICartService cart, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  private CartDto _cart = CartDto.Empty;

  public ObservableCollection<CartLineItem> Lines { get; } = [];

  [ObservableProperty]
  public partial bool IsRefreshing { get; set; }

  [ObservableProperty]
  public partial bool HasItems { get; set; }

  [ObservableProperty]
  public partial string SummaryText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string TotalText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => UpdateAsync(() => cart.GetAsync());

  [RelayCommand]
  private async Task RefreshAsync() {
    await UpdateAsync(() => cart.GetAsync());
    IsRefreshing = false;
  }

  [RelayCommand]
  private Task IncreaseAsync(CartLineItem line) =>
    UpdateAsync(() => cart.ChangeQuantityAsync(line.ProductId, line.Quantity + 1), showBusy: false);

  [RelayCommand]
  private Task DecreaseAsync(CartLineItem line) => line.Quantity <= 1
    ? RemoveAsync(line)
    : UpdateAsync(() => cart.ChangeQuantityAsync(line.ProductId, line.Quantity - 1), showBusy: false);

  [RelayCommand]
  private async Task RemoveAsync(CartLineItem line) {
    if (await Dialogs.ConfirmAsync("Корзина", $"Удалить «{line.Name}» из корзины?")) {
      await UpdateAsync(() => cart.RemoveAsync(line.ProductId), showBusy: false);
    }
  }

  [RelayCommand]
  private async Task ClearAsync() {
    if (HasItems && await Dialogs.ConfirmAsync("Корзина", "Удалить все товары из корзины?")) {
      await UpdateAsync(() => cart.ClearAsync());
    }
  }

  [RelayCommand]
  private async Task CheckoutAsync() {
    if (_cart.HasUnavailableItems) {
      await Dialogs.AlertAsync("Корзина", "Часть товаров недоступна в нужном количестве. Измените количество или удалите их.");
      return;
    }

    await Navigation.GoToAsync(Routes.Checkout);
  }

  [RelayCommand]
  private Task OpenProductAsync(CartLineItem line) =>
    Navigation.GoToAsync(Routes.Product, new Dictionary<string, object> { [Routes.Params.Id] = line.ProductId });

  [RelayCommand]
  private Task GoToCatalogAsync() => Navigation.GoToRootAsync($"{Routes.Main}/{Routes.Catalog}");

  private async Task UpdateAsync(Func<Task<Nonamebtww.Shop.Application.Common.Results.Result<CartDto>>> useCase, bool showBusy = true) {
    var dto = await ExecuteAsync(useCase, showBusy);
    if (dto is not null) {
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
