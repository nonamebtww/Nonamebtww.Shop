namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;

/// <param name="ParentId">null вместе с RootsOnly = true — только корневые категории.</param>
public record struct CategorySearch(
  string? Query = null,
  Guid? ParentId = null,
  bool RootsOnly = false,
  int Page = 1,
  int PageSize = 100);
