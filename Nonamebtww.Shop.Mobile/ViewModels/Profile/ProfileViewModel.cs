using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Profile;

/// <summary>
/// Use case «Управление профилем»: account header and menu. Personal data and devices are on a separate page,
/// notifications open from the bell in the toolbar, the title bar searches the catalog.
/// </summary>
public sealed partial class ProfileViewModel(
  IProfileService profile,
  IAuthService auth,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  private bool _loaded;

  [Reactive]
  public partial string DisplayName { get; set; } = string.Empty;

  [Reactive]
  public partial string Initials { get; set; } = string.Empty;

  [Reactive]
  public partial string LoginText { get; set; } = string.Empty;

  [Reactive]
  public partial string RoleText { get; set; } = string.Empty;

  [Reactive]
  public partial string RegisteredText { get; set; } = string.Empty;

  [Reactive]
  public partial bool IsStaff { get; set; }

  /// <summary>The shop owner's account cannot be deleted.</summary>
  [Reactive]
  public partial bool CanDeleteAccount { get; set; }

  [Reactive]
  public partial string SearchText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => LoadWithSkeletonAsync(async () => {
    var dto = await ExecuteAsync(() => cache.GetAsync(CacheKeys.Profile, () => profile.GetAsync()), showBusy: false);
    if (dto is not null) {
      Apply(dto);
      _loaded = true;
    }
  }, () => _loaded || cache.Contains(CacheKeys.Profile));

  /// <summary>Search from the title bar opens the catalog with the query.</summary>
  [ReactiveCommand]
  private async Task SearchAsync() {
    var query = SearchText.Trim();
    SearchText = string.Empty;
    await Navigation.GoToAsync($"//{Routes.Main}/{Routes.Catalog}", new Dictionary<string, object> { ["query"] = query });
  }

  [ReactiveCommand]
  private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

  [ReactiveCommand]
  private Task OpenPersonalDataAsync() => Navigation.GoToAsync(Routes.PersonalData);

  [ReactiveCommand]
  private Task ChangePasswordAsync() => Navigation.GoToAsync(Routes.ChangePassword);

  [ReactiveCommand]
  private Task DeleteAccountAsync() => Navigation.GoToAsync(Routes.DeleteAccount);

  [ReactiveCommand]
  private Task OpenOrdersAsync() => Navigation.GoToRootAsync($"{Routes.Main}/{Routes.Orders}");

  [ReactiveCommand]
  private Task OpenStaffPanelAsync() => Navigation.GoToAsync(Routes.Admin);

  [ReactiveCommand]
  private async Task LogoutAsync() {
    if (!await Dialogs.ConfirmAsync("Выход", "Выйти из аккаунта?")) {
      return;
    }

    if (await ExecuteAsync(() => auth.LogoutAsync())) {
      cache.Invalidate();
      _loaded = false;
      await Navigation.GoToRootAsync(Routes.Login);
    }
  }

  private void Apply(ProfileDto dto) {
    var fullName = $"{dto.FirstName} {dto.LastName}".Trim();
    DisplayName = string.IsNullOrWhiteSpace(fullName) ? dto.Login : fullName;
    Initials = string.Concat(new[] { dto.FirstName, dto.LastName }
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Select(x => char.ToUpperInvariant(x![0])));
    if (Initials.Length == 0) {
      Initials = dto.Login[..1].ToUpperInvariant();
    }

    LoginText = $"@{dto.Login}" + (dto.IsConfirmed ? string.Empty : " · аккаунт не подтверждён");
    RoleText = dto.Role.ToDisplayName();
    RegisteredText = $"С нами с {DisplayText.Date(dto.RegisteredAt)}";
    IsStaff = dto.Role.IsStaff();
    CanDeleteAccount = dto.Role != UserRole.Owner;
  }
}
