using Nonamebtww.Shop.Domain.Events.Abstractions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate.Events;

/// <summary>Заказ создан из корзины (15.1). Остаток товаров списывается под заказ.</summary>
public sealed record OrderCreated(Guid OrderId, Guid UserId, Money Total, IReadOnlyList<OrderLine> Items) : DomainEvent;

/// <summary>Любая смена статуса заказа.</summary>
public sealed record OrderStatusChanged(Guid OrderId, Guid UserId, OrderStatus From, OrderStatus To) : DomainEvent;

/// <summary>Вход в «Ожидает оплаты»: передать данные платежа в платёжную систему (15.4).</summary>
public sealed record OrderSubmitted(Guid OrderId, Guid UserId, Money Total, PaymentMethod PaymentMethod) : DomainEvent;

/// <summary>Оплата прошла, платёж сохранён (15.4).</summary>
public sealed record OrderPaymentSucceeded(Guid OrderId, Guid PaymentId, string TransactionId, Money Amount) : DomainEvent;

/// <summary>Оплата отклонена: уведомить покупателя.</summary>
public sealed record OrderPaymentDeclined(Guid OrderId, Guid UserId, Guid PaymentId, string? Reason) : DomainEvent;

/// <summary>Вход в «Подтверждён»: очистить корзину, отправить покупателю подтверждение (15.5).</summary>
public sealed record OrderConfirmed(Guid OrderId, Guid UserId) : DomainEvent;

/// <summary>Вход в «Курьер назначен»: сформировать задание на доставку курьеру (12.1).</summary>
public sealed record OrderCourierAssigned(Guid OrderId, Guid CourierId) : DomainEvent;

/// <summary>Вход в «В доставке»: уведомить покупателя «Заказ передан в доставку» (12.4).</summary>
public sealed record OrderInDelivery(Guid OrderId, Guid UserId, Guid CourierId) : DomainEvent;

/// <summary>Вход в «Завершён»: уведомить покупателя «Заказ вручён» (12.4).</summary>
public sealed record OrderCompleted(Guid OrderId, Guid UserId) : DomainEvent;

/// <summary>Вручение не состоялось, назначение курьера снято — заказ снова ждёт курьера.</summary>
public sealed record OrderDeliveryFailed(Guid OrderId, Guid CourierId, string? Reason) : DomainEvent;

/// <summary>
/// Вход в «Отменён»: вернуть товары в остаток, уведомить покупателя.
/// <see cref="PaymentVoidRequired"/> — аннулировать платёж в платёжной системе,
/// <see cref="RefundRequired"/> — вернуть оплату.
/// </summary>
public sealed record OrderCancelled(
  Guid OrderId,
  Guid UserId,
  OrderStatus PreviousStatus,
  OrderCancellationReason Reason,
  bool PaymentVoidRequired,
  bool RefundRequired,
  IReadOnlyList<OrderLine> Items) : DomainEvent;
