using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Payments;

/// <summary>«Статус оплаты» от платёжной системы.</summary>
public sealed record PaymentResult(PaymentStatus Status, string? TransactionId, string? Message = null) {
  public bool IsSuccess => Status is PaymentStatus.Succeeded or PaymentStatus.Refunded or PaymentStatus.Cancelled;
}
