using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nonamebtww.Shop.Application.Api;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Payments;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Infrastructure.Api;
using Nonamebtww.Shop.Infrastructure.Caching;
using Nonamebtww.Shop.Infrastructure.Common;
using Nonamebtww.Shop.Infrastructure.Events;
using Nonamebtww.Shop.Infrastructure.Notifications;
using Nonamebtww.Shop.Infrastructure.Options;
using Nonamebtww.Shop.Infrastructure.Payments;
using Nonamebtww.Shop.Infrastructure.Persistence;
using Nonamebtww.Shop.Infrastructure.Persistence.Repositories;
using Nonamebtww.Shop.Infrastructure.Persistence.Seed;
using Nonamebtww.Shop.Infrastructure.Security;
using Nonamebtww.Shop.Infrastructure.Session;

namespace Nonamebtww.Shop.Infrastructure;

public static class DependencyInjection {
  /// <summary>
  /// Регистрирует слой Infrastructure: SQLite (EF Core), репозитории и IUnitOfWork,
  /// реализации портов Application и клиентов API (локальные fake или HTTP — см. UseRemoteApi).
  /// </summary>
  /// <example>
  /// builder.Services.AddInfrastructure(o => o.DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "shop.db3"));
  /// ...
  /// await app.Services.InitializeInfrastructureAsync();
  /// </example>
  public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    Action<InfrastructureOptions>? configure = null) {
    var options = new InfrastructureOptions();
    configure?.Invoke(options);

    services.AddSingleton(options);
    services.TryAddSingleton(new ApiOptions {
      UseRemoteApi = options.UseRemoteApi,
      BaseAddress = options.ApiBaseUrl,
      Timeout = options.ApiTimeout,
    });
    services.AddLogging();

    // База данных. В MAUI сервисы резолвятся из корневого провайдера, поэтому scoped-контекст
    // фактически один на приложение: репозитории и IUnitOfWork видят одни и те же агрегаты.
    // Для изолированных операций (кэш, сид) используется фабрика контекстов.
    services.AddDbContextFactory<ShopDbContext>(
      db => db.UseSqlite(
        $"Data Source={options.DatabasePath}",
        sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)),
      ServiceLifetime.Singleton);
    services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ShopDbContext>>().CreateDbContext());

    services.AddScoped<DomainEventDispatcher>();
    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    services.AddScoped<IRolesRepository, RolesRepository>();
    services.AddScoped<IUsersRepository, UsersRepository>();
    services.AddScoped<ICategoriesRepository, CategoriesRepository>();
    services.AddScoped<IProductsRepository, ProductsRepository>();
    services.AddScoped<IReviewsRepository, ReviewsRepository>();
    services.AddScoped<ICartsRepository, CartsRepository>();
    services.AddScoped<IFavoritesRepository, FavoritesRepository>();
    services.AddScoped<IOrdersRepository, OrdersRepository>();
    services.AddTransient<DatabaseInitializer>();

    // Порты Application. TryAdd — чтобы Mobile мог заранее подменить реализацию
    // (например, ISessionStore поверх SecureStorage или IDeviceInfoProvider поверх DeviceInfo).
    services.TryAddSingleton<IClock, SystemClock>();
    services.TryAddSingleton<IDeviceInfoProvider, DefaultDeviceInfoProvider>();
    services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    services.TryAddSingleton<ITokenService, LocalTokenService>();
    services.TryAddSingleton<ISessionStore, FileSessionStore>();
    services.TryAddSingleton<IPersistenceCache, SqlitePersistenceCache>();
    services.TryAddSingleton<LocalNotificationService>();
    services.TryAddSingleton<INotificationService>(sp => sp.GetRequiredService<LocalNotificationService>());
    services.TryAddSingleton<IPaymentProvider, FakePaymentProvider>();

    // Клиенты будущего серверного API: точка подмены fake ↔ HttpClient.
    services.AddApiClients(options);

    return services;
  }

  /// <summary>Создаёт БД и заполняет её демо-данными. Вызывать один раз после построения провайдера.</summary>
  public static async Task InitializeInfrastructureAsync(this IServiceProvider services, CancellationToken ct = default) {
    using var scope = services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(ct);
  }
}
