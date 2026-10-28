using Microsoft.EntityFrameworkCore;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Repositories;

internal sealed class RolesRepository(ShopDbContext db) : EfRepository<Role>(db), IRolesRepository {
  public override async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await Set.OrderBy(x => x.Kind).ToListAsync(cancellationToken);

  public Task<Role?> GetByKindAsync(UserRole kind, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.Kind == kind, cancellationToken);
}

internal sealed class UsersRepository(ShopDbContext db) : EfRepository<User>(db), IUsersRepository {
  public override async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await Set.OrderBy(x => x.RegisteredAt).ToListAsync(cancellationToken);

  public Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.Login == login, cancellationToken);

  public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

  public Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);

  public async Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default) =>
    await Set.Where(x => x.Role == role).OrderBy(x => x.FirstName).ToListAsync(cancellationToken);
}

internal sealed class CategoriesRepository(ShopDbContext db) : EfRepository<Category>(db), ICategoriesRepository {
  public override async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await Set.OrderBy(x => x.Name).ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<Category>> GetRootsAsync(CancellationToken cancellationToken = default) =>
    await Set.Where(x => x.ParentId == null).OrderBy(x => x.Name).ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default) =>
    await Set.Where(x => x.ParentId == parentId).OrderBy(x => x.Name).ToListAsync(cancellationToken);
}

internal sealed class ProductsRepository(ShopDbContext db) : EfRepository<Product>(db), IProductsRepository {
  public override async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await Set.OrderBy(x => x.Name).ToListAsync(cancellationToken);

  public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);

  public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) {
    var list = ids.Distinct().ToList();
    return list.Count == 0 ? [] : await Set.Where(x => list.Contains(x.Id)).ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
    Guid categoryId,
    bool onlyActive = true,
    CancellationToken cancellationToken = default) =>
    await Set
      .Where(x => x.CategoryId == categoryId && (!onlyActive || x.IsActive))
      .OrderBy(x => x.Name)
      .ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<Product>> SearchAsync(
    string text,
    bool onlyActive = true,
    CancellationToken cancellationToken = default) {
    var query = Set.Where(x => !onlyActive || x.IsActive);

    if (string.IsNullOrWhiteSpace(text)) {
      return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    // SQLite LIKE/lower() не понимают кириллицу без учёта регистра,
    // поэтому для прототипа фильтруем на стороне клиента: каталог небольшой.
    var term = text.Trim();
    var products = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

    return products
      .Where(x => Contains(x.Name, term)
                  || Contains(x.Sku.Value, term)
                  || Contains(x.Description, term)
                  || Contains(x.Characteristics, term))
      .ToList();
  }

  private static bool Contains(string? source, string term) =>
    source is not null && source.Contains(term, StringComparison.CurrentCultureIgnoreCase);
}

internal sealed class ReviewsRepository(ShopDbContext db) : EfRepository<Review>(db), IReviewsRepository {
  public async Task<IReadOnlyList<Review>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
    await Set.Where(x => x.ProductId == productId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<Review>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
    await Set.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

  public Task<Review?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, cancellationToken);
}

internal sealed class CartsRepository(ShopDbContext db) : EfRepository<Cart>(db), ICartsRepository {
  public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
}

internal sealed class FavoritesRepository(ShopDbContext db) : EfRepository<Favorites>(db), IFavoritesRepository {
  public Task<Favorites?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
    Set.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
}

internal sealed class OrdersRepository(ShopDbContext db) : EfRepository<Order>(db), IOrdersRepository {
  public override async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await Set.OrderByDescending(x => x.PlacedAt).ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
    await Set.Where(x => x.UserId == userId).OrderByDescending(x => x.PlacedAt).ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<Order>> GetByStatusAsync(
    IReadOnlyCollection<OrderStatus> statuses,
    CancellationToken cancellationToken = default) {
    var list = statuses.ToList();
    return await Set.Where(x => list.Contains(x.Status)).OrderBy(x => x.PlacedAt).ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<Order>> GetByCourierAsync(Guid courierId, CancellationToken cancellationToken = default) =>
    await Set
      .Where(x => x.Delivery != null && x.Delivery.CourierId == courierId)
      .OrderByDescending(x => x.PlacedAt)
      .ToListAsync(cancellationToken);
}
