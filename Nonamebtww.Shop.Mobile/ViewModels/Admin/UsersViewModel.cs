using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>
/// Owner's list of users for «Изменить права» and «Заблокировать аккаунт»
/// (by login, email, phone number or user ID).
/// </summary>
public sealed partial class UsersViewModel(IUserAdministrationService administration, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  private const string ByLogin = "По логину";
  private const string ByEmail = "По почте";
  private const string ByPhone = "По номеру телефона";
  private const string ById = "По ID пользователя";

  private static readonly Option<UserRole?> AllRoles = new(null, "Все роли");

  public ObservableCollection<UserItem> Users { get; } = [];

  public IReadOnlyList<Option<UserRole?>> RoleFilters { get; } =
    Enum.GetValues<UserRole>().Select(x => new Option<UserRole?>(x, x.ToDisplayName())).Prepend(AllRoles).ToList();

  [ObservableProperty]
  public partial string SearchText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial Option<UserRole?>? SelectedRoleFilter { get; set; }

  [ObservableProperty]
  public partial bool IsRefreshing { get; set; }

  public override Task OnAppearingAsync() => LoadAsync();

  partial void OnSelectedRoleFilterChanged(Option<UserRole?>? value) => _ = LoadAsync();

  [RelayCommand]
  private Task SearchAsync() => LoadAsync();

  [RelayCommand]
  private async Task RefreshAsync() {
    await LoadAsync();
    IsRefreshing = false;
  }

  [RelayCommand]
  private Task OpenAsync(UserItem item) =>
    Navigation.GoToAsync(Routes.AdminUser, new Dictionary<string, object> { ["user"] = item.User });

  [RelayCommand]
  private async Task BlockByIdentifierAsync() {
    var mode = await Dialogs.ChooseAsync("Заблокировать аккаунт", ByLogin, ByEmail, ByPhone, ById);
    if (mode is null) {
      return;
    }

    var keyboard = mode switch { ByEmail => Keyboard.Email, ByPhone => Keyboard.Telephone, _ => Keyboard.Plain };
    var value = (await Dialogs.PromptAsync("Заблокировать аккаунт", mode, keyboard: keyboard))?.Trim();
    if (string.IsNullOrEmpty(value)) {
      return;
    }

    var page = await ExecuteAsync(() => administration.GetUsersAsync(new UserListQuery(Search: mode == ById ? null : value, PageSize: 500)));
    var user = page?.Items.FirstOrDefault(x => mode switch {
      ByLogin => string.Equals(x.Login, value, StringComparison.OrdinalIgnoreCase),
      ByEmail => string.Equals(x.Email, value, StringComparison.OrdinalIgnoreCase),
      ByPhone => Digits(x.Phone) == Digits(value) || Digits(x.Phone).EndsWith(Digits(value).TrimStart('8', '7')),
      _ => Guid.TryParse(value, out var id) && x.Id == id
    });

    if (user is null) {
      await Dialogs.AlertAsync("Заблокировать аккаунт", "Пользователь не найден.");
      return;
    }

    if (user.IsBlocked) {
      await Dialogs.AlertAsync("Заблокировать аккаунт", $"@{user.Login} уже заблокирован.");
      return;
    }

    var reason = await Dialogs.PromptAsync("Причина блокировки", $"@{user.Login}, {user.FullName}");
    if (string.IsNullOrWhiteSpace(reason)) {
      return;
    }

    if (await ExecuteAsync(() => administration.BlockAsync(new BlockUserRequest(user.Id, reason.Trim()))) is not null) {
      await Dialogs.AlertAsync("Готово", $"@{user.Login} заблокирован.");
      await LoadAsync();
    }
  }

  private async Task LoadAsync() {
    var query = new UserListQuery(
      Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
      Role: SelectedRoleFilter?.Value,
      PageSize: 200);

    var page = await ExecuteAsync(() => administration.GetUsersAsync(query));
    if (page is null) {
      return;
    }

    Users.Clear();
    foreach (var user in page.Items) {
      Users.Add(UserItem.From(user));
    }
  }

  private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
