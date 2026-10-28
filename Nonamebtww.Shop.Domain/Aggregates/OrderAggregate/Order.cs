using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate.Events;
using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Заказ (IDEF1X «Заказ») с позициями, платежами и доставкой.
/// Жизненный цикл — по диаграмме состояний «Заказ»: каждый метод проверяет переход
/// через <see cref="OrderStatusTransitions"/>, а entry-действия состояний публикуются доменными событиями.
/// </summary>
public class Order : AggregateRoot<Guid> {
  /// <summary>Сколько заказ может ждать оплату, прежде чем будет отменён (after 30 мин).</summary>
  public static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(30);

  private readonly List<OrderItem> _items = [];
  private readonly List<Payment> _payments = [];

  private Order() { }

  private Order(Guid id, Guid userId, DateTimeOffset placedAt) : base(id) {
    UserId = Guard.NotEmpty(userId, "id_пользователя");
    PlacedAt = placedAt;
    Status = OrderStatus.Checkout;
  }

  public Guid UserId { get; private set; }

  /// <summary>Дата оформления.</summary>
  public DateTimeOffset PlacedAt { get; private set; }

  public OrderStatus Status { get; private set; }
  public DeliveryType? DeliveryType { get; private set; }
  public DeliveryAddress? DeliveryAddress { get; private set; }
  public PaymentMethod? PaymentMethod { get; private set; }

  /// <summary>Сумма заказа.</summary>
  public Money Total { get; private set; } = null!;

  /// <summary>Когда заказ перешёл в «Ожидает оплаты» — от этого момента считается таймаут оплаты.</summary>
  public DateTimeOffset? SubmittedAt { get; private set; }

  public DateTimeOffset? ConfirmedAt { get; private set; }
  public DateTimeOffset? CompletedAt { get; private set; }
  public DateTimeOffset? CancelledAt { get; private set; }
  public OrderCancellationReason? CancellationReason { get; private set; }

  public Delivery? Delivery { get; private set; }
  public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
  public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

  /// <summary>Подсостояние «Оформления»; null, если заказ уже оформлен.</summary>
  public CheckoutStep? CheckoutStep => Status != OrderStatus.Checkout
    ? null
    : DeliveryAddress is null
      ? OrderAggregate.CheckoutStep.DeliverySelection
      : OrderAggregate.CheckoutStep.PaymentSelection;

  public Payment? SucceededPayment => _payments.FirstOrDefault(p => p.Status == PaymentStatus.Succeeded);

  public IReadOnlyList<OrderLine> Lines => _items.Select(i => i.ToLine()).ToList();

  public int TotalQuantity => _items.Sum(i => i.Quantity);

  #region Оформление

  /// <summary>
  /// «Оформить заказ / создать заказ из корзины» (15.1). Повторяющиеся товары объединяются.
  /// Наличие и списание остатков — в приложении (по событию <see cref="OrderCreated"/> или в том же use case).
  /// </summary>
  public static Order Create(Guid userId, IEnumerable<OrderLine> lines, DateTimeOffset now) {
    var merged = lines
      .GroupBy(l => l.ProductId)
      .Select(g => {
        var price = g.First().UnitPrice;
        if (g.Any(l => l.UnitPrice != price))
          throw new DomainException("У одного товара в заказе не может быть разных цен.");
        return new OrderLine(g.Key, price, g.Sum(l => Guard.Positive(l.Quantity, "Количество")));
      })
      .ToList();

    if (merged.Count == 0) throw new DomainException("Заказ должен содержать хотя бы один товар.");

    var order = new Order(Guid.NewGuid(), userId, now);
    foreach (var line in merged) order._items.Add(new OrderItem(line.ProductId, line.UnitPrice, line.Quantity));
    order.Total = order._items.Aggregate(Money.Zero(merged[0].UnitPrice.Currency), (sum, i) => sum + i.Subtotal);

    order.Raise(new OrderCreated(order.Id, userId, order.Total, order.Lines));
    return order;
  }

  /// <summary>Создаёт заказ из корзины по текущим ценам товаров. Корзина очищается позже, при подтверждении.</summary>
  public static Order CreateFromCart(Cart cart, Func<Guid, Money> priceOf, DateTimeOffset now) {
    if (cart.IsEmpty) throw new DomainException("Корзина пуста.");
    return Create(cart.UserId, cart.Items.Select(i => new OrderLine(i.ProductId, priceOf(i.ProductId), i.Quantity)), now);
  }

  /// <summary>«Выбор доставки»: указать тип доставки и адрес (15.2). Можно менять до подтверждения покупателем.</summary>
  public void SetDelivery(DeliveryType type, DeliveryAddress address) {
    EnsureStatus(OrderStatus.Checkout, "Доставку можно изменить только при оформлении заказа.");
    DeliveryType = type;
    DeliveryAddress = address;
  }

  /// <summary>«Выбор способа оплаты» (15.3). Доступен после выбора доставки.</summary>
  public void SetPaymentMethod(PaymentMethod method) {
    EnsureStatus(OrderStatus.Checkout, "Способ оплаты можно изменить только при оформлении заказа.");
    if (DeliveryAddress is null) throw new DomainException("Сначала укажите тип доставки и адрес.");
    PaymentMethod = method;
  }

  /// <summary>«Способ оплаты выбран, заказ подтверждён покупателем» → Ожидает оплаты.</summary>
  public void Submit(DateTimeOffset now) {
    EnsureStatus(OrderStatus.Checkout, "Заказ уже оформлен.");
    if (DeliveryType is null || DeliveryAddress is null) throw new DomainException("Не указаны тип доставки и адрес.");
    if (PaymentMethod is null) throw new DomainException("Не выбран способ оплаты.");

    TransitionTo(OrderStatus.AwaitingPayment);
    SubmittedAt = now;
    Raise(new OrderSubmitted(Id, UserId, Total, PaymentMethod.Value));
  }

  #endregion

  #region Оплата

  public bool IsPaymentExpired(DateTimeOffset now) =>
    Status == OrderStatus.AwaitingPayment && SubmittedAt is not null && now - SubmittedAt.Value >= PaymentTimeout;

  /// <summary>«Оплата прошла [статус_оплаты = успешно] / сохранить платёж» (15.4) → Подтверждён.</summary>
  public Payment MarkPaid(string transactionId, DateTimeOffset now) {
    EnsureStatus(OrderStatus.AwaitingPayment, "Заказ не ожидает оплаты.");
    var txId = Guard.NotEmpty(transactionId, "id_транзакции", 128);

    var payment = new Payment(Total, PaymentMethod!.Value, PaymentStatus.Succeeded, txId, now);
    _payments.Add(payment);
    Raise(new OrderPaymentSucceeded(Id, payment.Id, txId, payment.Amount));

    TransitionTo(OrderStatus.Confirmed);
    ConfirmedAt = now;
    Raise(new OrderConfirmed(Id, UserId));
    return payment;
  }

  /// <summary>«Оплата отклонена / уведомить покупателя» — заказ остаётся в «Ожидает оплаты».</summary>
  public Payment DeclinePayment(string? transactionId, string? reason, DateTimeOffset now) {
    EnsureStatus(OrderStatus.AwaitingPayment, "Заказ не ожидает оплаты.");
    OrderStatusTransitions.EnsureCanTransition(Status, OrderStatus.AwaitingPayment);

    var payment = new Payment(Total, PaymentMethod!.Value, PaymentStatus.Declined, transactionId, now, reason);
    _payments.Add(payment);
    Raise(new OrderPaymentDeclined(Id, UserId, payment.Id, payment.FailureReason));
    return payment;
  }

  #endregion

  #region Доставка

  /// <summary>«Логист назначил курьера / зарегистрировать доставку» (13.3–13.4) → Курьер назначен.</summary>
  public void AssignCourier(Guid courierId, DateTimeOffset now) {
    EnsureStatus(OrderStatus.Confirmed, "Курьера можно назначить только подтверждённому заказу.");
    Guard.NotEmpty(courierId, "id_курьера");

    TransitionTo(OrderStatus.CourierAssigned);
    if (Delivery is null) Delivery = new Delivery(Id, courierId, now);
    else Delivery.Reassign(courierId, now);

    Raise(new OrderCourierAssigned(Id, courierId));
  }

  /// <summary>«Курьер принял заказ / статус доставки «принят»» (12.2) → В доставке.</summary>
  public void AcceptByCourier(Guid courierId, DateTimeOffset now) {
    EnsureStatus(OrderStatus.CourierAssigned, "Заказ не ожидает курьера.");
    EnsureAssignedCourier(courierId);

    TransitionTo(OrderStatus.InDelivery);
    Delivery!.Accept(now);
    Raise(new OrderInDelivery(Id, UserId, courierId));
  }

  /// <summary>«Курьер подтвердил вручение / статус доставки «вручён»» (12.3) → Завершён.</summary>
  public void ConfirmDelivered(Guid courierId, DateTimeOffset now) {
    EnsureStatus(OrderStatus.InDelivery, "Заказ не находится в доставке.");
    EnsureAssignedCourier(courierId);

    TransitionTo(OrderStatus.Completed);
    Delivery!.MarkDelivered(now);
    CompletedAt = now;
    Raise(new OrderCompleted(Id, UserId));
  }

  /// <summary>«Вручение не состоялось / снять назначение курьера» → Подтверждён (ждёт нового курьера).</summary>
  public void ReportDeliveryFailed(Guid courierId, string? reason) {
    EnsureStatus(OrderStatus.InDelivery, "Заказ не находится в доставке.");
    EnsureAssignedCourier(courierId);

    TransitionTo(OrderStatus.Confirmed);
    Delivery!.MarkFailed(reason);
    Raise(new OrderDeliveryFailed(Id, courierId, Delivery.FailureReason));
  }

  #endregion

  #region Отмена

  /// <summary>
  /// Отмена заказа. Из «Ожидает оплаты» платёж аннулируется, из «Подтверждён» и «Курьер назначен» —
  /// оплата возвращается (и отменяется доставка). После вручения отмена невозможна.
  /// </summary>
  public void Cancel(DateTimeOffset now, OrderCancellationReason reason = OrderCancellationReason.CustomerCancelled) {
    if (reason == OrderCancellationReason.PaymentTimeout && Status != OrderStatus.AwaitingPayment)
      throw new DomainException("Отмена по таймауту оплаты возможна только для заказа, ожидающего оплаты.");
    if (Status == OrderStatus.Checkout && reason == OrderCancellationReason.CustomerCancelled)
      reason = OrderCancellationReason.CustomerRefusedCheckout;

    var previous = Status;
    TransitionTo(OrderStatus.Cancelled);

    var refundRequired = false;
    if (previous is OrderStatus.Confirmed or OrderStatus.CourierAssigned && SucceededPayment is { } paid) {
      paid.MarkRefunded();
      refundRequired = true;
    }

    if (previous == OrderStatus.CourierAssigned) Delivery?.Cancel();

    CancelledAt = now;
    CancellationReason = reason;
    Raise(new OrderCancelled(
      Id,
      UserId,
      previous,
      reason,
      PaymentVoidRequired: previous == OrderStatus.AwaitingPayment,
      RefundRequired: refundRequired,
      Items: Lines));
  }

  /// <summary>after(30 мин): отменяет заказ, если оплата не поступила вовремя. Возвращает true, если отменён.</summary>
  public bool CancelIfPaymentExpired(DateTimeOffset now) {
    if (!IsPaymentExpired(now)) return false;
    Cancel(now, OrderCancellationReason.PaymentTimeout);
    return true;
  }

  #endregion

  private void TransitionTo(OrderStatus next) {
    OrderStatusTransitions.EnsureCanTransition(Status, next);
    var previous = Status;
    Status = next;
    Raise(new OrderStatusChanged(Id, UserId, previous, next));
  }

  private void EnsureStatus(OrderStatus expected, string message) {
    if (Status != expected) throw new DomainException(message);
  }

  private void EnsureAssignedCourier(Guid courierId) {
    if (Delivery is null || Delivery.CourierId != courierId)
      throw new DomainException("Заказ назначен другому курьеру.");
  }
}
