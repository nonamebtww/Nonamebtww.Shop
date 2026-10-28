using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;

namespace Nonamebtww.Shop.Application.Models.DTOs.Favorites;

/// <summary>Элемент «Списка избранного».</summary>
public sealed record FavoriteItemDto(ProductSimple Product, DateTimeOffset AddedAt);
