using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;

/// <summary>Текущий пользователь приложения (ICurrentUser).</summary>
public interface ICurrentUserService {
  CurrentUserDto? User { get; }
  Guid? DeviceId { get; }
  bool IsAuthenticated { get; }
  Guid? UserId { get; }
  UserRole? Role { get; }

  bool IsInRole(params UserRole[] roles);

  /// <summary>Срабатывает при входе, выходе и изменении профиля.</summary>
  event EventHandler<CurrentUserDto?>? Changed;
}
