using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Cart;

/// <summary>
/// Use case «Оформить заказ»: delivery type and address (15.2), payment method (15.3),
/// confirmation (order goes to «Ожидает оплаты») and online payment (15.4).
/// </summary>
public sealed partial class CheckoutViewModel(
  ICartService cart,
  IOrderService orders,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  private Guid _orderId;

  [ObservableProperty]
  public partial string SummaryText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string TotalText { get; set; } = string.Empty;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(IsExpressDelivery))]
  public partial bool IsStandardDelivery { get; set; } = true;

  public bool IsExpressDelivery {
    get => !IsStandardDelivery;
    set => IsStandardDelivery = !value;
  }

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(IsSbp), nameof(PayButtonText))]
  public partial bool IsCard { get; set; } = true;

  public bool IsSbp {
    get => !IsCard;
    set => IsCard = !value;
  }

  [ObservableProperty]
  public partial string City { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Street { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string House { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Apartment { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string PostalCode { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Comment { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsPaymentStep { get; set; }

  [ObservableProperty]
  public partial string OrderText { get; set; } = string.Empty;

  public string PayButtonText => IsCard ? "Оплатить картой" : "Оплатить через СБП";

  public override async Task OnAppearingAsync() {
    if (IsPaymentStep) {
      return;
    }

    var dto = await ExecuteAsync(() => cart.GetAsync());
    if (dto is null) {
      return;
    }

    if (dto.IsEmpty) {
      await Dialogs.AlertAsync("Оформление заказа", "Корзина пуста.");
      await Navigation.GoBackAsync();
      return;
    }

    SummaryText = $"Товаров: {dto.TotalQuantity}";
    TotalText = DisplayText.Money(dto.TotalAmount);
  }

  [RelayCommand]
  private async Task PlaceOrderAsync() {
    var address = new AddressDto(
      City.Trim(),
      Street.Trim(),
      House.Trim(),
      NullIfEmpty(Apartment),
      NullIfEmpty(PostalCode),
      NullIfEmpty(Comment));

    var request = new CheckoutRequest(
      IsStandardDelivery ? DeliveryType.Standard : DeliveryType.Express,
      address,
      IsCard ? PaymentMethod.Card : PaymentMethod.Sbp);

    var order = await ExecuteAsync(() => orders.CheckoutAsync(request));
    if (order is null) {
      return;
    }

    _orderId = order.Id;
    TotalText = DisplayText.Money(order.TotalAmount);
    OrderText = $"Заказ {DisplayText.ShortId(order.Id)} создан и ожидает оплаты."
      + (order.PaymentDeadline is { } deadline ? $" Оплатите до {deadline.ToLocalTime():HH:mm}, иначе заказ отменится." : string.Empty);
    IsPaymentStep = true;
  }

  [RelayCommand]
  private async Task PayAsync() {
    var order = await ExecuteAsync(() => orders.PayAsync(_orderId));
    if (order is null) {
      return;
    }

    if (order.Status == OrderStatus.AwaitingPayment) {
      await Dialogs.AlertAsync("Оплата", "Платёж отклонён платёжной системой. Попробуйте ещё раз.");
      return;
    }

    await Dialogs.AlertAsync("Заказ подтверждён", "Оплата прошла. Мы сообщим, когда заказ передадут курьеру.");
    await OpenOrderAsync();
  }

  [RelayCommand]
  private Task PayLaterAsync() => OpenOrderAsync();

  private Task OpenOrderAsync() => Navigation.GoToAsync(
    $"//{Routes.Main}/{Routes.Orders}/{Routes.OrderDetails}",
    new Dictionary<string, object> { [Routes.Params.Id] = _orderId });

  private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
