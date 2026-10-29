using Nonamebtww.Shop.Mobile.Views.Admin;
using Nonamebtww.Shop.Mobile.Views.Auth;
using Nonamebtww.Shop.Mobile.Views.Cart;
using Nonamebtww.Shop.Mobile.Views.Catalog;
using Nonamebtww.Shop.Mobile.Views.Orders;
using Nonamebtww.Shop.Mobile.Views.Profile;

namespace Nonamebtww.Shop.Mobile;

public partial class AppShell : Shell {
  public AppShell() {
    InitializeComponent();

    Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
    Routing.RegisterRoute(Routes.ForgotPassword, typeof(ForgotPasswordPage));

    Routing.RegisterRoute(Routes.Product, typeof(ProductPage));
    Routing.RegisterRoute(Routes.Checkout, typeof(CheckoutPage));
    Routing.RegisterRoute(Routes.OrderDetails, typeof(OrderDetailsPage));
    Routing.RegisterRoute(Routes.ChangePassword, typeof(ChangePasswordPage));

    Routing.RegisterRoute(Routes.Admin, typeof(AdminHubPage));
    Routing.RegisterRoute(Routes.AdminCatalog, typeof(CatalogManagementPage));
    Routing.RegisterRoute(Routes.AdminProductEdit, typeof(ProductEditPage));
    Routing.RegisterRoute(Routes.AdminCategoryEdit, typeof(CategoryEditPage));
    Routing.RegisterRoute(Routes.AdminUsers, typeof(UsersPage));
    Routing.RegisterRoute(Routes.AdminUser, typeof(UserDetailsPage));
    Routing.RegisterRoute(Routes.DeliveryDistribution, typeof(DeliveryDistributionPage));
    Routing.RegisterRoute(Routes.CourierTasks, typeof(CourierTasksPage));
  }
}
