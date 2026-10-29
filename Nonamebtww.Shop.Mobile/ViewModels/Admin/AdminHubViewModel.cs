using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

public sealed record StaffTile(string Title, string Description, string Route);

/// <summary>Entry point to staff screens; tiles depend on the role (use case diagram actors).</summary>
public sealed partial class AdminHubViewModel(ICurrentUserService currentUser, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<StaffTile> Tiles { get; } = [];

  [Reactive]
  public partial string RoleText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() {
    Tiles.Clear();
    if (currentUser.Role is not { } role) {
      RoleText = "Вы не вошли в систему";
      return Task.CompletedTask;
    }

    RoleText = $"Ваша роль: {role.ToDisplayName()}";

    if (role.CanManageCatalog()) {
      Tiles.Add(new StaffTile("Управление каталогом", "Товары и категории: описание, характеристики, фото, цены и остатки", Routes.AdminCatalog));
    }

    if (role.CanManageUsers()) {
      Tiles.Add(new StaffTile("Пользователи и права", "Повышение и понижение прав, блокировка по логину, почте, телефону или ID", Routes.AdminUsers));
    }

    if (role.CanAssignCouriers()) {
      Tiles.Add(new StaffTile("Распределение доставки", "Назначение курьеров на подтверждённые заказы", Routes.DeliveryDistribution));
    }

    if (role == UserRole.Courier) {
      Tiles.Add(new StaffTile("Мои доставки", "Принять заказ в доставку и подтвердить вручение", Routes.CourierTasks));
    }

    return Task.CompletedTask;
  }

  [ReactiveCommand]
  private Task OpenAsync(StaffTile tile) => Navigation.GoToAsync(tile.Route);
}
