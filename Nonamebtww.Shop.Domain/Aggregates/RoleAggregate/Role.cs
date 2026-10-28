using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

/// <summary>
/// Роль (IDEF1X «Роль»). Справочник: на каждое значение <see cref="UserRole"/> — одна запись.
/// <see cref="Kind"/> — альтернативный ключ, по нему ссылается <c>User.Role</c>.
/// </summary>
public class Role : AggregateRoot<Guid> {
  private Role() { }

  public Role(Guid id, UserRole kind, string name, string? description = null) : base(Guard.NotEmpty(id, "id_роли")) {
    Kind = kind;
    Name = Guard.NotEmpty(name, "Название роли", 64);
    Description = Guard.Optional(description, "Описание роли", 512);
  }

  public UserRole Kind { get; private set; }
  public string Name { get; private set; } = null!;
  public string? Description { get; private set; }

  public static Role CreateDefault(UserRole kind) => new(Guid.NewGuid(), kind, kind.ToDisplayName(), DefaultDescription(kind));

  /// <summary>Полный справочник ролей для начального заполнения хранилища.</summary>
  public static IReadOnlyList<Role> CreateDefaults() => Enum.GetValues<UserRole>().Select(CreateDefault).ToList();

  public void Update(string name, string? description) {
    Name = Guard.NotEmpty(name, "Название роли", 64);
    Description = Guard.Optional(description, "Описание роли", 512);
  }

  private static string DefaultDescription(UserRole kind) => kind switch {
    UserRole.Customer => "Просматривает каталог, оформляет и оплачивает заказы",
    UserRole.SalesManager => "Управляет каталогом товаров и категорий",
    UserRole.Owner => "Полный доступ, управление правами пользователей",
    UserRole.Logistician => "Распределяет заказы по курьерам",
    UserRole.Courier => "Доставляет заказы покупателям",
    _ => string.Empty,
  };
}
