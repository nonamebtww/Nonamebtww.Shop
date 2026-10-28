using Nonamebtww.Shop.Application.Models.DTOs.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;

public record struct CategorySimple(
  Guid Id,
  Guid? ParentId,
  string Name,
  string? Photo,
  int ProductCount,
  bool IsFresh = true) : IResponseSimple;
