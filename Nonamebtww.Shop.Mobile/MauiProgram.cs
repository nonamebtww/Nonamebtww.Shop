using System.Reactive;
using FFImageLoading.Maui;
using Microsoft.Extensions.Logging;
using Nonamebtww.Shop.Application;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Infrastructure;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.Services.Notifications;
using Nonamebtww.Shop.Mobile.Services.Platform;
using Nonamebtww.Shop.Mobile.ViewModels;
using Nonamebtww.Shop.Mobile.ViewModels.Admin;
using Nonamebtww.Shop.Mobile.ViewModels.Auth;
using Nonamebtww.Shop.Mobile.ViewModels.Cart;
using Nonamebtww.Shop.Mobile.ViewModels.Catalog;
using Nonamebtww.Shop.Mobile.ViewModels.Favorites;
using Nonamebtww.Shop.Mobile.ViewModels.Orders;
using Nonamebtww.Shop.Mobile.ViewModels.Profile;
using Nonamebtww.Shop.Mobile.Views;
using Nonamebtww.Shop.Mobile.Views.Admin;
using Nonamebtww.Shop.Mobile.Views.Auth;
using Nonamebtww.Shop.Mobile.Views.Cart;
using Nonamebtww.Shop.Mobile.Views.Catalog;
using Nonamebtww.Shop.Mobile.Views.Favorites;
using Nonamebtww.Shop.Mobile.Views.Orders;
using Nonamebtww.Shop.Mobile.Views.Profile;
using ReactiveUI;
using ReactiveUI.Builder;

namespace Nonamebtww.Shop.Mobile;

public static class MauiProgram {
  public static MauiApp CreateMauiApp() {
    var builder = MauiApp.CreateBuilder();

    builder
      .UseMauiApp<App>()
      // ReactiveUI: MAUI main-thread scheduler for ReactiveCommand / WhenAnyValue / ToProperty;
      // unhandled errors of reactive pipelines are shown instead of crashing the prototype.
      .UseReactiveUI(rx => rx
        .WithMaui(Dispatcher.GetForCurrentThread()!)
        .WithExceptionHandler(Observer.Create<Exception>(ex =>
          MainThread.BeginInvokeOnMainThread(() => _ = Shell.Current?.DisplayAlertAsync("Ошибка", ex.Message, "OK")))))
      // FFImageLoading: CachedImage with memory + disk cache for product photos.
      .UseFFImageLoading()
#if IOS || MACCATALYST
      // The default iOS CollectionView handler of .NET 10 (CollectionViewHandler2) stalled the main thread
      // for 50–150 ms on every new row of the catalog grid; the classic handler scrolls smoothly.
      .ConfigureMauiHandlers(handlers =>
        handlers.AddHandler<CollectionView, Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler>())
#endif
      .ConfigureFonts(fonts => {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
      });

#if DEBUG
    builder.Logging.AddDebug();
#endif

    FormHandlers.Configure();

    // Platform overrides must be registered before AddInfrastructure (it uses TryAdd).
    builder.Services.AddSingleton<IDeviceInfoProvider, MauiDeviceInfoProvider>();

    // Layers: use cases (Application) and local storage / fake external services (Infrastructure).
    // No network: UseRemoteApi stays false until a backend exists.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(options => {
      options.DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "shop.db3");
      options.UseRemoteApi = false;
    });

    builder.Services
      .AddSingleton<INavigationService, ShellNavigationService>()
      .AddSingleton<IDialogService, DialogService>()
      .AddSingleton<NotificationPresenter>()
      .AddSingleton<RemoteDataCache>();

    builder.Services
      .AddScreen<StartupPage, StartupViewModel>()
      .AddScreen<LoginPage, LoginViewModel>()
      .AddScreen<RegisterPage, RegisterViewModel>()
      .AddScreen<ForgotPasswordPage, ForgotPasswordViewModel>()
      .AddScreen<CatalogPage, CatalogViewModel>()
      .AddScreen<ProductPage, ProductViewModel>()
      .AddScreen<FavoritesPage, FavoritesViewModel>()
      .AddScreen<CartPage, CartViewModel>()
      .AddScreen<CheckoutPage, CheckoutViewModel>()
      .AddScreen<OrdersPage, OrdersViewModel>()
      .AddScreen<OrderDetailsPage, OrderDetailsViewModel>()
      .AddScreen<ProfilePage, ProfileViewModel>()
      .AddScreen<ChangePasswordPage, ChangePasswordViewModel>()
      .AddScreen<PersonalDataPage, PersonalDataViewModel>()
      .AddScreen<NotificationsPage, NotificationsViewModel>()
      .AddScreen<AdminHubPage, AdminHubViewModel>()
      .AddScreen<CatalogManagementPage, CatalogManagementViewModel>()
      .AddScreen<ProductEditPage, ProductEditViewModel>()
      .AddScreen<CategoryEditPage, CategoryEditViewModel>()
      .AddScreen<UsersPage, UsersViewModel>()
      .AddScreen<UserDetailsPage, UserDetailsViewModel>()
      .AddScreen<DeliveryDistributionPage, DeliveryDistributionViewModel>()
      .AddScreen<CourierTasksPage, CourierTasksViewModel>();

    var app = builder.Build();
    app.Services.GetRequiredService<NotificationPresenter>().Start();
    return app;
  }

  private static IServiceCollection AddScreen<TPage, TViewModel>(this IServiceCollection services)
    where TPage : Page
    where TViewModel : class {
    services.AddTransient<TPage>();
    services.AddTransient<TViewModel>();
    return services;
  }
}
