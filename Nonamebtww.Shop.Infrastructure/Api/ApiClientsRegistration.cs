using Microsoft.Extensions.DependencyInjection;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Api;

internal static class ApiClientsRegistration {
  /// <summary>
  /// Точка подмены клиентов API: UseRemoteApi = false — локальные fake-реализации поверх SQLite,
  /// true — HTTP-реализации на HttpClient (backend в прототипе не реализован).
  /// </summary>
  public static IServiceCollection AddApiClients(this IServiceCollection services, InfrastructureOptions options) {
    // Заполняется после коммита I*Api в Application.
    return services;
  }
}
