using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Auth;

/// <summary>Demo account of the prototype seed (see Infrastructure seed data).</summary>
public sealed record DemoAccount(string RoleCaption, string Login, string Password);

/// <summary>Use case «Вход»: login, email or phone + password, or an external provider.</summary>
public sealed partial class LoginViewModel(IAuthService auth, RemoteDataCache cache, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  [Reactive]
  public partial string Identifier { get; set; } = string.Empty;

  [Reactive]
  public partial string Password { get; set; } = string.Empty;

  public IReadOnlyList<DemoAccount> DemoAccounts { get; } = [
    new("Покупатель", "buyer", "Buyer123!"),
    new("Менеджер по продажам", "manager", "Manager123!"),
    new("Владелец", "owner", "Owner123!"),
    new("Логист", "logist", "Logist123!"),
    new("Курьер", "courier", "Courier123!")
  ];

  [ReactiveCommand]
  private async Task LoginAsync() {
    await RunAsync(async () => {
      var result = await auth.LoginAsync(new LoginRequest(Identifier.Trim(), Password));
      if (result.IsSuccess) {
        await OnSignedInAsync(result.Value.User);
        return;
      }

      // Registration was started but not confirmed: continue with the confirmation code step.
      if (result.Error is { Code: "auth.not_confirmed" } error
          && error.Fields.TryGetValue("userId", out var ids)
          && Guid.TryParse(ids.FirstOrDefault(), out var userId)) {
        await Dialogs.AlertAsync("Вход", error.Message);
        await Navigation.GoToAsync(Routes.Register, new Dictionary<string, object> { [Routes.Params.Id] = userId });
        return;
      }

      await ShowErrorAsync(result.Error);
    });
  }

  [ReactiveCommand]
  private async Task ExternalLoginAsync(ExternalAuthProvider provider) {
    var result = await ExecuteAsync(() => auth.ExternalLoginAsync(new ExternalLoginRequest(provider)));
    if (result is not null) {
      await OnSignedInAsync(result.User);
    }
  }

  [ReactiveCommand]
  private void FillDemo(DemoAccount account) {
    Identifier = account.Login;
    Password = account.Password;
  }

  [ReactiveCommand]
  private Task RegisterAsync() => Navigation.GoToAsync(Routes.Register);

  [ReactiveCommand]
  private Task ForgotPasswordAsync() => Navigation.GoToAsync(Routes.ForgotPassword);

  private async Task OnSignedInAsync(CurrentUserDto user) {
    Password = string.Empty;
    // Cached screens belong to the previous account.
    cache.Invalidate();
    await Navigation.GoHomeAsync(user);
  }
}
