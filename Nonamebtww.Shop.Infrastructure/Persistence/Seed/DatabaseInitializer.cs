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

    // Локальная БД прототипа пересоздаётся, если сменилась версия сида/схемы (нет миграций).
    var versionFile = Path.GetFullPath(_options.DatabasePath) + ".version";
    var storedVersion = File.Exists(versionFile) ? (await File.ReadAllTextAsync(versionFile, ct)).Trim() : null;
    var currentVersion = DemoCatalog.Version.ToString();

    if (_options.RecreateDatabaseOnStartup || storedVersion != currentVersion) {
      await db.Database.EnsureDeletedAsync(ct);
      _logger.LogInformation("БД пересоздаётся: версия {Old} → {New}", storedVersion ?? "нет", currentVersion);
    }

    // Прототип без миграций: схема создаётся по модели. При изменении модели
    // включите RecreateDatabaseOnStartup или удалите файл БД.
    await db.Database.EnsureCreatedAsync(ct);
    await File.WriteAllTextAsync(versionFile, currentVersion, ct);

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
    var usersByLogin = users.ToDictionary(u => u.Login.Value, StringComparer.OrdinalIgnoreCase);
    var productsBySku = products.ToDictionary(p => p.Sku.Value, StringComparer.OrdinalIgnoreCase);

    // Корзина и избранное есть у каждого: сотрудники тоже могут покупать.
    foreach (var user in users) {
      db.Carts.Add(Cart.CreateFor(user.Id));
    }

    var favorites = Favorites.CreateFor(buyer.Id);
    favorites.Add(productsBySku["AU-APMAX"].Id, now);
    favorites.Add(productsBySku["TB-IPMINI"].Id, now);
    db.Favorites.Add(favorites);

    db.Reviews.AddRange(DemoCatalog.Reviews.Select(r => Review.Create(
      usersByLogin[r.Login].Id,
      productsBySku[r.Sku].Id,
      new Rating(r.Rating),
      r.Text,
      now.AddDays(-r.DaysAgo))));

    // Сид не публикует доменные события: это начальное состояние, а не действия пользователей.
    foreach (var aggregate in db.ChangeTracker.Entries()
               .Select(e => e.Entity)
               .OfType<Domain.Entities.Abstractions.Interfaces.IAggregateRoot>()) {
      aggregate.ClearDomainEvents();
    }

    await db.SaveChangesAsync(ct);
  }
}
