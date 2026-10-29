using System.Globalization;
using Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Models.DTOs.Cart;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Application.Models.DTOs.Favorites;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;

namespace Nonamebtww.Shop.Infrastructure.Api.Http;

// Заготовки HTTP-клиентов будущего backend (REST, JSON, Bearer-токен).
// В прототипе не используются: включаются AddInfrastructure(o => { o.UseRemoteApi = true; o.ApiBaseUrl = ...; }).
// Маршруты — предложение для backend, их можно поменять в одном месте здесь.

internal sealed class HttpAuthApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), IAuthApi {
  public Task<Result<RegistrationPending>> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
    PostAsync<RegistrationPending>("api/v1/auth/register", request, ct);

  public Task<Result<AuthResult>> ConfirmRegistrationAsync(ConfirmRegistrationRequest request, CancellationToken ct = default) =>
    PostAsync<AuthResult>("api/v1/auth/register/confirm", request, ct);

  public Task<Result<AuthResult>> LoginAsync(LoginRequest request, string deviceName, CancellationToken ct = default) =>
    PostAsync<AuthResult>("api/v1/auth/login", new { request.Identifier, request.Password, DeviceName = deviceName }, ct);

  public Task<Result<AuthResult>> ExternalLoginAsync(ExternalLoginRequest request, string deviceName, CancellationToken ct = default) =>
    PostAsync<AuthResult>("api/v1/auth/external", new { request.Provider, request.IdToken, DeviceName = deviceName }, ct);

  public Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
    PostAsync<AuthTokens>("api/v1/auth/refresh", new { RefreshToken = refreshToken }, ct);

  public Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Post, "api/v1/auth/logout", new { RefreshToken = refreshToken }, ct);

  public Task<Result<PasswordResetPending>> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken ct = default) =>
    PostAsync<PasswordResetPending>("api/v1/auth/password/forgot", request, ct);

  public Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Post, "api/v1/auth/password/reset", request, ct);
}

internal sealed class HttpCatalogApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), ICatalogApi {
  public Task<Result<IReadOnlyList<CategoryNode>>> GetCategoryTreeAsync(CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<CategoryNode>>("api/v1/categories/tree", ct);

  public Task<Result<PagedList<CategorySimple>>> SearchCategoriesAsync(CategorySearch search, CancellationToken ct = default) =>
    GetAsync<PagedList<CategorySimple>>("api/v1/categories" + Query.From(
      ("query", search.Query), ("parentId", search.ParentId), ("rootsOnly", search.RootsOnly),
      ("page", search.Page), ("pageSize", search.PageSize)), ct);

  public Task<Result<CategoryDetailed>> GetCategoryAsync(Guid id, CancellationToken ct = default) =>
    GetAsync<CategoryDetailed>($"api/v1/categories/{id}", ct);

  public Task<Result<PagedList<ProductSimple>>> SearchProductsAsync(ProductSearch search, CancellationToken ct = default) =>
    GetAsync<PagedList<ProductSimple>>("api/v1/products" + Query.From(
      ("query", search.Query), ("categoryId", search.CategoryId), ("includeSubcategories", search.IncludeSubcategories),
      ("minPrice", search.MinPrice), ("maxPrice", search.MaxPrice), ("onlyInStock", search.OnlyInStock),
      ("sort", search.Sort), ("page", search.Page), ("pageSize", search.PageSize),
      ("includeInactive", search.IncludeInactive)), ct);

  public Task<Result<ProductDetailed>> GetProductAsync(Guid id, CancellationToken ct = default) =>
    GetAsync<ProductDetailed>($"api/v1/products/{id}", ct);

  public Task<Result<IReadOnlyList<ReviewDto>>> GetReviewsAsync(Guid productId, CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<ReviewDto>>($"api/v1/products/{productId}/reviews", ct);

  public Task<Result<ReviewDto>> AddReviewAsync(AddReviewRequest request, CancellationToken ct = default) =>
    PostAsync<ReviewDto>($"api/v1/products/{request.ProductId}/reviews", request, ct);
}

internal sealed class HttpCatalogManagementApi(HttpClient http, ISessionStore session)
  : HttpApiClientBase(http, session), ICatalogManagementApi {
  public Task<Result<CategoryDetailed>> SaveCategoryAsync(SaveCategoryRequest request, CancellationToken ct = default) =>
    PutAsync<CategoryDetailed>("api/v1/manage/categories", request, ct);

  public Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Delete, $"api/v1/manage/categories/{categoryId}", null, ct);

  public Task<Result<ProductDetailed>> SaveProductAsync(SaveProductRequest request, CancellationToken ct = default) =>
    PutAsync<ProductDetailed>("api/v1/manage/products", request, ct);

  public Task<Result<ProductDetailed>> SetProductActiveAsync(Guid productId, bool isActive, CancellationToken ct = default) =>
    PutAsync<ProductDetailed>($"api/v1/manage/products/{productId}/active", new { IsActive = isActive }, ct);

  public Task<Result<ProductDetailed>> SetStockAsync(Guid productId, int stock, CancellationToken ct = default) =>
    PutAsync<ProductDetailed>($"api/v1/manage/products/{productId}/stock", new { Stock = stock }, ct);
}

internal sealed class HttpCartApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), ICartApi {
  public Task<Result<CartDto>> GetAsync(CancellationToken ct = default) => GetAsync<CartDto>("api/v1/cart", ct);

  public Task<Result<CartDto>> SetQuantityAsync(Guid productId, int quantity, CancellationToken ct = default) =>
    PutAsync<CartDto>($"api/v1/cart/items/{productId}", new { Quantity = quantity }, ct);

  public Task<Result<CartDto>> ClearAsync(CancellationToken ct = default) => DeleteAsync<CartDto>("api/v1/cart", ct);
}

internal sealed class HttpFavoritesApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), IFavoritesApi {
  public Task<Result<IReadOnlyList<FavoriteItemDto>>> GetAsync(CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<FavoriteItemDto>>("api/v1/favorites", ct);

  public Task<Result> AddAsync(Guid productId, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Put, $"api/v1/favorites/{productId}", null, ct);

  public Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Delete, $"api/v1/favorites/{productId}", null, ct);
}

internal sealed class HttpOrderApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), IOrderApi {
  public Task<Result<OrderDetailsDto>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default) =>
    PostAsync<OrderDetailsDto>("api/v1/orders", request, ct);

  public Task<Result<OrderDetailsDto>> PayAsync(Guid orderId, CancellationToken ct = default) =>
    PostAsync<OrderDetailsDto>($"api/v1/orders/{orderId}/pay", null, ct);

  public Task<Result<OrderDetailsDto>> CancelAsync(Guid orderId, CancellationToken ct = default) =>
    PostAsync<OrderDetailsDto>($"api/v1/orders/{orderId}/cancel", null, ct);

  public Task<Result<PagedList<OrderSummaryDto>>> GetMyOrdersAsync(
    OrderListFilter filter,
    int page,
    int pageSize,
    CancellationToken ct = default) =>
    GetAsync<PagedList<OrderSummaryDto>>("api/v1/orders" + Query.From(
      ("filter", filter), ("page", page), ("pageSize", pageSize)), ct);

  public Task<Result<OrderDetailsDto>> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
    GetAsync<OrderDetailsDto>($"api/v1/orders/{orderId}", ct);
}

internal sealed class HttpDeliveryApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), IDeliveryApi {
  public Task<Result<IReadOnlyList<DispatchOrderDto>>> GetOrdersToDispatchAsync(CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<DispatchOrderDto>>("api/v1/dispatch/orders", ct);

  public Task<Result<IReadOnlyList<CourierDto>>> GetCouriersAsync(CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<CourierDto>>("api/v1/dispatch/couriers", ct);

  public Task<Result<DispatchOrderDto>> AssignCourierAsync(AssignCourierRequest request, CancellationToken ct = default) =>
    PostAsync<DispatchOrderDto>($"api/v1/dispatch/orders/{request.OrderId}/courier", request, ct);

  public Task<Result<IReadOnlyList<DeliveryTaskDto>>> GetMyTasksAsync(bool activeOnly, CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<DeliveryTaskDto>>("api/v1/deliveries/my" + Query.From(("activeOnly", activeOnly)), ct);

  public Task<Result<DeliveryTaskDto>> AcceptAsync(Guid orderId, CancellationToken ct = default) =>
    PostAsync<DeliveryTaskDto>($"api/v1/deliveries/{orderId}/accept", null, ct);

  public Task<Result<DeliveryTaskDto>> ConfirmDeliveredAsync(Guid orderId, CancellationToken ct = default) =>
    PostAsync<DeliveryTaskDto>($"api/v1/deliveries/{orderId}/delivered", null, ct);

  public Task<Result<DeliveryTaskDto>> ReportFailedAsync(ReportDeliveryFailedRequest request, CancellationToken ct = default) =>
    PostAsync<DeliveryTaskDto>($"api/v1/deliveries/{request.OrderId}/failed", request, ct);
}

internal sealed class HttpProfileApi(HttpClient http, ISessionStore session) : HttpApiClientBase(http, session), IProfileApi {
  public Task<Result<ProfileDto>> GetAsync(CancellationToken ct = default) => GetAsync<ProfileDto>("api/v1/profile", ct);

  public Task<Result<ProfileDto>> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
    PutAsync<ProfileDto>("api/v1/profile", request, ct);

  public Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Post, "api/v1/profile/password", request, ct);

  public Task<Result<IReadOnlyList<DeviceDto>>> GetDevicesAsync(CancellationToken ct = default) =>
    GetAsync<IReadOnlyList<DeviceDto>>("api/v1/profile/devices", ct);

  public Task<Result> SignOutDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
    SendAsync(HttpMethod.Delete, $"api/v1/profile/devices/{deviceId}", null, ct);
}

internal sealed class HttpUserAdministrationApi(HttpClient http, ISessionStore session)
  : HttpApiClientBase(http, session), IUserAdministrationApi {
  public Task<Result<PagedList<UserSummaryDto>>> GetUsersAsync(UserListQuery query, CancellationToken ct = default) =>
    GetAsync<PagedList<UserSummaryDto>>("api/v1/admin/users" + Query.From(
      ("search", query.Search), ("role", query.Role), ("isBlocked", query.IsBlocked),
      ("page", query.Page), ("pageSize", query.PageSize)), ct);

  public Task<Result<UserSummaryDto>> ChangeRoleAsync(ChangeRoleRequest request, CancellationToken ct = default) =>
    PutAsync<UserSummaryDto>($"api/v1/admin/users/{request.UserId}/role", request, ct);

  public Task<Result<UserSummaryDto>> BlockAsync(BlockUserRequest request, CancellationToken ct = default) =>
    PostAsync<UserSummaryDto>($"api/v1/admin/users/{request.UserId}/block", request, ct);

  public Task<Result<UserSummaryDto>> UnblockAsync(Guid userId, CancellationToken ct = default) =>
    PostAsync<UserSummaryDto>($"api/v1/admin/users/{userId}/unblock", null, ct);
}

/// <summary>Сборка query string без пустых параметров.</summary>
internal static class Query {
  public static string From(params (string Name, object? Value)[] parameters) {
    var parts = parameters
      .Where(p => p.Value is not null && !(p.Value is string s && string.IsNullOrWhiteSpace(s)))
      .Select(p => $"{p.Name}={Uri.EscapeDataString(Format(p.Value!))}")
      .ToList();

    return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
  }

  private static string Format(object value) => value switch {
    bool b => b ? "true" : "false",
    IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
    _ => value.ToString() ?? string.Empty,
  };
}
