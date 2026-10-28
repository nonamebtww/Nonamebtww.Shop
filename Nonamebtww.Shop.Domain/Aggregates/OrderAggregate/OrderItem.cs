using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>Позиция заказа (IDEF1X «Позиция заказа»). Цена фиксируется на момент оформления.</summary>
public class OrderItem : Entity<Guid> {
  private OrderItem() { }

  internal OrderItem(Guid productId, Money unitPrice, int quantity) : base(Guid.NewGuid()) {
    ProductId = Guard.NotEmpty(productId, "id_товара");
    UnitPrice = unitPrice;
    Quantity = Guard.Positive(quantity, "Количество");
  }

  public Guid ProductId { get; private set; }
  public int Quantity { get; private set; }

  /// <summary>Цена за единицу на момент оформления.</summary>
  public Money UnitPrice { get; private set; } = null!;

  public Money Subtotal => UnitPrice * Quantity;

  public OrderLine ToLine() => new(ProductId, UnitPrice, Quantity);
}
