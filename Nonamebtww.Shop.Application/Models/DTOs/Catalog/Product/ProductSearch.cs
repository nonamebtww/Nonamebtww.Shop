using Nonamebtww.Shop.Application.Models.Enums;

namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;

/// <summary>«Запрос, фильтры» процесса «Просмотр каталога».</summary>
public record struct ProductSearch(
  string? Query = null,
  Guid? CategoryId = null,
  bool IncludeSubcategories = true,
  decimal? MinPrice = null,
  decimal? MaxPrice = null,
  bool OnlyInStock = false,
  ProductSort Sort = ProductSort.Default,
  int Page = 1,
  int PageSize = 20,
  bool IncludeInactive = false);
