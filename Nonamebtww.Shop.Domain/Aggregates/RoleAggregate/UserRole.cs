namespace Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

/// <summary>Роли пользователей системы.</summary>
public enum UserRole {
  /// <summary>Покупатель.</summary>
  Customer = 0,

  /// <summary>Менеджер по продажам: ведёт каталог.</summary>
  SalesManager = 1,

  /// <summary>Владелец магазина: полный доступ, управляет правами.</summary>
  Owner = 2,

  /// <summary>Логист: распределяет заказы по курьерам.</summary>
  Logistician = 3,

  /// <summary>Курьер: доставляет заказы.</summary>
  Courier = 4,
}

public static class UserRoleExtensions {
  public static string ToDisplayName(this UserRole role) => role switch {
    UserRole.Customer => "Покупатель",
    UserRole.SalesManager => "Менеджер по продажам",
    UserRole.Owner => "Владелец",
    UserRole.Logistician => "Логист",
    UserRole.Courier => "Курьер",
    _ => role.ToString(),
  };

  /// <summary>Сотрудник магазина (не покупатель).</summary>
  public static bool IsStaff(this UserRole role) => role != UserRole.Customer;

  /// <summary>Может управлять каталогом: товары, категории, цены, остатки.</summary>
  public static bool CanManageCatalog(this UserRole role) => role is UserRole.SalesManager or UserRole.Owner;

  /// <summary>Может назначать курьеров на заказы.</summary>
  public static bool CanAssignCouriers(this UserRole role) => role is UserRole.Logistician or UserRole.Owner;

  /// <summary>Может менять права и блокировать пользователей.</summary>
  public static bool CanManageUsers(this UserRole role) => role == UserRole.Owner;

  /// <summary>Может оформлять заказы, вести корзину и избранное, писать отзывы.</summary>
  public static bool CanShop(this UserRole role) => role == UserRole.Customer;
}
