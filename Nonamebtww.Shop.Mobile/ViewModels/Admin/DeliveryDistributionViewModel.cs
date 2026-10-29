using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Delivery;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>Logistician's use case «Распределить доставку заказов»: assign a courier to a confirmed order.</summary>
public sealed partial class DeliveryDistributionViewModel(IDeliveryService delivery, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  private IReadOnlyList<CourierDto> _couriers = [];

  public ObservableCollection<DispatchItem> Orders { get; } = [];

  [ObservableProperty]
  public partial bool IsRefreshing { get; set; }

  [ObservableProperty]
  public partial string CouriersText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => LoadAsync();

  [RelayCommand]
  private async Task RefreshAsync() {
    await LoadAsync();
    IsRefreshing = false;
  }

  [RelayCommand]
  private async Task AssignAsync(DispatchItem item) {
    if (_couriers.Count == 0) {
      await Dialogs.AlertAsync("Курьеры", "Нет доступных курьеров.");
      return;
    }

    var captions = _couriers.Select(x => $"{x.FullName} · заданий {x.ActiveTasks}").ToArray();
    var choice = await Dialogs.ChooseAsync($"Курьер для {item.Number}", captions);
    if (choice is null) {
      return;
    }

    var courier = _couriers[Array.IndexOf(captions, choice)];
    if (await ExecuteAsync(() => delivery.AssignCourierAsync(new AssignCourierRequest(item.Order.OrderId, courier.Id))) is not null) {
      await LoadAsync();
    }
  }

  private async Task LoadAsync() {
    _couriers = await ExecuteAsync(() => delivery.GetCouriersAsync()) ?? [];
    CouriersText = _couriers.Count == 0
      ? "Курьеров нет"
      : "Курьеры: " + string.Join(", ", _couriers.Select(x => $"{x.FullName} ({x.ActiveTasks})"));

    var orders = await ExecuteAsync(() => delivery.GetOrdersToDispatchAsync());
    if (orders is null) {
      return;
    }

    Orders.Clear();
    foreach (var order in orders.OrderBy(x => x.CourierId is not null).ThenBy(x => x.CreatedAt)) {
      Orders.Add(DispatchItem.From(order));
    }
  }
}
