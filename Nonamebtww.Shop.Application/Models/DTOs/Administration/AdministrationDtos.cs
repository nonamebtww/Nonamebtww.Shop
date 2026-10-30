using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Administration;

public sealed record UserSummaryDto(
  Guid Id,
  string Login,
  string? Email,
  string? Phone,
  string FullName,
  UserRole Role,
  bool IsBlocked,
  string? BlockReason,
  DateTimeOffset? BlockedAt,
  DateTimeOffset RegisteredAt);

public sealed record UserListQuery(
  string? Search = null,
  UserRole? Role = null,
  bool? IsBlocked = null,
  int Page = 1,
  int PageSize = 50);

/// <summary>«Пользователь и роль».</summary>
public sealed record ChangeRoleRequest(Guid UserId, UserRole Role);

/// <summary>«Данные блокировки».</summary>
public sealed record BlockUserRequest(Guid UserId, string Reason);
