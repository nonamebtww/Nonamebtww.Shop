using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>«Изменить права» (повысить / понизить) and block / unblock one user.</summary>
public sealed partial class UserDetailsViewModel(IUserAdministrationService administration, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private UserSummaryDto? _user;

  public IReadOnlyList<Option<UserRole>> RoleOptions { get; } =
    Enum.GetValues<UserRole>().Select(x => new Option<UserRole>(x, x.ToDisplayName())).ToList();

  [ObservableProperty]
  public partial string NameText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string ContactsText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string RegisteredText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string RoleText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsBlocked { get; set; }

  [ObservableProperty]
  public partial string BlockText { get; set; } = string.Empty;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(ChangeRoleText))]
  public partial Option<UserRole>? SelectedRole { get; set; }

  public string ChangeRoleText => _user is null || SelectedRole is null || SelectedRole.Value == _user.Role
    ? "Изменить права"
    : SelectedRole.Value > _user.Role ? $"Повысить до «{SelectedRole.Caption}»" : $"Понизить до «{SelectedRole.Caption}»";

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    if (query.TryGetValue("user", out var value) && value is UserSummaryDto user) {
      Apply(user);
    }
  }

  [RelayCommand]
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
      Apply(updated);
    }
  }

  [RelayCommand]
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
      Apply(updated);
    }
  }

  [RelayCommand]
  private async Task UnblockAsync() {
    if (_user is null || !await Dialogs.ConfirmAsync("Блокировка", $"Разблокировать @{_user.Login}?")) {
      return;
    }

    var updated = await ExecuteAsync(() => administration.UnblockAsync(_user.Id));
    if (updated is not null) {
      Apply(updated);
    }
  }

  private void Apply(UserSummaryDto user) {
    _user = user;
    Title = $"@{user.Login}";
    NameText = string.IsNullOrWhiteSpace(user.FullName) ? user.Login : user.FullName;
    ContactsText = string.Join(" · ", new[] { $"@{user.Login}", user.Email, user.Phone }.Where(x => !string.IsNullOrWhiteSpace(x)));
    RegisteredText = $"Регистрация: {DisplayText.Date(user.RegisteredAt)} · ID {user.Id}";
    RoleText = $"Текущая роль: {user.Role.ToDisplayName()}";
    IsBlocked = user.IsBlocked;
    BlockText = user.IsBlocked
      ? $"Заблокирован{(user.BlockedAt is { } at ? " " + DisplayText.Date(at) : string.Empty)}: {user.BlockReason}"
      : string.Empty;
    SelectedRole = RoleOptions.First(x => x.Value == user.Role);
  }
}
