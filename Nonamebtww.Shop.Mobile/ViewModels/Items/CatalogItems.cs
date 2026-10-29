using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Mobile.Presentation;

namespace Nonamebtww.Shop.Mobile.ViewModels.Items;

/// <summary>Category chip in the catalog. Id = null means «all categories».</summary>
public sealed partial class CategoryChip(Guid? id, string name, IReadOnlyList<CategoryNode> children) : ReactiveObject {
  public Guid? Id { get; } = id;
  public string Name { get; } = name;
  public IReadOnlyList<CategoryNode> Children { get; } = children;

  [Reactive]
  public partial bool IsSelected { get; set; }

  public static CategoryChip From(CategoryNode node) => new(node.Id, node.Name, node.Children);
}

/// <summary>Product tile used by the catalog and favorites lists.</summary>
public sealed partial class ProductCardItem : ReactiveObject {
  public ProductCardItem(ProductSimple product) {
    Id = product.Id;
    Name = product.Name;
    Price = product.Price;
    Photo = ImageSources.Photo(product.Photo);
    Stock = product.Stock;
    RatingText = product.Rating is null
      ? "нет отзывов"
      : $"{DisplayText.Stars(product.Rating)} · {product.ReviewsCount} отз.";
    IsFavorite = product.IsFavorite;
  }

  public Guid Id { get; }
  public string Name { get; }
  public decimal Price { get; }
  public string Photo { get; }
  public int Stock { get; }
  public string RatingText { get; }

  public bool InStock => Stock > 0;
  public string PriceText => DisplayText.Money(Price);
  public string StockText => InStock ? $"В наличии: {Stock} шт." : "Нет в наличии";
  public string CartButtonText => InStock ? "В корзину" : "Нет в наличии";

  [Reactive]
  public partial bool IsFavorite { get; set; }
}

public sealed record SpecItem(string Name, string Value);

public sealed record ReviewItem(string Author, string Stars, string? Text, string Date) {
  public static ReviewItem From(ReviewDto review) =>
    new(review.AuthorName, DisplayText.Stars(review.Rating), review.Text, DisplayText.Date(review.CreatedAt));
}
