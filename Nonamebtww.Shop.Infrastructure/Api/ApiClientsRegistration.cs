using Microsoft.Extensions.DependencyInjection;
using Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;
using Nonamebtww.Shop.Infrastructure.Api.Http;
using Nonamebtww.Shop.Infrastructure.Api.Local;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Api;

internal static class ApiClientsRegistration {
  /// <summary>
  /// Точка подмены клиентов API. UseRemoteApi = false (прототип) — локальные fake-реализации,
  /// выполняющие use case Application поверх SQLite; true — HTTP-клиенты на HttpClient к ApiBaseUrl.
  /// </summary>
  public static IServiceCollection AddApiClients(this IServiceCollection services, InfrastructureOptions options) {
    if (!options.UseRemoteApi) {
      services.AddTransient<IAuthApi, LocalAuthApi>();
      services.AddTransient<ICatalogApi, LocalCatalogApi>();
      services.AddTransient<ICatalogManagementApi, LocalCatalogManagementApi>();
      services.AddTransient<ICartApi, LocalCartApi>();
      services.AddTransient<IFavoritesApi, LocalFavoritesApi>();
      services.AddTransient<IOrderApi, LocalOrderApi>();
      services.AddTransient<IDeliveryApi, LocalDeliveryApi>();
      services.AddTransient<IProfileApi, LocalProfileApi>();
      services.AddTransient<IUserAdministrationApi, LocalUserAdministrationApi>();
      return services;
    }

    var baseAddress = options.ApiBaseUrl
      ?? throw new InvalidOperationException("Для UseRemoteApi = true нужно указать InfrastructureOptions.ApiBaseUrl.");

    void Configure(HttpClient client) {
      client.BaseAddress = baseAddress;
      client.Timeout = options.ApiTimeout;
    }

    services.AddHttpClient<IAuthApi, HttpAuthApi>(Configure);
    services.AddHttpClient<ICatalogApi, HttpCatalogApi>(Configure);
    services.AddHttpClient<ICatalogManagementApi, HttpCatalogManagementApi>(Configure);
    services.AddHttpClient<ICartApi, HttpCartApi>(Configure);
    services.AddHttpClient<IFavoritesApi, HttpFavoritesApi>(Configure);
    services.AddHttpClient<IOrderApi, HttpOrderApi>(Configure);
    services.AddHttpClient<IDeliveryApi, HttpDeliveryApi>(Configure);
    services.AddHttpClient<IProfileApi, HttpProfileApi>(Configure);
    services.AddHttpClient<IUserAdministrationApi, HttpUserAdministrationApi>(Configure);
    return services;
  }
}
