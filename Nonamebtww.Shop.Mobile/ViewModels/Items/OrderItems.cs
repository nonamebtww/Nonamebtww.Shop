using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Mobile.Presentation;

namespace Nonamebtww.Shop.Mobile.ViewModels.Items;

public sealed record OrderListItem(Guid Id, string Number, string StatusText, Color StatusColor, string Details, string Date, string TotalText) {
  public static OrderListItem From(OrderSummaryDto order) => new(
    order.Id,
    $"Заказ {DisplayText.ShortId(order.Id)}",
    DisplayText.Of(order.Status),
    DisplayText.ColorOf(order.Status),
    $"Товаров: {order.ItemsCount}"
      + (order.DeliveryType is { } type ? $" · {DisplayText.Of(type)}" : string.Empty)
      + (order.DeliveryStatus is { } delivery ? $" · {DisplayText.Of(delivery)}" : string.Empty),
    DisplayText.Date(order.CreatedAt),
    DisplayText.Money(order.TotalAmount));
}

public sealed record OrderLineItem(string Name, string Photo, string QuantityText, string TotalText) {
  public static OrderLineItem From(OrderItemDto item) => new(
    item.Name,
    ImageSources.Photo(item.PhotoUrl),
    $"{item.Quantity} × {DisplayText.Money(item.Price)}",
    DisplayText.Money(item.LineTotal));
}

/// <summary>One step of the order state diagram shown as a timeline.</summary>
public sealed record StatusStep(string Caption, string Glyph, Color Color, double Opacity) {
  private static readonly OrderStatus[] HappyPath = [
    OrderStatus.Checkout,
    OrderStatus.AwaitingPayment,
    OrderStatus.Confirmed,
    OrderStatus.CourierAssigned,
    OrderStatus.InDelivery,
    OrderStatus.Completed
  ];

  public static IReadOnlyList<StatusStep> For(OrderStatus current) {
    if (current == OrderStatus.Cancelled) {
      return [new StatusStep(DisplayText.Of(OrderStatus.Cancelled), "✕", DisplayText.ColorOf(OrderStatus.Cancelled), 1)];
    }

    var index = Array.IndexOf(HappyPath, current);
    return HappyPath.Select((status, i) => i < index
        ? new StatusStep(DisplayText.Of(status), "✓", DisplayText.ColorOf(OrderStatus.Completed), 1)
        : i == index
          ? new StatusStep(DisplayText.Of(status), "●", DisplayText.ColorOf(status), 1)
          : new StatusStep(DisplayText.Of(status), "○", Colors.Gray, 0.5))
      .ToList();
  }
}
