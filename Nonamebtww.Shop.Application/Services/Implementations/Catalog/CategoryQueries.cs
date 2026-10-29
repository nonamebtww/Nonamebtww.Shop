using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;

namespace Nonamebtww.Shop.Application.Services.Implementations.Catalog;

/// <summary>Просмотр каталога (DFD 7): категории.</summary>
public sealed class CategoryQueries(
  ICurrentUserService currentUser,
  ICategoriesRepository categories,
  IProductsRepository products,
  IReviewsRepository reviews,
  IFavoritesRepository favorites) : ApplicationService(currentUser), ICategoryQueries {

  public Task<Result<IReadOnlyList<CategoryNode>>> GetTreeAsync(CancellationToken ct = default) =>
    Run<IReadOnlyList<CategoryNode>>(async () => {
      var snapshot = await LoadAsync(ct);
      return Result.Success(BuildLevel(snapshot, null, []));
    });

  public Task<Result<PagedList<CategorySimple>>> SearchAsync(CategorySearch search, CancellationToken ct = default) =>
    Run<PagedList<CategorySimple>>(async () => {
      var snapshot = await LoadAsync(ct);
      IEnumerable<Category> query = snapshot.Categories;

      if (search.ParentId is { } parentId) {
        query = query.Where(c => c.ParentId == parentId);
      } else if (search.RootsOnly) {
        query = query.Where(c => c.IsRoot);
      }

      if (!string.IsNullOrWhiteSpace(search.Query)) {
        query = query.Where(c => c.Name.Contains(search.Query.Trim(), StringComparison.OrdinalIgnoreCase));
      }

      var list = query.OrderBy(c => c.Name).ToList();
      var (page, size) = Paging.Normalize(search.Page, search.PageSize, 100);
      var items = list.Skip((page - 1) * size).Take(size)
        .Select(c => c.ToSimple(snapshot.ProductCount(c.Id)))
        .ToList();

      return new PagedList<CategorySimple>(items, page, size, list.Count);
    });

  public Task<Result<CategoryDetailed>> GetAsync(Guid id, CancellationToken ct = default) =>
    Run<CategoryDetailed>(async () => {
      var snapshot = await LoadAsync(ct);
      var category = snapshot.FindCategory(id);
      if (category is null) {
        return Errors.NotFound("category");
      }

      return ToDetailed(snapshot, category);
    });

  internal static CategoryDetailed ToDetailed(CatalogSnapshot snapshot, Category category) =>
    new(category.Id, category.ParentId, category.Name, category.Description, category.Photo,
      snapshot.ChildrenOf(category.Id).Select(c => c.ToSimple(snapshot.ProductCount(c.Id))).ToList(),
      snapshot.PathTo(category).Select(c => c.ToSimple(snapshot.ProductCount(c.Id))).ToList(),
      snapshot.ProductCount(category.Id));

  private Task<CatalogSnapshot> LoadAsync(CancellationToken ct) =>
    CatalogSnapshot.LoadAsync(categories, products, reviews, favorites, null, includeInactive: false, ct);

  private static IReadOnlyList<CategoryNode> BuildLevel(CatalogSnapshot snapshot, Guid? parentId, HashSet<Guid> visited) =>
    snapshot.ChildrenOf(parentId)
      .Where(c => visited.Add(c.Id))
      .Select(c => new CategoryNode(c.Id, c.Name, c.Photo, snapshot.ProductCount(c.Id), BuildLevel(snapshot, c.Id, visited)))
      .ToList();
}
