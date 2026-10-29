using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Application.Services.Implementations.Catalog;

/// <summary>Просмотр каталога (DFD 7): поиск товаров; Просмотр товара (DFD 8): карточка с отзывами.</summary>
public sealed class ProductQueries(
  ICurrentUserService currentUser,
  ICategoriesRepository categories,
  IProductsRepository products,
  IReviewsRepository reviews,
  IFavoritesRepository favorites,
  ICartsRepository carts,
  IUsersRepository users) : ApplicationService(currentUser), IProductQueries {

  public Task<Result<PagedList<ProductSimple>>> SearchAsync(ProductSearch search, CancellationToken ct = default) =>
    Run<PagedList<ProductSimple>>(async () => {
      var includeInactive = search.IncludeInactive && CanManageCatalog;
      var snapshot = await CatalogSnapshot.LoadAsync(categories, products, reviews, favorites, ShopperId, includeInactive, ct);
      IEnumerable<Product> query = snapshot.Products;

      if (search.CategoryId is { } categoryId) {
        var ids = search.IncludeSubcategories ? snapshot.WithDescendants(categoryId) : [categoryId];
        query = query.Where(p => ids.Contains(p.CategoryId));
      }

      if (!string.IsNullOrWhiteSpace(search.Query)) {
        var text = search.Query.Trim();
        query = query.Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
          || p.Sku.Value.Contains(text, StringComparison.OrdinalIgnoreCase)
          || (p.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
      }

      if (search.MinPrice is { } min) {
        query = query.Where(p => p.Price.Amount >= min);
      }

      if (search.MaxPrice is { } max) {
        query = query.Where(p => p.Price.Amount <= max);
      }

      if (search.OnlyInStock) {
        query = query.Where(p => p.IsAvailable);
      }

      query = search.Sort switch {
        ProductSort.PriceAscending => query.OrderBy(p => p.Price.Amount),
        ProductSort.PriceDescending => query.OrderByDescending(p => p.Price.Amount),
        ProductSort.NameAscending => query.OrderBy(p => p.Name),
        ProductSort.RatingDescending => query.OrderByDescending(p => snapshot.RatingOf(p.Id).Average ?? 0)
          .ThenByDescending(p => snapshot.RatingOf(p.Id).Count),
        _ => query.OrderByDescending(p => p.IsAvailable).ThenBy(p => p.Name)
      };

      var list = query.ToList();
      var (page, size) = Paging.Normalize(search.Page, search.PageSize);
      var items = Paging.Slice(list, page, size)
        .Select(p => p.ToSimple(snapshot.RatingOf(p.Id), snapshot.IsFavorite(p.Id)))
        .ToList();

      return new PagedList<ProductSimple>(items, page, size, list.Count);
    });

  public Task<Result<ProductDetailed>> GetAsync(Guid id, CancellationToken ct = default) =>
    Run<ProductDetailed>(async () => {
      var product = await products.GetByIdAsync(id, ct);
      if (product is null || (!product.IsActive && !CanManageCatalog)) {
        return Errors.NotFound("product");
      }

      return await BuildDetailedAsync(product, ct);
    });

  internal async Task<ProductDetailed> BuildDetailedAsync(Product product, CancellationToken ct) {
    var category = await categories.GetByIdAsync(product.CategoryId, ct);
    var productReviews = (await reviews.GetByProductAsync(product.Id, ct))
      .OrderByDescending(r => r.EditedAt ?? r.CreatedAt)
      .ToList();

    var authors = new Dictionary<Guid, string>();
    foreach (var authorId in productReviews.Select(r => r.UserId).Distinct()) {
      authors[authorId] = (await users.GetByIdAsync(authorId, ct))?.FullName ?? "Покупатель";
    }

    var isFavorite = false;
    var inCart = 0;

    if (ShopperId is { } userId) {
      isFavorite = (await favorites.GetByUserIdAsync(userId, ct))?.Contains(product.Id) ?? false;
      inCart = (await carts.GetByUserIdAsync(userId, ct))?.Items.FirstOrDefault(i => i.ProductId == product.Id)?.Quantity ?? 0;
    }

    var rating = RatingInfo.From(productReviews);

    return new ProductDetailed(
      product.Id,
      product.CategoryId,
      category?.Name ?? string.Empty,
      product.Sku.Value,
      product.Name,
      product.Description,
      Characteristics.Parse(product.Characteristics),
      product.Price.Amount,
      product.Stock,
      product.Photo,
      product.IsActive,
      rating.Average,
      productReviews.Select(r => r.ToDto(authors[r.UserId])).ToList(),
      isFavorite,
      inCart,
      CanReview: ShopperId is not null);
  }

  /// <summary>Id покупателя: избранное, корзина и отзывы есть только у роли «Покупатель».</summary>
  private Guid? ShopperId => CurrentUser.Role is { } role && role.CanShop() ? CurrentUser.UserId : null;

  private bool CanManageCatalog => CurrentUser.Role is { } role && role.CanManageCatalog();
}
