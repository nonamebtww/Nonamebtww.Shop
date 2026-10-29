using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Mappings;

internal static class OrderMappings {
  public static AddressDto ToDto(this DeliveryAddress address) =>
    new(address.City, address.Street, address.House, address.Apartment, address.PostalCode, address.Comment);

  public static DeliveryAddress ToDomain(this AddressDto address) =>
    new(address.City, address.Street, address.House, address.Apartment, address.PostalCode, address.Comment);

  public static OrderSummaryDto ToSummary(this Order order) =>
    new(order.Id, order.PlacedAt, order.Status, order.Total.Amount, order.TotalQuantity, order.DeliveryType,
      order.Delivery?.Status);

  public static OrderDetailsDto ToDetails(this Order order, IReadOnlyDictionary<Guid, Product> products, string? courierName) {
    var lastPayment = order.SucceededPayment ?? order.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault();

    return new OrderDetailsDto(
      order.Id,
      order.UserId,
      order.PlacedAt,
      order.Status,
      order.DeliveryType,
      order.DeliveryAddress?.ToDto(),
      order.PaymentMethod,
      order.Total.Amount,
      order.Items
        .Select(i => new OrderItemDto(i.ProductId, products.GetValueOrDefault(i.ProductId)?.Name ?? "Товар удалён",
          products.GetValueOrDefault(i.ProductId)?.Photo, i.UnitPrice.Amount, i.Quantity))
        .ToList(),
      lastPayment is null
        ? null
        : new PaymentDto(lastPayment.Id, lastPayment.Amount.Amount, lastPayment.Method, lastPayment.Status,
          lastPayment.TransactionId, lastPayment.PaidAt),
      order.Delivery is null
        ? null
        : new DeliveryDto(order.Delivery.CourierId, courierName, order.Delivery.Status, order.Delivery.AssignedAt,
          order.Delivery.DeliveredAt, order.Delivery.FailureReason),
      order.Status == OrderStatus.AwaitingPayment && order.SubmittedAt is { } submitted
        ? submitted + Order.PaymentTimeout
        : null);
  }
}
