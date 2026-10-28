using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Profile;

/// <summary>«Данные профиля».</summary>
public sealed record ProfileDto(
  Guid Id,
  string Login,
  string Email,
  string? Phone,
  string FirstName,
  string? LastName,
  UserRole Role,
  DateTimeOffset RegisteredAt,
  bool IsConfirmed);

/// <summary>«Изменения профиля».</summary>
public sealed record UpdateProfileRequest(string FirstName, string? LastName, string Email, string? Phone);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record DeviceDto(Guid Id, string Name, string? IpAddress, DateTimeOffset LastLoginAt, bool IsActive, bool IsCurrent);
