using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nonamebtww.Shop.Application.Api;
using Nonamebtww.Shop.Application.Common.Validation;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Delivery;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Application.Services.Implementations.Administration;
using Nonamebtww.Shop.Application.Services.Implementations.Auth;
using Nonamebtww.Shop.Application.Services.Implementations.Cart;
using Nonamebtww.Shop.Application.Services.Implementations.Catalog;
using Nonamebtww.Shop.Application.Services.Implementations.Delivery;
using Nonamebtww.Shop.Application.Services.Implementations.Favorites;
using Nonamebtww.Shop.Application.Services.Implementations.Orders;
using Nonamebtww.Shop.Application.Services.Implementations.Profile;
using Nonamebtww.Shop.Application.Validation;

namespace Nonamebtww.Shop.Application;

public static class DependencyInjection {
  /// <summary>
  /// Регистрирует use case слоя Application. Порты (репозитории, IPasswordHasher, ITokenService, IClock,
  /// INotificationService, IPaymentProvider, ISessionStore, IDeviceInfoProvider, IPersistenceCache)
  /// регистрирует Infrastructure.
  /// </summary>
  public static IServiceCollection AddApplication(this IServiceCollection services, Action<ApiOptions>? configureApi = null) {
    var apiOptions = new ApiOptions();
    configureApi?.Invoke(apiOptions);
    services.TryAddSingleton(apiOptions);

    // Состояние приложения: один экземпляр на всё время работы.
    services.TryAddSingleton<CurrentUserService>();
    services.TryAddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
    services.TryAddSingleton<CartChangeNotifier>();

    // Use case: процессы DFD.
    services.TryAddTransient<IAuthService, AuthService>();
    services.TryAddTransient<IProfileService, ProfileService>();
    services.TryAddTransient<IUserAdministrationService, UserAdministrationService>();
    services.TryAddTransient<ICategoryQueries, CategoryQueries>();
    services.TryAddTransient<ProductQueries>();
    services.TryAddTransient<IProductQueries>(sp => sp.GetRequiredService<ProductQueries>());
    services.TryAddTransient<IReviewService, ReviewService>();
    services.TryAddTransient<ICatalogManagementService, CatalogManagementService>();
    services.TryAddTransient<IFavoritesService, FavoritesService>();
    services.TryAddTransient<ICartService, CartService>();
    services.TryAddTransient<IOrderService, OrderService>();
    services.TryAddTransient<IDeliveryService, DeliveryService>();

    // Валидаторы доступны и UI для проверки форм на лету.
    services.TryAddSingleton<IValidator<RegisterRequest>>(RegisterRequestValidator.Instance);
    services.TryAddSingleton<IValidator<ConfirmRegistrationRequest>>(ConfirmRegistrationRequestValidator.Instance);
    services.TryAddSingleton<IValidator<LoginRequest>>(LoginRequestValidator.Instance);
    services.TryAddSingleton<IValidator<PasswordResetRequest>>(PasswordResetRequestValidator.Instance);
    services.TryAddSingleton<IValidator<ResetPasswordRequest>>(ResetPasswordRequestValidator.Instance);
    services.TryAddSingleton<IValidator<UpdateProfileRequest>>(UpdateProfileRequestValidator.Instance);
    services.TryAddSingleton<IValidator<ChangePasswordRequest>>(ChangePasswordRequestValidator.Instance);
    services.TryAddSingleton<IValidator<DeleteAccountRequest>>(DeleteAccountRequestValidator.Instance);
    services.TryAddSingleton<IValidator<BlockUserRequest>>(BlockUserRequestValidator.Instance);
    services.TryAddSingleton<IValidator<SaveCategoryRequest>>(SaveCategoryRequestValidator.Instance);
    services.TryAddSingleton<IValidator<SaveProductRequest>>(SaveProductRequestValidator.Instance);
    services.TryAddSingleton<IValidator<AddReviewRequest>>(AddReviewRequestValidator.Instance);
    services.TryAddSingleton<IValidator<CheckoutRequest>>(CheckoutRequestValidator.Instance);
    services.TryAddSingleton<IValidator<AssignCourierRequest>>(AssignCourierRequestValidator.Instance);
    services.TryAddSingleton<IValidator<ReportDeliveryFailedRequest>>(ReportDeliveryFailedRequestValidator.Instance);

    return services;
  }
}
