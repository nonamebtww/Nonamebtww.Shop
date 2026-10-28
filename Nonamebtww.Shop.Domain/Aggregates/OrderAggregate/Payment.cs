using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>Платёж по заказу (IDEF1X «Платёж»). У заказа может быть несколько попыток оплаты.</summary>
public class Payment : Entity<Guid> {
  private Payment() { }

  internal Payment(
    Money amount,
    PaymentMethod method,
    PaymentStatus status,
    string? transactionId,
    DateTimeOffset paidAt,
    string? failureReason = null) : base(Guid.NewGuid()) {
    Amount = amount;
    Method = method;
    Status = status;
    TransactionId = Guard.Optional(transactionId, "id_транзакции", 128);
    PaidAt = paidAt;
    FailureReason = Guard.Optional(failureReason, "Причина отказа", 512);
  }

  public Money Amount { get; private set; } = null!;
  public PaymentMethod Method { get; private set; }
  public PaymentStatus Status { get; private set; }

  /// <summary>Идентификатор транзакции в платёжной системе.</summary>
  public string? TransactionId { get; private set; }

  /// <summary>Дата платежа.</summary>
  public DateTimeOffset PaidAt { get; private set; }

  public string? FailureReason { get; private set; }

  internal void MarkRefunded() => Status = PaymentStatus.Refunded;
}
