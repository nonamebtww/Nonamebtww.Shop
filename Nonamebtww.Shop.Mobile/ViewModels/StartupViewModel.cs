using CommunityToolkit.Mvvm.ComponentModel;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Infrastructure;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels;

/// <summary>Prepares local storage, restores the saved session and opens the first screen.</summary>
public sealed partial class StartupViewModel(
  IServiceProvider services,
  IAuthService auth,
  IOrderService orders,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  private bool _started;

  [ObservableProperty]
  public partial string StatusText { get; set; } = "Подготовка данных…";

  public override async Task OnAppearingAsync() {
    if (_started) {
      return;
    }

    _started = true;

    try {
      await services.InitializeInfrastructureAsync();

      StatusText = "Восстановление сессии…";
      var session = await auth.RestoreSessionAsync();
      if (session.IsSuccess && session.Value is not null) {
        await orders.CancelExpiredUnpaidAsync();
        await Navigation.GoHomeAsync(session.Value);
        return;
      }
    } catch (Exception ex) {
      await Dialogs.AlertAsync("Ошибка запуска", ex.Message);
    }

    await Navigation.GoToRootAsync(Routes.Login);
  }
}
