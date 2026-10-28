using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>Строка для создания заказа и для событий: товар, цена за штуку, количество.</summary>
public sealed record OrderLine(Guid ProductId, Money UnitPrice, int Quantity) {
  public Money Subtotal => UnitPrice * Quantity;
}
