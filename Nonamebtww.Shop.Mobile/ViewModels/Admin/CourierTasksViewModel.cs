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

/// <summary>Courier's use cases «Принять заказ в доставку» and «Подтвердить доставку».</summary>
public sealed partial class CourierTasksViewModel(IDeliveryService delivery, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<CourierTaskItem> Tasks { get; } = [];

  [ObservableProperty]
  public partial bool ShowFinished { get; set; }

  [ObservableProperty]
  public partial bool IsRefreshing { get; set; }

  public override Task OnAppearingAsync() => LoadAsync();

  partial void OnShowFinishedChanged(bool value) => _ = LoadAsync();

  [RelayCommand]
  private async Task RefreshAsync() {
    await LoadAsync();
    IsRefreshing = false;
  }

  [RelayCommand]
  private async Task AcceptAsync(CourierTaskItem item) {
    if (await ExecuteAsync(() => delivery.AcceptAsync(item.Task.OrderId)) is not null) {
      await LoadAsync();
    }
  }

  [RelayCommand]
  private async Task ConfirmDeliveredAsync(CourierTaskItem item) {
    if (!await Dialogs.ConfirmAsync("Доставка", $"Подтвердить вручение {item.Number}?")) {
      return;
    }

    if (await ExecuteAsync(() => delivery.ConfirmDeliveredAsync(item.Task.OrderId)) is not null) {
      await LoadAsync();
    }
  }

  [RelayCommand]
  private async Task ReportFailedAsync(CourierTaskItem item) {
    var reason = await Dialogs.PromptAsync("Вручение не состоялось", "Укажите причину");
    if (string.IsNullOrWhiteSpace(reason)) {
      return;
    }

    if (await ExecuteAsync(() => delivery.ReportFailedAsync(new ReportDeliveryFailedRequest(item.Task.OrderId, reason.Trim()))) is not null) {
      await LoadAsync();
    }
  }

  private async Task LoadAsync() {
    var tasks = await ExecuteAsync(() => delivery.GetMyTasksAsync(activeOnly: !ShowFinished));
    if (tasks is null) {
      return;
    }

    Tasks.Clear();
    foreach (var task in tasks.OrderByDescending(x => x.AssignedAt)) {
      Tasks.Add(CourierTaskItem.From(task));
    }
  }
}
