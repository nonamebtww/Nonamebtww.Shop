namespace Nonamebtww.Shop.Mobile;

/// <summary>Shell routes. Roots use "//", pushed pages are registered in AppShell.</summary>
public static class Routes {
  // Roots
  public const string Startup = "startup";
  public const string Login = "login";
  public const string Main = "main";
  public const string Catalog = "catalog";
  public const string Favorites = "favorites";
  public const string Cart = "cart";
  public const string Orders = "orders";
  public const string Profile = "profile";

  // Auth
  public const string Register = "register";
  public const string ForgotPassword = "forgot-password";

  // Shopping
  public const string Product = "product";
  public const string Checkout = "checkout";
  public const string OrderDetails = "order";
  public const string ChangePassword = "change-password";

  // Staff
  public const string Admin = "admin";
  public const string AdminCatalog = "admin-catalog";
  public const string AdminProductEdit = "admin-product";
  public const string AdminCategoryEdit = "admin-category";
  public const string AdminUsers = "admin-users";
  public const string AdminUser = "admin-user";
  public const string DeliveryDistribution = "delivery-distribution";
  public const string CourierTasks = "courier-tasks";

  public static class Params {
    public const string Id = "id";
  }
}
