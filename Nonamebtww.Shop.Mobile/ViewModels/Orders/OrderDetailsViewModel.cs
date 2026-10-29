using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Orders;

/// <summary>Order card with its status on the order state diagram; pay or cancel when allowed.</summary>
public sealed partial class OrderDetailsViewModel(IOrderService orders, RemoteDataCache cache, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private Guid _orderId;

  public ObservableCollection<StatusStep> Timeline { get; } = [];
  public ObservableCollection<OrderLineItem> Lines { get; } = [];

  [Reactive]
  public partial bool IsLoaded { get; set; }

  [Reactive]
  public partial string DateText { get; set; } = string.Empty;

  [Reactive]
  public partial string StatusText { get; set; } = string.Empty;

  [Reactive]
  public partial Color StatusColor { get; set; } = Colors.Gray;

  [Reactive]
  public partial string DeliveryText { get; set; } = string.Empty;

  [Reactive]
  public partial string AddressText { get; set; } = string.Empty;

  [Reactive]
  public partial string CourierText { get; set; } = string.Empty;

  [Reactive]
  public partial string PaymentText { get; set; } = string.Empty;

  [Reactive]
  public partial string TotalText { get; set; } = string.Empty;

  [Reactive]
  public partial bool CanPay { get; set; }

  [Reactive]
  public partial bool CanCancel { get; set; }

  [Reactive]
  public partial string PayButtonText { get; set; } = "Оплатить";

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    if (query.TryGetValue(Routes.Params.Id, out var id) && id is Guid orderId) {
      _orderId = orderId;
    }
  }

  public override async Task OnAppearingAsync() {
    var order = await ExecuteAsync(() => orders.GetOrderAsync(_orderId));
    if (order is not null) {
      Apply(order);
    }
  }

  [ReactiveCommand]
  private async Task PayAsync() {
    var order = await ExecuteAsync(() => orders.PayAsync(_orderId));
    if (order is null) {
      return;
    }

    cache.Invalidate();
    Apply(order);
    await Dialogs.AlertAsync("Оплата", order.Status == OrderStatus.AwaitingPayment
      ? "Платёж отклонён платёжной системой. Попробуйте ещё раз."
      : "Оплата прошла, заказ подтверждён.");
  }

  [ReactiveCommand]
  private async Task CancelAsync() {
    if (!await Dialogs.ConfirmAsync("Отмена заказа", "Отменить заказ? Оплаченные деньги вернутся на карту.")) {
      return;
    }

    var order = await ExecuteAsync(() => orders.CancelAsync(_orderId));
    if (order is not null) {
      cache.Invalidate();
      Apply(order);
    }
  }

  private void Apply(OrderDetailsDto order) {
    Title = $"Заказ {DisplayText.ShortId(order.Id)}";
    DateText = $"Оформлен {DisplayText.Date(order.CreatedAt)}";
    StatusText = DisplayText.Of(order.Status);
    StatusColor = DisplayText.ColorOf(order.Status);

    DeliveryText = order.DeliveryType is { } type ? DisplayText.Of(type) : "Тип доставки не выбран";
    // Pickup / parcel locker: the point name is kept in the address comment.
    AddressText = order.Address is not { } address
      ? string.Empty
      : order.DeliveryType is { } kind && !kind.IsToDoor() && !string.IsNullOrWhiteSpace(address.Comment)
        ? $"{address.Comment}: {address}"
        : address.ToString();
    CourierText = order.Delivery is { } delivery
      ? $"{DisplayText.Of(delivery.Status)}" + (delivery.CourierName is { } courier ? $" · курьер {courier}" : string.Empty)
        + (delivery.FailureReason is { } reason ? $" · {reason}" : string.Empty)
      : string.Empty;

    var method = order.PaymentMethod is { } m ? DisplayText.Of(m) : "Способ оплаты не выбран";
    PaymentText = order.Payment is { } payment
      ? $"{method} · {DisplayText.Of(payment.Status)}" + (payment.PaidAt is { } paidAt ? $" {DisplayText.Date(paidAt)}" : string.Empty)
      : method;

    TotalText = DisplayText.Money(order.TotalAmount);
    CanPay = order.CanPay;
    CanCancel = order.CanCancel;
    PayButtonText = $"Оплатить {TotalText}"
      + (order.PaymentDeadline is { } deadline ? $" до {deadline.ToLocalTime():HH:mm}" : string.Empty);

    Timeline.Clear();
    foreach (var step in StatusStep.For(order.Status)) {
      Timeline.Add(step);
    }

    Lines.Clear();
    foreach (var item in order.Items) {
      Lines.Add(OrderLineItem.From(item));
    }

    IsLoaded = true;
  }
}
