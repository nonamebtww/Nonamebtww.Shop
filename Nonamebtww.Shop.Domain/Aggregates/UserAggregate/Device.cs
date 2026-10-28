using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

/// <summary>Устройство, с которого выполнен вход (IDEF1X «Устройство»), часть агрегата <see cref="User"/>.</summary>
public class Device : Entity<Guid> {
  private Device() { }

  internal Device(string name, string? ipAddress, string refreshToken, DateTimeOffset now) : base(Guid.NewGuid()) {
    Name = Guard.NotEmpty(name, "Название устройства", 128);
    Touch(ipAddress, refreshToken, now);
  }

  public string Name { get; private set; } = null!;
  public string? IpAddress { get; private set; }
  public string? RefreshToken { get; private set; }
  public DateTimeOffset LastSignInAt { get; private set; }
  public bool IsActive { get; private set; }

  internal void Touch(string? ipAddress, string refreshToken, DateTimeOffset now) {
    IpAddress = Guard.Optional(ipAddress, "IP-адрес", 64);
    RefreshToken = Guard.NotEmpty(refreshToken, "Refresh-токен", 1024);
    LastSignInAt = now;
    IsActive = true;
  }

  internal void Deactivate() {
    IsActive = false;
    RefreshToken = null;
  }
}
