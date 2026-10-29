using Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
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
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Delivery;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;

namespace Nonamebtww.Shop.Infrastructure.Api.Local;

// «Локальный сервер»: fake-реализации контрактов API. Сети нет — каждый вызов выполняет
// тот же use case Application поверх локальной SQLite. Когда появится backend,
// AddInfrastructure(o => o.UseRemoteApi = true) подставит Http*Api с теми же контрактами.

internal sealed class LocalCatalogApi(
  ICategoryQueries categories,
  IProductQueries products,
  IReviewService reviews) : ICatalogApi {
  public Task<Result<IReadOnlyList<CategoryNode>>> GetCategoryTreeAsync(CancellationToken ct = default) =>
    categories.GetTreeAsync(ct);

  public Task<Result<PagedList<CategorySimple>>> SearchCategoriesAsync(CategorySearch search, CancellationToken ct = default) =>
    categories.SearchAsync(search, ct);

  public Task<Result<CategoryDetailed>> GetCategoryAsync(Guid id, CancellationToken ct = default) =>
    categories.GetAsync(id, ct);

  public Task<Result<PagedList<ProductSimple>>> SearchProductsAsync(ProductSearch search, CancellationToken ct = default) =>
    products.SearchAsync(search, ct);

  public Task<Result<ProductDetailed>> GetProductAsync(Guid id, CancellationToken ct = default) =>
    products.GetAsync(id, ct);

  public Task<Result<IReadOnlyList<ReviewDto>>> GetReviewsAsync(Guid productId, CancellationToken ct = default) =>
    reviews.GetForProductAsync(productId, ct);

  public Task<Result<ReviewDto>> AddReviewAsync(AddReviewRequest request, CancellationToken ct = default) =>
    reviews.AddAsync(request, ct);
}

internal sealed class LocalCatalogManagementApi(ICatalogManagementService management) : ICatalogManagementApi {
  public Task<Result<CategoryDetailed>> SaveCategoryAsync(SaveCategoryRequest request, CancellationToken ct = default) =>
    management.SaveCategoryAsync(request, ct);

  public Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
    management.DeleteCategoryAsync(categoryId, ct);

  public Task<Result<ProductDetailed>> SaveProductAsync(SaveProductRequest request, CancellationToken ct = default) =>
    management.SaveProductAsync(request, ct);

  public Task<Result<ProductDetailed>> SetProductActiveAsync(Guid productId, bool isActive, CancellationToken ct = default) =>
    management.SetProductActiveAsync(productId, isActive, ct);

  public Task<Result<ProductDetailed>> SetStockAsync(Guid productId, int stock, CancellationToken ct = default) =>
    management.SetStockAsync(productId, stock, ct);
}

internal sealed class LocalCartApi(ICartService cart) : ICartApi {
  public Task<Result<CartDto>> GetAsync(CancellationToken ct = default) => cart.GetAsync(ct);

  public async Task<Result<CartDto>> SetQuantityAsync(Guid productId, int quantity, CancellationToken ct = default) {
    // Серверный контракт — «установить количество»; локально раскладываем его на операции корзины.
    var current = await cart.GetAsync(ct);

    if (current.IsFailure) {
      return current;
    }

    var inCart = current.Value.Items.Any(i => i.ProductId == productId);

    return (quantity, inCart) switch {
      (<= 0, true) => await cart.RemoveAsync(productId, ct),
      (<= 0, false) => current,
      (_, true) => await cart.ChangeQuantityAsync(productId, quantity, ct),
      _ => await cart.AddAsync(productId, quantity, ct),
    };
  }

  public Task<Result<CartDto>> ClearAsync(CancellationToken ct = default) => cart.ClearAsync(ct);
}

internal sealed class LocalFavoritesApi(IFavoritesService favorites) : IFavoritesApi {
  public Task<Result<IReadOnlyList<FavoriteItemDto>>> GetAsync(CancellationToken ct = default) => favorites.GetAsync(ct);

  public Task<Result> AddAsync(Guid productId, CancellationToken ct = default) => favorites.AddAsync(productId, ct);

  public Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default) => favorites.RemoveAsync(productId, ct);
}

internal sealed class LocalOrderApi(IOrderService orders) : IOrderApi {
  public Task<Result<OrderDetailsDto>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default) =>
    orders.CheckoutAsync(request, ct);

  public Task<Result<OrderDetailsDto>> PayAsync(Guid orderId, CancellationToken ct = default) =>
    orders.PayAsync(orderId, ct);

  public Task<Result<OrderDetailsDto>> CancelAsync(Guid orderId, CancellationToken ct = default) =>
    orders.CancelAsync(orderId, ct);

  public Task<Result<PagedList<OrderSummaryDto>>> GetMyOrdersAsync(
    OrderListFilter filter,
    int page,
    int pageSize,
    CancellationToken ct = default) =>
    orders.GetMyOrdersAsync(filter, page, pageSize, ct);

  public Task<Result<OrderDetailsDto>> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
    orders.GetOrderAsync(orderId, ct);
}

internal sealed class LocalDeliveryApi(IDeliveryService delivery) : IDeliveryApi {
  public Task<Result<IReadOnlyList<DispatchOrderDto>>> GetOrdersToDispatchAsync(CancellationToken ct = default) =>
    delivery.GetOrdersToDispatchAsync(ct);

  public Task<Result<IReadOnlyList<CourierDto>>> GetCouriersAsync(CancellationToken ct = default) =>
    delivery.GetCouriersAsync(ct);

  public Task<Result<DispatchOrderDto>> AssignCourierAsync(AssignCourierRequest request, CancellationToken ct = default) =>
    delivery.AssignCourierAsync(request, ct);

  public Task<Result<IReadOnlyList<DeliveryTaskDto>>> GetMyTasksAsync(bool activeOnly, CancellationToken ct = default) =>
    delivery.GetMyTasksAsync(activeOnly, ct);

  public Task<Result<DeliveryTaskDto>> AcceptAsync(Guid orderId, CancellationToken ct = default) =>
    delivery.AcceptAsync(orderId, ct);

  public Task<Result<DeliveryTaskDto>> ConfirmDeliveredAsync(Guid orderId, CancellationToken ct = default) =>
    delivery.ConfirmDeliveredAsync(orderId, ct);

  public Task<Result<DeliveryTaskDto>> ReportFailedAsync(ReportDeliveryFailedRequest request, CancellationToken ct = default) =>
    delivery.ReportFailedAsync(request, ct);
}

internal sealed class LocalProfileApi(IProfileService profile) : IProfileApi {
  public Task<Result<ProfileDto>> GetAsync(CancellationToken ct = default) => profile.GetAsync(ct);

  public Task<Result<ProfileDto>> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
    profile.UpdateAsync(request, ct);

  public Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
    profile.ChangePasswordAsync(request, ct);

  public Task<Result<IReadOnlyList<DeviceDto>>> GetDevicesAsync(CancellationToken ct = default) =>
    profile.GetDevicesAsync(ct);

  public Task<Result> SignOutDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
    profile.SignOutDeviceAsync(deviceId, ct);
}

internal sealed class LocalUserAdministrationApi(IUserAdministrationService administration) : IUserAdministrationApi {
  public Task<Result<PagedList<UserSummaryDto>>> GetUsersAsync(UserListQuery query, CancellationToken ct = default) =>
    administration.GetUsersAsync(query, ct);

  public Task<Result<UserSummaryDto>> ChangeRoleAsync(ChangeRoleRequest request, CancellationToken ct = default) =>
    administration.ChangeRoleAsync(request, ct);

  public Task<Result<UserSummaryDto>> BlockAsync(BlockUserRequest request, CancellationToken ct = default) =>
    administration.BlockAsync(request, ct);

  public Task<Result<UserSummaryDto>> UnblockAsync(Guid userId, CancellationToken ct = default) =>
    administration.UnblockAsync(userId, ct);
}
