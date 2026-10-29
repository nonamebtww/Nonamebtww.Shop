using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Orders;

/// <summary>Use case «Просмотр заказов»: active (14.2) and completed (14.3) orders.</summary>
public sealed partial class OrdersViewModel(IOrderService orders, RemoteDataCache cache, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  private ObservableAsPropertyHelper<string>? _emptyHint;

  public ObservableCollection<OrderListItem> Orders { get; } = [];

  [Reactive]
  public partial bool IsActiveTab { get; set; } = true;

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  public string EmptyHint => Derived(ref _emptyHint, this.WhenAnyValue(x => x.IsActiveTab)
    .Select(active => active ? "Оформленные заказы появятся здесь" : "Здесь будут вручённые и отменённые заказы"));

  public override async Task OnAppearingAsync() {
    if ((await orders.CancelExpiredUnpaidAsync()) is { IsSuccess: true, Value: > 0 }) {
      cache.Invalidate(CacheKeys.Orders);
    }

    await LoadAsync();
  }

  [ReactiveCommand]
  private Task ShowActiveAsync() {
    IsActiveTab = true;
    return LoadAsync();
  }

  [ReactiveCommand]
  private Task ShowCompletedAsync() {
    IsActiveTab = false;
    return LoadAsync();
  }

  [ReactiveCommand]
  private async Task RefreshAsync() {
    cache.Invalidate(CacheKeys.Orders);
    await LoadAsync(showSkeleton: false);
    IsRefreshing = false;
  }

  [ReactiveCommand]
  private Task OpenAsync(OrderListItem order) =>
    Navigation.GoToAsync(Routes.OrderDetails, new Dictionary<string, object> { [Routes.Params.Id] = order.Id });

  private Task LoadAsync(bool showSkeleton = true) {
    var filter = IsActiveTab ? OrderListFilter.Active : OrderListFilter.Completed;
    var key = CacheKeys.Orders + filter;
    if (showSkeleton && !cache.Contains(key)) {
      Orders.Clear();
    }

    return LoadWithSkeletonAsync(async () => {
      var page = await ExecuteAsync(() => cache.GetAsync<PagedList<OrderSummaryDto>>(key, () => orders.GetMyOrdersAsync(filter, 1, 100)), showBusy: false);
      if (page is null) {
        return;
      }

      Orders.Clear();
      foreach (var order in page.Items.OrderByDescending(x => x.CreatedAt)) {
        Orders.Add(OrderListItem.From(order));
      }
    }, () => !showSkeleton || cache.Contains(key));
  }
}
