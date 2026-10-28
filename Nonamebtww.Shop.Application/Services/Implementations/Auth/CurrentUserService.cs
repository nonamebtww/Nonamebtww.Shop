using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Application.Services.Implementations.Auth;

/// <summary>Состояние текущего пользователя в памяти. Регистрируется как singleton; меняется только сервисами Application.</summary>
public sealed class CurrentUserService : ICurrentUserService {
  private readonly Lock _lock = new();

  public CurrentUserDto? User { get; private set; }
  public Guid? DeviceId { get; private set; }
  public bool IsAuthenticated => User is not null;
  public Guid? UserId => User?.Id;
  public UserRole? Role => User?.Role;

  public event EventHandler<CurrentUserDto?>? Changed;

  public bool IsInRole(params UserRole[] roles) => User is not null && roles.Contains(User.Role);

  internal void SignIn(CurrentUserDto user, Guid deviceId) {
    lock (_lock) {
      User = user;
      DeviceId = deviceId;
    }

    Changed?.Invoke(this, user);
  }

  internal void Update(CurrentUserDto user) {
    lock (_lock) {
      if (User?.Id != user.Id) {
        return;
      }

      User = user;
    }

    Changed?.Invoke(this, user);
  }

  internal void SignOut() {
    lock (_lock) {
      if (User is null) {
        return;
      }

      User = null;
      DeviceId = null;
    }

    Changed?.Invoke(this, null);
  }
}
