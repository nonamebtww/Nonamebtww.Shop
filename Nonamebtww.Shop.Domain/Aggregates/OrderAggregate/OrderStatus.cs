namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Статус заказа (IDEF1X «Заказ.статус_заказа») — состояния с диаграммы состояний «Заказ».
/// Допустимые переходы — <see cref="OrderStatusTransitions"/>.
/// </summary>
public enum OrderStatus {
  /// <summary>Оформление: выбор доставки и способа оплаты (подсостояние — <see cref="Order.CheckoutStep"/>).</summary>
  Checkout = 0,

  /// <summary>Ожидает оплаты.</summary>
  AwaitingPayment = 1,

  /// <summary>Подтверждён (оплачен).</summary>
  Confirmed = 2,

  /// <summary>Курьер назначен.</summary>
  CourierAssigned = 3,

  /// <summary>В доставке.</summary>
  InDelivery = 4,

  /// <summary>Завершён (вручён).</summary>
  Completed = 5,

  /// <summary>Отменён.</summary>
  Cancelled = 6,
}

/// <summary>Подсостояния «Оформления».</summary>
public enum CheckoutStep {
  /// <summary>Выбор доставки: указать тип доставки и адрес (15.2).</summary>
  DeliverySelection = 0,

  /// <summary>Выбор способа оплаты (15.3).</summary>
  PaymentSelection = 1,
}

public static class OrderStatusExtensions {
  public static string ToDisplayName(this OrderStatus status) => status switch {
    OrderStatus.Checkout => "Оформление",
    OrderStatus.AwaitingPayment => "Ожидает оплаты",
    OrderStatus.Confirmed => "Подтверждён",
    OrderStatus.CourierAssigned => "Курьер назначен",
    OrderStatus.InDelivery => "В доставке",
    OrderStatus.Completed => "Завершён",
    OrderStatus.Cancelled => "Отменён",
    _ => status.ToString(),
  };

  /// <summary>Активный заказ (14.2).</summary>
  public static bool IsActive(this OrderStatus status) => !status.IsFinished();

  /// <summary>Завершённый заказ (14.3): вручён или отменён.</summary>
  public static bool IsFinished(this OrderStatus status) => status is OrderStatus.Completed or OrderStatus.Cancelled;

  /// <summary>Покупатель может отменить заказ в этом состоянии.</summary>
  public static bool IsCancellable(this OrderStatus status) =>
    OrderStatusTransitions.CanTransition(status, OrderStatus.Cancelled);
}
