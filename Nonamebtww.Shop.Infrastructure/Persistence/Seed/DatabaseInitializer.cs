using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.ValueObjects;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Seed;

/// <summary>Создаёт файл SQLite и заполняет пустую БД справочником ролей и демо-данными.</summary>
internal sealed class DatabaseInitializer {
  private readonly IDbContextFactory<ShopDbContext> _contextFactory;
  private readonly IPasswordHasher _passwordHasher;
  private readonly IClock _clock;
  private readonly InfrastructureOptions _options;
  private readonly ILogger<DatabaseInitializer> _logger;

  public DatabaseInitializer(
    IDbContextFactory<ShopDbContext> contextFactory,
    IPasswordHasher passwordHasher,
    IClock clock,
    InfrastructureOptions options,
    ILogger<DatabaseInitializer> logger) {
    _contextFactory = contextFactory;
    _passwordHasher = passwordHasher;
    _clock = clock;
    _options = options;
    _logger = logger;
  }

  public async Task InitializeAsync(CancellationToken ct = default) {
    var directory = Path.GetDirectoryName(Path.GetFullPath(_options.DatabasePath));

    if (!string.IsNullOrEmpty(directory)) {
      Directory.CreateDirectory(directory);
    }

    await using var db = await _contextFactory.CreateDbContextAsync(ct);

    if (_options.RecreateDatabaseOnStartup) {
      await db.Database.EnsureDeletedAsync(ct);
    }

    // Прототип без миграций: схема создаётся по модели. При изменении модели
    // включите RecreateDatabaseOnStartup или удалите файл БД.
    await db.Database.EnsureCreatedAsync(ct);

    if (!await db.Roles.AnyAsync(ct)) {
      db.Roles.AddRange(Role.CreateDefaults());
      await db.SaveChangesAsync(ct);
      _logger.LogInformation("Справочник ролей заполнен");
    }

    if (_options.SeedDemoData && !await db.Users.AnyAsync(ct)) {
      await SeedDemoDataAsync(db, ct);
      _logger.LogInformation("Демо-данные добавлены в {Path}", _options.DatabasePath);
    }
  }

  private async Task SeedDemoDataAsync(ShopDbContext db, CancellationToken ct) {
    var now = _clock.UtcNow;

    var users = DemoCatalog.Users
      .Select(u => User.Register(
        new Login(u.Login),
        new Email(u.Email),
        new PhoneNumber(u.Phone),
        _passwordHasher.Hash(u.Password),
        u.FirstName,
        u.LastName,
        now,
        u.Role,
        confirmed: true))
      .ToList();

    db.Users.AddRange(users);

    var categories = new Dictionary<string, Category>();

    foreach (var data in DemoCatalog.Categories) {
      Guid? parentId = data.ParentKey is null ? null : categories[data.ParentKey].Id;
      categories[data.Key] = Category.Create(data.Name, parentId, data.Description, data.Photo);
    }

    db.Categories.AddRange(categories.Values);

    var products = DemoCatalog.Products
      .Select(p => Product.Create(
        categories[p.CategoryKey].Id,
        new Sku(p.Sku),
        p.Name,
        Money.Rub(p.Price),
        p.Stock,
        p.Description,
        p.Characteristics,
        p.Photo))
      .ToList();

    db.Products.AddRange(products);

    var buyer = users.Single(u => u.Role == UserRole.Customer);
    db.Carts.Add(Cart.CreateFor(buyer.Id));

    var favorites = Favorites.CreateFor(buyer.Id);
    favorites.Add(products[1].Id, now);
    favorites.Add(products[5].Id, now);
    db.Favorites.Add(favorites);

    db.Reviews.AddRange(
      Review.Create(buyer.Id, products[0].Id, new Rating(5), "Отличный смартфон за свои деньги, экран очень яркий.", now.AddDays(-10)),
      Review.Create(buyer.Id, products[5].Id, new Rating(4), "Хорошее шумоподавление, но кейс немного великоват.", now.AddDays(-3)));

    // Сид не публикует доменные события: это начальное состояние, а не действия пользователей.
    foreach (var aggregate in db.ChangeTracker.Entries()
               .Select(e => e.Entity)
               .OfType<Domain.Entities.Abstractions.Interfaces.IAggregateRoot>()) {
      aggregate.ClearDomainEvents();
    }

    await db.SaveChangesAsync(ct);
  }
}
