using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Profile;

public sealed partial class ChangePasswordViewModel(IProfileService profile, INavigationService navigation, IDialogService dialogs)
  : ViewModelBase(navigation, dialogs) {
  [ObservableProperty]
  public partial string CurrentPassword { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string NewPassword { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string ConfirmPassword { get; set; } = string.Empty;

  [RelayCommand]
  private async Task SaveAsync() {
    if (NewPassword != ConfirmPassword) {
      await Dialogs.AlertAsync("Смена пароля", "Пароли не совпадают.");
      return;
    }

    if (await ExecuteAsync(() => profile.ChangePasswordAsync(new ChangePasswordRequest(CurrentPassword, NewPassword)))) {
      await Dialogs.AlertAsync("Смена пароля", "Пароль изменён.");
      await Navigation.GoBackAsync();
    }
  }
}
