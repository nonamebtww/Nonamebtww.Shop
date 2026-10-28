using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Application.Services.Implementations;

/// <summary>Общая основа use case: проверка доступа и перевод доменных исключений в Result.</summary>
public abstract class ApplicationService(ICurrentUserService currentUser) {
  protected ICurrentUserService CurrentUser { get; } = currentUser;

  /// <summary>Id текущего пользователя или ошибка Unauthorized.</summary>
  protected Result<Guid> RequireUser() =>
    CurrentUser.UserId is { } id ? id : Errors.Unauthorized;

  /// <summary>Id текущего пользователя, если его роль удовлетворяет условию.</summary>
  protected Result<Guid> RequireUser(Func<UserRole, bool> isAllowed) {
    if (CurrentUser.UserId is not { } id || CurrentUser.Role is not { } role) {
      return Errors.Unauthorized;
    }

    return isAllowed(role) ? id : Errors.Forbidden;
  }

  protected static async Task<Result<T>> Run<T>(Func<Task<Result<T>>> action) {
    try {
      return await action();
    } catch (DomainException e) {
      return Errors.Domain(e);
    }
  }

  protected static async Task<Result> Execute(Func<Task<Result>> action) {
    try {
      return await action();
    } catch (DomainException e) {
      return Errors.Domain(e);
    }
  }

  /// <summary>Создаёт value object из ввода пользователя; DomainException превращается в ошибку поля.</summary>
  protected static Result<T> Parse<T>(string field, Func<T> create) {
    try {
      return create();
    } catch (DomainException e) {
      return Error.Validation(field, e.Message);
    }
  }
}
