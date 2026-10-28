using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nonamebtww.Shop.Application.Entities;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence;

/// <summary>Локальная база SQLite приложения. Таблицы соответствуют сущностям IDEF1X.</summary>
public sealed class ShopDbContext : DbContext {
  public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options) { }

  public DbSet<Role> Roles => Set<Role>();
  public DbSet<User> Users => Set<User>();
  public DbSet<Category> Categories => Set<Category>();
  public DbSet<Product> Products => Set<Product>();
  public DbSet<Review> Reviews => Set<Review>();
  public DbSet<Cart> Carts => Set<Cart>();
  public DbSet<Favorites> Favorites => Set<Favorites>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<Cache> CacheEntries => Set<Cache>();

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
    // SQLite не умеет сравнивать и сортировать DateTimeOffset и decimal на стороне БД,
    // поэтому храним даты как long, а суммы как double (для цен магазина точности хватает).
    configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    configurationBuilder.Properties<decimal>().HaveConversion<double>();
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
  }
}
