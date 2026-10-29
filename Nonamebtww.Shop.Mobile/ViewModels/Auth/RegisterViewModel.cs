using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Auth;

/// <summary>
/// Use case «Регистрация»: login + email + password or login + phone + password,
/// then confirmation code; or registration through Google / Apple ID / Yandex ID.
/// </summary>
public sealed partial class RegisterViewModel(IAuthService auth, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private Guid _pendingUserId;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(UsePhone))]
  public partial bool UseEmail { get; set; } = true;

  public bool UsePhone {
    get => !UseEmail;
    set => UseEmail = !value;
  }

  [ObservableProperty]
  public partial string Login { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Email { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Phone { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string FirstName { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string LastName { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Password { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string ConfirmPassword { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsCodeStep { get; set; }

  [ObservableProperty]
  public partial string CodeHint { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Code { get; set; } = string.Empty;

  /// <summary>Opened from the login screen for an unconfirmed account (the new code is already sent by the login use case).</summary>
  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    if (query.TryGetValue(Routes.Params.Id, out var id) && id is Guid userId) {
      _pendingUserId = userId;
      CodeHint = "Мы отправили новый код подтверждения. В прототипе код приходит в локальное уведомление.";
      IsCodeStep = true;
    }
  }

  [RelayCommand]
  private async Task ResendCodeAsync() {
    var pending = await ExecuteAsync(() => auth.ResendRegistrationCodeAsync(_pendingUserId));
    if (pending is not null) {
      CodeHint = $"Мы отправили код подтверждения на {pending.SentTo}. В прототипе код приходит в локальное уведомление.";
    }
  }

  [RelayCommand]
  private async Task RegisterAsync() {
    if (Password != ConfirmPassword) {
      await Dialogs.AlertAsync("Регистрация", "Пароли не совпадают.");
      return;
    }

    var request = new RegisterRequest(
      Login.Trim(),
      UseEmail ? Email.Trim() : string.Empty,
      UsePhone ? Phone.Trim() : string.Empty,
      Password,
      FirstName.Trim(),
      LastName.Trim());

    var pending = await ExecuteAsync(() => auth.RegisterAsync(request));
    if (pending is null) {
      return;
    }

    _pendingUserId = pending.UserId;
    CodeHint = $"Мы отправили код подтверждения на {pending.SentTo}. В прототипе код приходит в локальное уведомление.";
    IsCodeStep = true;
  }

  [RelayCommand]
  private async Task ConfirmAsync() {
    var result = await ExecuteAsync(() => auth.ConfirmRegistrationAsync(new ConfirmRegistrationRequest(_pendingUserId, Code.Trim())));
    if (result is not null) {
      await Navigation.GoHomeAsync(result.User);
    }
  }

  [RelayCommand]
  private async Task ExternalRegisterAsync(ExternalAuthProvider provider) {
    var result = await ExecuteAsync(() => auth.ExternalLoginAsync(new ExternalLoginRequest(provider)));
    if (result is not null) {
      await Navigation.GoHomeAsync(result.User);
    }
  }
}
