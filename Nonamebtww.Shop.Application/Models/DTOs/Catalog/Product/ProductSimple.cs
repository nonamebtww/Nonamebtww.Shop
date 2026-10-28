using Nonamebtww.Shop.Application.Models.DTOs.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;

/// <summary>Карточка товара в списке каталога.</summary>
public record struct ProductSimple(
  Guid Id,
  Guid CategoryId,
  string Sku,
  string Name,
  decimal Price,
  string? Photo,
  int Stock,
  bool IsActive,
  double? Rating,
  int ReviewsCount,
  bool IsFavorite,
  bool IsFresh = true) : IResponseSimple {
  public readonly bool InStock => Stock > 0;
}
