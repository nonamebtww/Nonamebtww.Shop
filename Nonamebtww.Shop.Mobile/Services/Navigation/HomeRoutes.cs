using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Mobile.Services.Navigation;

public static class HomeRoutes {
  /// <summary>First screen after sign-in: customers see the catalog, staff see their panel.</summary>
  public static string For(CurrentUserDto user) => user.Role.IsStaff()
    ? $"//{Routes.Main}/{Routes.Profile}/{Routes.Admin}"
    : $"//{Routes.Main}/{Routes.Catalog}";

  public static Task GoHomeAsync(this INavigationService navigation, CurrentUserDto user) => navigation.GoToAsync(For(user));
}
