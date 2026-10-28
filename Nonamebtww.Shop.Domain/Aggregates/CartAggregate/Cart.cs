using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.CartAggregate;

/// <summary>
/// Корзина покупателя: одна на пользователя. Цены в корзине не хранятся —
/// актуальная сумма считается по текущим ценам товаров (<see cref="CalculateTotal"/>).
/// Наличие на складе проверяет приложение.
/// </summary>
public class Cart : AggregateRoot<Guid> {
  public const int MaxQuantityPerItem = 99;

  private readonly List<CartItem> _items = [];

  private Cart() { }

  private Cart(Guid id, Guid userId) : base(id) {
    UserId = Guard.NotEmpty(userId, "id_пользователя");
  }

  public Guid UserId { get; private set; }
  public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

  public bool IsEmpty => _items.Count == 0;
  public int TotalQuantity => _items.Sum(i => i.Quantity);

  public static Cart CreateFor(Guid userId) => new(Guid.NewGuid(), userId);

  public bool Contains(Guid productId) => _items.Any(i => i.ProductId == productId);

  /// <summary>Добавляет товар; если он уже в корзине — увеличивает количество.</summary>
  public CartItem AddItem(Guid productId, int quantity, DateTimeOffset now) {
    Guard.Positive(quantity, "Количество");

    var item = _items.FirstOrDefault(i => i.ProductId == productId);
    if (item is null) {
      item = new CartItem(productId, ValidateQuantity(quantity), now);
      _items.Add(item);
    } else {
      item.SetQuantity(ValidateQuantity(item.Quantity + quantity));
    }

    return item;
  }

  /// <summary>Устанавливает количество; 0 удаляет позицию.</summary>
  public void ChangeQuantity(Guid productId, int quantity) {
    Guard.NotNegative(quantity, "Количество");

    var item = FindItem(productId);
    if (quantity == 0) {
      _items.Remove(item);
      return;
    }

    item.SetQuantity(ValidateQuantity(quantity));
  }

  public void RemoveItem(Guid productId) => _items.Remove(FindItem(productId));

  public void Clear() => _items.Clear();

  /// <summary>Сумма корзины по текущим ценам товаров.</summary>
  public Money CalculateTotal(Func<Guid, Money> priceOf) =>
    _items.Aggregate(Money.Zero(), (total, item) => total + priceOf(item.ProductId) * item.Quantity);

  private CartItem FindItem(Guid productId) =>
    _items.FirstOrDefault(i => i.ProductId == productId) ?? throw new DomainException("Товара нет в корзине.");

  private static int ValidateQuantity(int quantity) {
    if (quantity > MaxQuantityPerItem)
      throw new DomainException($"Нельзя добавить больше {MaxQuantityPerItem} шт. одного товара.");
    return quantity;
  }
}
