using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;

public interface ITokenService {
  AuthTokens Issue(Guid userId, UserRole role);

  /// <summary>Возвращает id пользователя, если токен валиден и не истёк.</summary>
  Guid? ValidateAccessToken(string accessToken);
}
