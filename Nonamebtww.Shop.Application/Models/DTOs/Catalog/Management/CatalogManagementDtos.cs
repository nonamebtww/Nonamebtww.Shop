namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;

/// <summary>«Изменения каталога»: категория. Id = null — создание.</summary>
public sealed record SaveCategoryRequest(
  Guid? Id,
  Guid? ParentId,
  string Name,
  string? Description,
  string? Photo);

/// <summary>«Изменения каталога»: товар. Id = null — создание.</summary>
public sealed record SaveProductRequest(
  Guid? Id,
  Guid CategoryId,
  string Sku,
  string Name,
  string? Description,
  IReadOnlyDictionary<string, string>? Characteristics,
  decimal Price,
  int Stock,
  string? Photo,
  bool IsActive = true);
