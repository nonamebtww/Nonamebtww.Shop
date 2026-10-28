namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;

/// <summary>Узел дерева категорий.</summary>
public sealed record CategoryNode(Guid Id, string Name, string? Photo, int ProductCount, IReadOnlyList<CategoryNode> Children);
