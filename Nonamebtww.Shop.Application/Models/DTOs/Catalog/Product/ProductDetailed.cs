using Nonamebtww.Shop.Application.Models.DTOs.Abstractions.Interfaces;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;

namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;

/// <summary>«Карточка товара» (Просмотр товара).</summary>
public record struct ProductDetailed(
  Guid Id,
  Guid CategoryId,
  string CategoryName,
  string Sku,
  string Name,
  string? Description,
  IReadOnlyDictionary<string, string> Characteristics,
  decimal Price,
  int Stock,
  string? Photo,
  bool IsActive,
  double? Rating,
  IReadOnlyList<ReviewDto> Reviews,
  bool IsFavorite,
  int InCartQuantity,
  bool CanReview,
  bool IsFresh = true) : IResponseDetailed {
  public readonly bool InStock => Stock > 0;
}
