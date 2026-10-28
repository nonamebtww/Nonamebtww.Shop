using Nonamebtww.Shop.Application.Models.DTOs.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;

/// <param name="Path">Цепочка родителей от корня (хлебные крошки), без самой категории.</param>
public record struct CategoryDetailed(
  Guid Id,
  Guid? ParentId,
  string Name,
  string? Description,
  string? Photo,
  IReadOnlyList<CategorySimple> Children,
  IReadOnlyList<CategorySimple> Path,
  int ProductCount,
  bool IsFresh = true) : IResponseDetailed;
