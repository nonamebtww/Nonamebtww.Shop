using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Services.Implementations.Auth;

internal static class UserLookup {
  /// <summary>Ищет пользователя по логину, email или телефону.</summary>
  public static async Task<User?> FindByIdentifierAsync(this IUsersRepository users, string identifier, CancellationToken ct) {
    var value = identifier.Trim();

    try {
      if (value.Contains('@')) {
        return await users.GetByEmailAsync(new Email(value), ct);
      }

      if (value.StartsWith('+') || (value.Length >= 10 && value.All(c => char.IsDigit(c) || c is ' ' or '-' or '(' or ')'))) {
        return await users.GetByPhoneAsync(new PhoneNumber(value), ct);
      }

      return await users.GetByLoginAsync(new Login(value), ct);
    } catch (DomainException) {
      return null;
    }
  }
}
