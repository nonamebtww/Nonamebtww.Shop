using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Profile;

/// <summary>Use case «Удаление аккаунта» (3.5): confirmation with the password, then sign-out to the login screen.</summary>
public sealed partial class DeleteAccountViewModel(
  IProfileService profile,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  [Reactive]
  public partial string Password { get; set; } = string.Empty;

  [ReactiveCommand]
  private async Task DeleteAsync() {
    if (!await Dialogs.ConfirmAsync("Удаление аккаунта", "Удалить аккаунт без возможности восстановления?", "Удалить", "Отмена")) {
      return;
    }

    if (await ExecuteAsync(() => profile.DeleteAccountAsync(new DeleteAccountRequest(Password)))) {
      Password = string.Empty;
      cache.Invalidate();
      await Dialogs.AlertAsync("Удаление аккаунта", "Аккаунт удалён.");
      await Navigation.GoToRootAsync(Routes.Login);
    }
  }
}
