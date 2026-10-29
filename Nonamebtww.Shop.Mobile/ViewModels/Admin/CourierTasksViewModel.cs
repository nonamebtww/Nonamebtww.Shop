using System.Reactive.Linq;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Delivery;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>Courier's use cases «Принять заказ в доставку» and «Подтвердить доставку».</summary>
public sealed partial class CourierTasksViewModel : ViewModelBase {
  private readonly IDeliveryService delivery;

  public CourierTasksViewModel(IDeliveryService delivery, INavigationService navigation, IDialogService dialogs)
    : base(navigation, dialogs) {
    this.delivery = delivery;
    // Switch «Показать завершённые» reloads the list.
    this.WhenAnyValue(x => x.ShowFinished).Skip(1).Subscribe(changed => _ = LoadAsync());
  }

  public ObservableCollection<CourierTaskItem> Tasks { get; } = [];

  [Reactive]
  public partial bool ShowFinished { get; set; }

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  public override Task OnAppearingAsync() => LoadAsync();

  [ReactiveCommand]
  private async Task RefreshAsync() {
    await LoadAsync();
    IsRefreshing = false;
  }

  [ReactiveCommand]
  private async Task AcceptAsync(CourierTaskItem item) {
    if (await ExecuteAsync(() => delivery.AcceptAsync(item.Task.OrderId)) is not null) {
      await LoadAsync();
    }
  }

  [ReactiveCommand]
  private async Task ConfirmDeliveredAsync(CourierTaskItem item) {
    if (!await Dialogs.ConfirmAsync("Доставка", $"Подтвердить вручение {item.Number}?")) {
      return;
    }

    if (await ExecuteAsync(() => delivery.ConfirmDeliveredAsync(item.Task.OrderId)) is not null) {
      await LoadAsync();
    }
  }

  [ReactiveCommand]
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
