using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Orders;

/// <summary>«Доставка, адрес, оплата» процесса «Оформление заказа».</summary>
public sealed record CheckoutRequest(DeliveryType DeliveryType, AddressDto Address, PaymentMethod PaymentMethod);

/// <summary>Строка «Списка заказов».</summary>
public sealed record OrderSummaryDto(
  Guid Id,
  DateTimeOffset CreatedAt,
  OrderStatus Status,
  decimal TotalAmount,
  int ItemsCount,
  DeliveryType? DeliveryType,
  DeliveryStatus? DeliveryStatus) {
  public bool IsActive => Status.IsActive();
  public string StatusName => Status.ToDisplayName();
}

public sealed record OrderItemDto(Guid ProductId, string Name, string? PhotoUrl, decimal Price, int Quantity) {
  public decimal LineTotal => Price * Quantity;
}

public sealed record PaymentDto(
  Guid Id,
  decimal Amount,
  PaymentMethod Method,
  PaymentStatus Status,
  string? TransactionId,
  DateTimeOffset? PaidAt);

public sealed record DeliveryDto(
  Guid? CourierId,
  string? CourierName,
  DeliveryStatus Status,
  DateTimeOffset AssignedAt,
  DateTimeOffset? DeliveredAt,
  string? FailureReason);

/// <summary>«Подтверждение заказа» и детальная карточка заказа.</summary>
public sealed record OrderDetailsDto(
  Guid Id,
  Guid UserId,
  DateTimeOffset CreatedAt,
  OrderStatus Status,
  DeliveryType? DeliveryType,
  AddressDto? Address,
  PaymentMethod? PaymentMethod,
  decimal TotalAmount,
  IReadOnlyList<OrderItemDto> Items,
  PaymentDto? Payment,
  DeliveryDto? Delivery,
  DateTimeOffset? PaymentDeadline) {
  public bool IsActive => Status.IsActive();

  public string StatusName => Status.ToDisplayName();

  public bool CanPay => Status == OrderStatus.AwaitingPayment;

  public bool CanCancel => Status.IsCancellable();
}
