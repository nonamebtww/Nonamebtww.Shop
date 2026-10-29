using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Orders;

/// <summary>Use case «Просмотр заказов»: active (14.2) and completed (14.3) orders.</summary>
public sealed partial class OrdersViewModel(IOrderService orders, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<OrderListItem> Orders { get; } = [];

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(EmptyHint))]
  public partial bool IsActiveTab { get; set; } = true;

  [ObservableProperty]
  public partial bool IsRefreshing { get; set; }

  public string EmptyHint => IsActiveTab ? "Оформленные заказы появятся здесь" : "Здесь будут вручённые и отменённые заказы";

  public override async Task OnAppearingAsync() {
    await orders.CancelExpiredUnpaidAsync();
    await LoadAsync();
  }

  [RelayCommand]
  private Task ShowActiveAsync() {
    IsActiveTab = true;
    return LoadAsync();
  }

  [RelayCommand]
  private Task ShowCompletedAsync() {
    IsActiveTab = false;
    return LoadAsync();
  }

  [RelayCommand]
  private async Task RefreshAsync() {
    await LoadAsync();
    IsRefreshing = false;
  }

  [RelayCommand]
  private Task OpenAsync(OrderListItem order) =>
    Navigation.GoToAsync(Routes.OrderDetails, new Dictionary<string, object> { [Routes.Params.Id] = order.Id });

  private async Task LoadAsync() {
    var filter = IsActiveTab ? OrderListFilter.Active : OrderListFilter.Completed;
    var page = await ExecuteAsync(() => orders.GetMyOrdersAsync(filter, 1, 100));
    if (page is null) {
      return;
    }

    Orders.Clear();
    foreach (var order in page.Items.OrderByDescending(x => x.CreatedAt)) {
      Orders.Add(OrderListItem.From(order));
    }
  }
}
