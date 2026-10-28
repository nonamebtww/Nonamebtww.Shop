using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;

/// <summary>Товар в избранном (IDEF1X «Избранное»), часть агрегата <see cref="Favorites"/>.</summary>
public class FavoriteItem : Entity<Guid> {
  private FavoriteItem() { }

  internal FavoriteItem(Guid productId, DateTimeOffset addedAt) : base(Guid.NewGuid()) {
    ProductId = Guard.NotEmpty(productId, "id_товара");
    AddedAt = addedAt;
  }

  public Guid ProductId { get; private set; }

  /// <summary>Дата добавления.</summary>
  public DateTimeOffset AddedAt { get; private set; }
}
