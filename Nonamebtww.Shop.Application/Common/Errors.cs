using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Application.Common;

/// <summary>Типовые ошибки use case.</summary>
public static class Errors {
  public static readonly Error Unauthorized = Error.Unauthorized("auth.required", "Войдите в аккаунт.");
  public static readonly Error Forbidden = Error.Forbidden("auth.forbidden", "Недостаточно прав для этого действия.");
  public static readonly Error InvalidCredentials = Error.Unauthorized("auth.invalid_credentials", "Неверный логин или пароль.");

  public static Error NotFound(string entity) => Error.NotFound($"{entity}.not_found", $"{EntityName(entity)} не найден(а).");

  public static Error Conflict(string code, string message) => Error.Conflict(code, message);

  /// <summary>Нарушение бизнес-правила, сообщение из Domain показывается пользователю как есть.</summary>
  public static Error Domain(DomainException exception) => Error.Failure("domain.rule", exception.Message);

  private static string EntityName(string entity) => entity switch {
    "user" => "Пользователь",
    "product" => "Товар",
    "category" => "Категория",
    "order" => "Заказ",
    "cart_item" => "Позиция корзины",
    "device" => "Устройство",
    "courier" => "Курьер",
    _ => "Объект"
  };
}
