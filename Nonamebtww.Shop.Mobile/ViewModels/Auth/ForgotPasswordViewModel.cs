using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Auth;

/// <summary>Use case «Восстановление пароля»: code to email/phone, then a new password.</summary>
public sealed partial class ForgotPasswordViewModel(IAuthService auth, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  [Reactive]
  public partial string Identifier { get; set; } = string.Empty;

  [Reactive]
  public partial bool IsCodeStep { get; set; }

  [Reactive]
  public partial string CodeHint { get; set; } = string.Empty;

  [Reactive]
  public partial string Code { get; set; } = string.Empty;

  [Reactive]
  public partial string NewPassword { get; set; } = string.Empty;

  [Reactive]
  public partial string ConfirmPassword { get; set; } = string.Empty;

  [ReactiveCommand]
  private async Task SendCodeAsync() {
    var pending = await ExecuteAsync(() => auth.RequestPasswordResetAsync(new PasswordResetRequest(Identifier.Trim())));
    if (pending is null) {
      return;
    }

    CodeHint = $"Код отправлен на {pending.SentTo}. В прототипе он приходит в локальное уведомление.";
    IsCodeStep = true;
  }

  [ReactiveCommand]
  private async Task ResetAsync() {
    if (NewPassword != ConfirmPassword) {
      await Dialogs.AlertAsync("Восстановление пароля", "Пароли не совпадают.");
      return;
    }

    var ok = await ExecuteAsync(() => auth.ResetPasswordAsync(new ResetPasswordRequest(Identifier.Trim(), Code.Trim(), NewPassword)));
    if (!ok) {
      return;
    }

    await Dialogs.AlertAsync("Готово", "Пароль изменён. Войдите с новым паролем.");
    await Navigation.GoBackAsync();
  }
}
