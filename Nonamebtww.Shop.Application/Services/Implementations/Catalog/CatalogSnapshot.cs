using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using FavoritesList = Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate.Favorites;

namespace Nonamebtww.Shop.Application.Services.Implementations.Catalog;

/// <summary>
/// Срез каталога для запросов чтения. Прототип работает с небольшой локальной БД,
/// поэтому фильтрация и подсчёты выполняются в памяти.
/// </summary>
internal sealed class CatalogSnapshot {
  private readonly Dictionary<Guid, Category> _categories;
  private readonly ILookup<Guid?, Category> _children;

  private CatalogSnapshot(IReadOnlyList<Category> categories, IReadOnlyList<Product> products,
    IReadOnlyList<Review> reviews, FavoritesList? favorites, bool includeInactive) {
    _categories = categories.ToDictionary(c => c.Id);
    _children = categories.ToLookup(c => c.ParentId);
    Products = includeInactive ? products : products.Where(p => p.IsActive).ToList();
    Ratings = reviews.GroupBy(r => r.ProductId).ToDictionary(g => g.Key, g => RatingInfo.From(g));
    Favorites = favorites;
  }

  public IReadOnlyList<Product> Products { get; }
  public IReadOnlyDictionary<Guid, RatingInfo> Ratings { get; }
  public FavoritesList? Favorites { get; }
  public IEnumerable<Category> Categories => _categories.Values;

  public static async Task<CatalogSnapshot> LoadAsync(
    ICategoriesRepository categories,
    IProductsRepository products,
    IReviewsRepository reviews,
    IFavoritesRepository favorites,
    Guid? userId,
    bool includeInactive,
    CancellationToken ct) {
    var allCategories = await categories.GetAllAsync(ct);
    var allProducts = await products.GetAllAsync(ct);
    var allReviews = await reviews.GetAllAsync(ct);
    var userFavorites = userId is { } id ? await favorites.GetByUserIdAsync(id, ct) : null;
    return new CatalogSnapshot(allCategories, allProducts, allReviews, userFavorites, includeInactive);
  }

  public Category? FindCategory(Guid id) => _categories.GetValueOrDefault(id);

  public IEnumerable<Category> ChildrenOf(Guid? parentId) => _children[parentId].OrderBy(c => c.Name);

  /// <summary>Категория и все её потомки.</summary>
  public HashSet<Guid> WithDescendants(Guid categoryId) {
    var result = new HashSet<Guid>();
    var stack = new Stack<Guid>([categoryId]);

    while (stack.Count > 0) {
      var id = stack.Pop();
      if (!result.Add(id)) {
        continue;
      }

      foreach (var child in _children[id]) {
        stack.Push(child.Id);
      }
    }

    return result;
  }

  /// <summary>Цепочка родителей от корня, без самой категории.</summary>
  public IReadOnlyList<Category> PathTo(Category category) {
    var path = new List<Category>();
    var visited = new HashSet<Guid> { category.Id };
    var parentId = category.ParentId;

    while (parentId is { } id && FindCategory(id) is { } parent && visited.Add(id)) {
      path.Insert(0, parent);
      parentId = parent.ParentId;
    }

    return path;
  }

  /// <summary>Количество товаров в категории с учётом подкатегорий.</summary>
  public int ProductCount(Guid categoryId) {
    var ids = WithDescendants(categoryId);
    return Products.Count(p => ids.Contains(p.CategoryId));
  }

  public RatingInfo RatingOf(Guid productId) => Ratings.GetValueOrDefault(productId);

  public bool IsFavorite(Guid productId) => Favorites?.Contains(productId) ?? false;
}
