using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;

/// <summary>Список избранных товаров покупателя (IDEF1X «Избранное»): один на пользователя.</summary>
public class Favorites : AggregateRoot<Guid> {
  private readonly List<FavoriteItem> _items = [];

  private Favorites() { }

  private Favorites(Guid id, Guid userId) : base(id) {
    UserId = Guard.NotEmpty(userId, "id_пользователя");
  }

  public Guid UserId { get; private set; }
  public IReadOnlyCollection<FavoriteItem> Items => _items.AsReadOnly();

  public static Favorites CreateFor(Guid userId) => new(Guid.NewGuid(), userId);

  public bool Contains(Guid productId) => _items.Any(i => i.ProductId == productId);

  /// <summary>Добавляет товар. Возвращает false, если он уже в избранном.</summary>
  public bool Add(Guid productId, DateTimeOffset now) {
    if (Contains(productId)) return false;
    _items.Add(new FavoriteItem(productId, now));
    return true;
  }

  /// <summary>Убирает товар. Возвращает false, если его не было.</summary>
  public bool Remove(Guid productId) => _items.RemoveAll(i => i.ProductId == productId) > 0;

  /// <summary>Переключает отметку. Возвращает true, если товар теперь в избранном.</summary>
  public bool Toggle(Guid productId, DateTimeOffset now) {
    if (Remove(productId)) return false;
    return Add(productId, now);
  }

  public void Clear() => _items.Clear();
}
