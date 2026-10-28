using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Domain.Exceptions;

/// <summary>Переход заказа, которого нет на диаграмме состояний.</summary>
public class OrderStatusTransitionException : DomainException {
  public OrderStatusTransitionException(OrderStatus from, OrderStatus to)
    : base($"Недопустимый переход заказа из состояния «{from.ToDisplayName()}» в «{to.ToDisplayName()}».") {
    From = from;
    To = to;
  }

  public OrderStatus From { get; }
  public OrderStatus To { get; }
}
