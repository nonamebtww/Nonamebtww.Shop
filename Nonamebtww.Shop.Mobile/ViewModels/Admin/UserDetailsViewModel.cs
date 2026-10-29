using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>One «Подпись: значение» line of the user card.</summary>
public sealed record InfoRow(string Label, string Value, bool IsWarning = false);

/// <summary>«Изменить права» (повысить / понизить) and block / unblock one user.</summary>
public sealed partial class UserDetailsViewModel(
  IUserAdministrationService administration,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private UserSummaryDto? _user;
  private ObservableAsPropertyHelper<string>? _changeRoleText;

  public IReadOnlyList<Option<UserRole>> RoleOptions { get; } =
    Enum.GetValues<UserRole>().Select(x => new Option<UserRole>(x, x.ToDisplayName())).ToList();

  /// <summary>User card, line by line: «Логин: @buyer», «Email: …», «Роль: …».</summary>
  public ObservableCollection<InfoRow> Info { get; } = [];

  [Reactive]
  public partial string NameText { get; set; } = string.Empty;

  [Reactive]
  public partial UserRole Role { get; set; }

  [Reactive]
  public partial bool IsBlocked { get; set; }

  [Reactive]
  public partial Option<UserRole>? SelectedRole { get; set; }

  public string ChangeRoleText => Derived(ref _changeRoleText, this.WhenAnyValue(x => x.SelectedRole, x => x.Role,
    (selected, role) => selected is null || selected.Value == role
      ? "Изменить права"
      : selected.Value > role ? $"Повысить до «{selected.Caption}»" : $"Понизить до «{selected.Caption}»"));

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    if (query.TryGetValue("user", out var value) && value is UserSummaryDto user) {
      Apply(user);
    }
  }

  [ReactiveCommand]
  private async Task ChangeRoleAsync() {
    if (_user is null || SelectedRole is null || SelectedRole.Value == _user.Role) {
      await Dialogs.AlertAsync("Права", "Выберите новую роль.");
      return;
    }

    if (!await Dialogs.ConfirmAsync("Права", $"{ChangeRoleText} для @{_user.Login}?")) {
      return;
    }

    var updated = await ExecuteAsync(() => administration.ChangeRoleAsync(new ChangeRoleRequest(_user.Id, SelectedRole.Value)));
    if (updated is not null) {
      cache.Invalidate(CacheKeys.Users);
      Apply(updated);
    }
  }

  [ReactiveCommand]
  private async Task BlockAsync() {
    if (_user is null) {
      return;
    }

    var reason = await Dialogs.PromptAsync("Блокировка", $"Причина блокировки @{_user.Login}");
    if (string.IsNullOrWhiteSpace(reason)) {
      return;
    }

    var updated = await ExecuteAsync(() => administration.BlockAsync(new BlockUserRequest(_user.Id, reason.Trim())));
    if (updated is not null) {
      cache.Invalidate(CacheKeys.Users);
      Apply(updated);
    }
  }

  [ReactiveCommand]
  private async Task UnblockAsync() {
    if (_user is null || !await Dialogs.ConfirmAsync("Блокировка", $"Разблокировать @{_user.Login}?")) {
      return;
    }

    var updated = await ExecuteAsync(() => administration.UnblockAsync(_user.Id));
    if (updated is not null) {
      cache.Invalidate(CacheKeys.Users);
      Apply(updated);
    }
  }

  private void Apply(UserSummaryDto user) {
    _user = user;
    Title = $"@{user.Login}";
    NameText = string.IsNullOrWhiteSpace(user.FullName) ? user.Login : user.FullName;
    Role = user.Role;
    IsBlocked = user.IsBlocked;

    Info.Clear();
    Info.Add(new InfoRow("Логин", $"@{user.Login}"));
    Info.Add(new InfoRow("Email", user.Email));
    Info.Add(new InfoRow("Телефон", string.IsNullOrWhiteSpace(user.Phone) ? "не указан" : user.Phone));
    Info.Add(new InfoRow("Роль", user.Role.ToDisplayName()));
    Info.Add(new InfoRow("Дата регистрации", DisplayText.Date(user.RegisteredAt)));
    Info.Add(new InfoRow("Статус", user.IsBlocked ? "заблокирован" : "активен", user.IsBlocked));
    if (user.IsBlocked) {
      if (user.BlockedAt is { } at) {
        Info.Add(new InfoRow("Заблокирован", DisplayText.Date(at), true));
      }

      Info.Add(new InfoRow("Причина блокировки", user.BlockReason ?? "не указана", true));
    }

    Info.Add(new InfoRow("ID", user.Id.ToString()));
    SelectedRole = RoleOptions.First(x => x.Value == user.Role);
  }
}
