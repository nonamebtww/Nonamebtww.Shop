using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.CartAggregate;

/// <summary>Позиция корзины (IDEF1X «Позиция корзины»), часть агрегата <see cref="Cart"/>.</summary>
public class CartItem : Entity<Guid> {
  private CartItem() { }

  internal CartItem(Guid productId, int quantity, DateTimeOffset addedAt) : base(Guid.NewGuid()) {
    ProductId = Guard.NotEmpty(productId, "id_товара");
    Quantity = quantity;
    AddedAt = addedAt;
  }

  public Guid ProductId { get; private set; }
  public int Quantity { get; private set; }

  /// <summary>Дата добавления.</summary>
  public DateTimeOffset AddedAt { get; private set; }

  internal void SetQuantity(int quantity) => Quantity = quantity;
}
