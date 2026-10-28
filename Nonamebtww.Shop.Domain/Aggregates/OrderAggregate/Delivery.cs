using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Доставка заказа (IDEF1X «Доставка», 0..1 на заказ). Идентификатор совпадает с id заказа.
/// </summary>
public class Delivery : Entity<Guid> {
  private Delivery() { }

  internal Delivery(Guid orderId, Guid courierId, DateTimeOffset assignedAt) : base(orderId) {
    CourierId = Guard.NotEmpty(courierId, "id_курьера");
    Status = DeliveryStatus.Assigned;
    AssignedAt = assignedAt;
  }

  public Guid CourierId { get; private set; }
  public DeliveryStatus Status { get; private set; }

  /// <summary>Дата назначения курьера.</summary>
  public DateTimeOffset AssignedAt { get; private set; }

  public DateTimeOffset? AcceptedAt { get; private set; }

  /// <summary>Дата вручения.</summary>
  public DateTimeOffset? DeliveredAt { get; private set; }

  public string? FailureReason { get; private set; }

  internal void Reassign(Guid courierId, DateTimeOffset now) {
    CourierId = Guard.NotEmpty(courierId, "id_курьера");
    Status = DeliveryStatus.Assigned;
    AssignedAt = now;
    AcceptedAt = null;
    DeliveredAt = null;
    FailureReason = null;
  }

  internal void Accept(DateTimeOffset now) {
    Status = DeliveryStatus.Accepted;
    AcceptedAt = now;
  }

  internal void MarkDelivered(DateTimeOffset now) {
    Status = DeliveryStatus.Delivered;
    DeliveredAt = now;
  }

  internal void MarkFailed(string? reason) {
    Status = DeliveryStatus.Failed;
    FailureReason = Guard.Optional(reason, "Причина", 512);
  }

  internal void Cancel() => Status = DeliveryStatus.Cancelled;
}
