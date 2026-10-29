using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>
/// Owner's list of users for «Изменить права» and «Заблокировать аккаунт»
/// (by login, email, phone number or user ID).
/// </summary>
public sealed partial class UsersViewModel : ViewModelBase {
  private const string ByLogin = "По логину";
  private const string ByEmail = "По почте";
  private const string ByPhone = "По номеру телефона";
  private const string ById = "По ID пользователя";

  private static readonly Option<UserRole?> AllRoles = new(null, "Все роли");

  private readonly IUserAdministrationService administration;
  private readonly RemoteDataCache cache;

  public UsersViewModel(IUserAdministrationService administration, RemoteDataCache cache, INavigationService navigation, IDialogService dialogs)
    : base(navigation, dialogs) {
    this.administration = administration;
    this.cache = cache;
    // Changing the role filter reloads the list.
    this.WhenAnyValue(x => x.SelectedRoleFilter).Skip(1).Subscribe(changed => _ = LoadAsync());
  }

  public ObservableCollection<UserItem> Users { get; } = [];

  public IReadOnlyList<Option<UserRole?>> RoleFilters { get; } =
    Enum.GetValues<UserRole>().Select(x => new Option<UserRole?>(x, x.ToDisplayName())).Prepend(AllRoles).ToList();

  [Reactive]
  public partial string SearchText { get; set; } = string.Empty;

  [Reactive]
  public partial Option<UserRole?>? SelectedRoleFilter { get; set; }

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  public override Task OnAppearingAsync() => LoadAsync();

  [ReactiveCommand]
  private Task SearchAsync() => LoadAsync();

  [ReactiveCommand]
  private async Task RefreshAsync() {
    cache.Invalidate(CacheKeys.Users);
    await LoadAsync(showSkeleton: false);
    IsRefreshing = false;
  }

  [ReactiveCommand]
  private Task OpenAsync(UserItem item) =>
    Navigation.GoToAsync(Routes.AdminUser, new Dictionary<string, object> { ["user"] = item.User });

  [ReactiveCommand]
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
      cache.Invalidate(CacheKeys.Users);
      await Dialogs.AlertAsync("Готово", $"@{user.Login} заблокирован.");
      await LoadAsync(showSkeleton: false);
    }
  }

  private Task LoadAsync(bool showSkeleton = true) {
    var query = new UserListQuery(
      Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
      Role: SelectedRoleFilter?.Value,
      PageSize: 200);
    var key = CacheKeys.Users + query;

    return LoadWithSkeletonAsync(async () => {
      var page = await ExecuteAsync(() => cache.GetAsync<PagedList<UserSummaryDto>>(key, () => administration.GetUsersAsync(query)), showBusy: false);
      if (page is null) {
        return;
      }

      Users.Clear();
      foreach (var user in page.Items) {
        Users.Add(UserItem.From(user));
      }
    }, () => !showSkeleton || cache.Contains(key));
  }

  private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
