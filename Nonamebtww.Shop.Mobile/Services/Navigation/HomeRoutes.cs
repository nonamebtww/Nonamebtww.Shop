using Nonamebtww.Shop.Application.Models.DTOs.Auth;

namespace Nonamebtww.Shop.Mobile.Services.Navigation;

public static class HomeRoutes {
  /// <summary>
  /// First screen after sign-in is the buyer catalog for every role. Staff open their panel
  /// from Profile → «Панель сотрудника».
  /// </summary>
  public static string For(CurrentUserDto user) => $"//{Routes.Main}/{Routes.Catalog}";

  public static Task GoHomeAsync(this INavigationService navigation, CurrentUserDto user) => navigation.GoToAsync(For(user));
}
