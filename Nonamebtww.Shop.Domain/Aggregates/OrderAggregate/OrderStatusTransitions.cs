using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>Таблица переходов по диаграмме состояний «Заказ».</summary>
public static class OrderStatusTransitions {
  private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Allowed =
    new Dictionary<OrderStatus, OrderStatus[]> {
      // способ оплаты выбран, заказ подтверждён покупателем | покупатель отказался от оформления
      [OrderStatus.Checkout] = [OrderStatus.AwaitingPayment, OrderStatus.Cancelled],

      // оплата прошла | оплата отклонена (остаёмся) | отмена покупателем или after(30 мин)
      [OrderStatus.AwaitingPayment] = [OrderStatus.Confirmed, OrderStatus.AwaitingPayment, OrderStatus.Cancelled],

      // логист назначил курьера | отмена покупателем (возврат оплаты)
      [OrderStatus.Confirmed] = [OrderStatus.CourierAssigned, OrderStatus.Cancelled],

      // курьер принял заказ | отмена покупателем (возврат оплаты, отмена доставки)
      [OrderStatus.CourierAssigned] = [OrderStatus.InDelivery, OrderStatus.Cancelled],

      // курьер подтвердил вручение | вручение не состоялось
      [OrderStatus.InDelivery] = [OrderStatus.Completed, OrderStatus.Confirmed],

      [OrderStatus.Completed] = [],
      [OrderStatus.Cancelled] = [],
    };

  public static bool CanTransition(OrderStatus from, OrderStatus to) =>
    Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

  public static IReadOnlyList<OrderStatus> AllowedFrom(OrderStatus from) =>
    Allowed.TryGetValue(from, out var targets) ? targets : [];

  public static void EnsureCanTransition(OrderStatus from, OrderStatus to) {
    if (!CanTransition(from, to)) throw new OrderStatusTransitionException(from, to);
  }
}
