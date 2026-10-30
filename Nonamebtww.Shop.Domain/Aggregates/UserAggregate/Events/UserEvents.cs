using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Events.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate.Events;

public sealed record UserRegistered(Guid UserId, string Login, string? Email, string? Phone, UserRole Role) : DomainEvent;

public sealed record UserConfirmed(Guid UserId) : DomainEvent;

public sealed record UserBlocked(Guid UserId, string Reason) : DomainEvent;

public sealed record UserUnblocked(Guid UserId) : DomainEvent;

public sealed record UserRoleChanged(Guid UserId, UserRole OldRole, UserRole NewRole) : DomainEvent;

public sealed record UserPasswordChanged(Guid UserId) : DomainEvent;

public sealed record UserProfileUpdated(Guid UserId) : DomainEvent;

public sealed record UserEmailChanged(Guid UserId, string? OldEmail, string? NewEmail) : DomainEvent;

public sealed record UserPhoneChanged(Guid UserId, string? OldPhone, string? NewPhone) : DomainEvent;

/// <summary>Аккаунт удалён по просьбе пользователя (DFD 3.5): персональные данные стёрты.</summary>
public sealed record UserDeleted(Guid UserId) : DomainEvent;

public sealed record UserSignedIn(Guid UserId, Guid DeviceId, string DeviceName) : DomainEvent;

/// <summary>Выпущен код подтверждения — его нужно отправить пользователю (на email, а без него — SMS на телефон).</summary>
public sealed record ConfirmationCodeIssued(
  Guid UserId,
  string? Email,
  string? Phone,
  ConfirmationCodePurpose Purpose,
  string Code,
  DateTimeOffset ExpiresAt) : DomainEvent;
