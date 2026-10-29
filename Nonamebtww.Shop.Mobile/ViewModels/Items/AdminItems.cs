using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Mobile.Presentation;

namespace Nonamebtww.Shop.Mobile.ViewModels.Items;

public sealed record ManagedProductItem(Guid Id, string Name, string Photo, string Details, bool IsActive, int Stock) {
  public string ToggleText => IsActive ? "Снять" : "Вернуть";

  public static ManagedProductItem From(ProductSimple product) => new(
    product.Id,
    product.Name,
    ImageSources.Photo(product.Photo),
    $"{product.Sku} · {DisplayText.Money(product.Price)} · остаток {product.Stock}" + (product.IsActive ? string.Empty : " · снят с продажи"),
    product.IsActive,
    product.Stock);
}

/// <summary>Category tree flattened for the management list.</summary>
public sealed record CategoryRow(Guid Id, string Name, int Level, int ProductCount) {
  public Thickness Indent => new(Level * 16, 0, 0, 0);
  public string CountText => $"{ProductCount} тов.";

  public static IEnumerable<CategoryRow> Flatten(IEnumerable<CategoryNode> nodes, int level = 0) =>
    nodes.SelectMany(node => Flatten(node.Children, level + 1).Prepend(new CategoryRow(node.Id, node.Name, level, node.ProductCount)));
}

public sealed record UserItem(UserSummaryDto User, string Title, string RoleText, string Contacts, bool IsBlocked, string BlockText) {
  public static UserItem From(UserSummaryDto user) => new(
    user,
    string.IsNullOrWhiteSpace(user.FullName) ? user.Login : $"{user.FullName} (@{user.Login})",
    DisplayText.Of(user.Role),
    string.Join(" · ", new[] { user.Email, user.Phone }.Where(x => !string.IsNullOrWhiteSpace(x))),
    user.IsBlocked,
    user.IsBlocked ? $"Заблокирован: {user.BlockReason}" : string.Empty);
}

public sealed record DispatchItem(
  DispatchOrderDto Order,
  string Number,
  string StatusText,
  Color StatusColor,
  string Customer,
  string Address,
  string Details,
  string CourierText,
  string AssignText) {
  public static DispatchItem From(DispatchOrderDto order) => new(
    order,
    $"Заказ {DisplayText.ShortId(order.OrderId)}",
    DisplayText.Of(order.Status),
    DisplayText.ColorOf(order.Status),
    order.CustomerName + (order.CustomerPhone is { } phone ? $", {phone}" : string.Empty),
    order.Address.ToString(),
    $"{DisplayText.Date(order.CreatedAt)} · товаров {order.ItemsCount} · {DisplayText.Money(order.TotalAmount)}",
    order.CourierName is { } courier ? $"Курьер: {courier}" : "Курьер не назначен",
    order.CourierId is null ? "Назначить курьера" : "Переназначить курьера");
}

public sealed record CourierTaskItem(
  DeliveryTaskDto Task,
  string Number,
  string StatusText,
  string Customer,
  string Address,
  string Details,
  bool CanAccept,
  bool CanFinish) {
  public static CourierTaskItem From(DeliveryTaskDto task) => new(
    task,
    $"Заказ {DisplayText.ShortId(task.OrderId)}",
    DisplayText.Of(task.Status),
    task.CustomerName + (task.CustomerPhone is { } phone ? $", {phone}" : string.Empty),
    task.Address.ToString(),
    $"Товаров {task.ItemsCount} · {DisplayText.Money(task.TotalAmount)} · {(task.IsPaid ? "оплачен" : "не оплачен")}"
      + $" · назначен {DisplayText.Date(task.AssignedAt)}",
    task.Status == DeliveryStatus.Assigned,
    task.Status == DeliveryStatus.Accepted);
}
