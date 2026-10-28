using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate.Events;
using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;

/// <summary>Товар (IDEF1X «Товар»).</summary>
public class Product : AggregateRoot<Guid>, IHasDetails {
  private Product() { }

  private Product(
    Guid id,
    Guid categoryId,
    Sku sku,
    string name,
    Money price,
    int stock,
    string? description,
    string? characteristics,
    string? photo) : base(id) {
    CategoryId = Guard.NotEmpty(categoryId, "id_категории");
    Sku = sku;
    Price = price;
    Stock = Guard.NotNegative(stock, "Остаток");
    IsActive = true;
    SetDetails(name, description, characteristics, photo);
  }

  public Guid CategoryId { get; private set; }

  /// <summary>Артикул (AK1).</summary>
  public Sku Sku { get; private set; } = null!;

  public string Name { get; private set; } = null!;
  public string? Description { get; private set; }

  /// <summary>Характеристики в свободной форме (например, «Цвет: чёрный; Вес: 1,2 кг»).</summary>
  public string? Characteristics { get; private set; }

  public Money Price { get; private set; } = null!;

  /// <summary>Остаток на складе, шт.</summary>
  public int Stock { get; private set; }

  public string? Photo { get; private set; }

  /// <summary>Показывается ли товар в каталоге.</summary>
  public bool IsActive { get; private set; }

  public bool IsAvailable => IsActive && Stock > 0;

  public static Product Create(
    Guid categoryId,
    Sku sku,
    string name,
    Money price,
    int stock = 0,
    string? description = null,
    string? characteristics = null,
    string? photo = null) {
    var product = new Product(Guid.NewGuid(), categoryId, sku, name, price, stock, description, characteristics, photo);
    product.Raise(new ProductCreated(product.Id, categoryId, sku.Value, product.Name));
    return product;
  }

  public bool HasEnough(int quantity) => quantity > 0 && Stock >= quantity;

  public void UpdateDetails(string name, string? description, string? characteristics, string? photo) =>
    SetDetails(name, description, characteristics, photo);

  public void ChangeSku(Sku sku) => Sku = sku;

  public void ChangePrice(Money price) {
    if (Price == price) return;

    var old = Price;
    Price = price;
    Raise(new ProductPriceChanged(Id, old, price));
  }

  public void MoveToCategory(Guid categoryId) => CategoryId = Guard.NotEmpty(categoryId, "id_категории");

  /// <summary>Поступление на склад или возврат товара из отменённого заказа.</summary>
  public void IncreaseStock(int quantity) {
    Guard.Positive(quantity, "Количество");
    SetStock(Stock + quantity);
  }

  /// <summary>Списание остатка под заказ.</summary>
  public void DecreaseStock(int quantity) {
    Guard.Positive(quantity, "Количество");
    if (!IsActive) throw new DomainException($"Товар «{Name}» снят с продажи.");
    if (Stock < quantity) throw new DomainException($"Недостаточно товара «{Name}» на складе: доступно {Stock} шт.");
    SetStock(Stock - quantity);
  }

  /// <summary>Инвентаризация: установить точный остаток.</summary>
  public void SetStock(int stock) {
    Guard.NotNegative(stock, "Остаток");
    if (Stock == stock) return;

    var old = Stock;
    Stock = stock;
    Raise(new ProductStockChanged(Id, old, stock));
    if (stock == 0) Raise(new ProductOutOfStock(Id));
  }

  public void Activate() {
    if (IsActive) return;
    IsActive = true;
    Raise(new ProductActivated(Id));
  }

  public void Deactivate() {
    if (!IsActive) return;
    IsActive = false;
    Raise(new ProductDeactivated(Id));
  }

  private void SetDetails(string name, string? description, string? characteristics, string? photo) {
    Name = Guard.NotEmpty(name, "Название товара", 256);
    Description = Guard.Optional(description, "Описание товара", 4096);
    Characteristics = Guard.Optional(characteristics, "Характеристики", 4096);
    Photo = Guard.Optional(photo, "Фото товара", 1024);
  }
}
