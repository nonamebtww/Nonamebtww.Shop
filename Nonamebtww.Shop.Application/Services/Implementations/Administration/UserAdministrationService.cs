using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Services.Implementations.Administration;

/// <summary>Изменение прав (DFD 6) и Блокировка аккаунта (DFD 5). Только Владелец.</summary>
public sealed class UserAdministrationService(
  ICurrentUserService currentUser,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  INotificationService notifications,
  IClock clock) : ApplicationService(currentUser), IUserAdministrationService {

  public Task<Result<PagedList<UserSummaryDto>>> GetUsersAsync(UserListQuery query, CancellationToken ct = default) =>
    Run<PagedList<UserSummaryDto>>(async () => {
      var owner = RequireUser(r => r.CanManageUsers());
      if (owner.IsFailure) {
        return Result.Failure<PagedList<UserSummaryDto>>(owner.Error!);
      }

      IEnumerable<User> all = (await users.GetAllAsync(ct)).Where(u => !u.IsDeleted);

      if (!string.IsNullOrWhiteSpace(query.Search)) {
        var s = query.Search.Trim();
        all = all.Where(u => u.Login.Value.Contains(s, StringComparison.OrdinalIgnoreCase)
          || (u.Email?.Value.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
          || (u.Phone?.Value.Contains(s) ?? false)
          || u.FullName.Contains(s, StringComparison.OrdinalIgnoreCase));
      }

      if (query.Role is { } role) {
        all = all.Where(u => u.Role == role);
      }

      if (query.IsBlocked is { } blocked) {
        all = all.Where(u => u.IsBlocked == blocked);
      }

      var list = all.OrderBy(u => u.Role).ThenBy(u => u.Login.Value).ToList();
      var page = Math.Max(1, query.Page);
      var size = Math.Clamp(query.PageSize, 1, 200);
      var items = list.Skip((page - 1) * size).Take(size).Select(u => u.ToSummary()).ToList();

      return new PagedList<UserSummaryDto>(items, page, size, list.Count);
    });

  public Task<Result<UserSummaryDto>> ChangeRoleAsync(ChangeRoleRequest request, CancellationToken ct = default) =>
    Run<UserSummaryDto>(async () => {
      var loaded = await LoadTargetAsync(request.UserId, ct);
      if (loaded.IsFailure) {
        return Result.Failure<UserSummaryDto>(loaded.Error!);
      }

      var user = loaded.Value;
      user.ChangeRole(request.Role);
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await notifications.SendToContactAsync(user, "Изменены права доступа",
        $"Ваша роль: {request.Role.ToDisplayName()}. Войдите в аккаунт заново.", ct);

      return user.ToSummary();
    });

  public Task<Result<UserSummaryDto>> BlockAsync(BlockUserRequest request, CancellationToken ct = default) =>
    Run<UserSummaryDto>(async () => {
      if (BlockUserRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<UserSummaryDto>(invalid);
      }

      var loaded = await LoadTargetAsync(request.UserId, ct);
      if (loaded.IsFailure) {
        return Result.Failure<UserSummaryDto>(loaded.Error!);
      }

      var user = loaded.Value;
      user.Block(request.Reason, clock.UtcNow);
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await notifications.SendToContactAsync(user, "Аккаунт заблокирован",
        $"Ваш аккаунт заблокирован. Причина: {request.Reason}", ct);

      return user.ToSummary();
    });

  public Task<Result<UserSummaryDto>> UnblockAsync(Guid userId, CancellationToken ct = default) =>
    Run<UserSummaryDto>(async () => {
      var loaded = await LoadTargetAsync(userId, ct);
      if (loaded.IsFailure) {
        return Result.Failure<UserSummaryDto>(loaded.Error!);
      }

      var user = loaded.Value;
      user.Unblock();
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await notifications.SendToContactAsync(user, "Аккаунт разблокирован", "Доступ к вашему аккаунту восстановлен.", ct);

      return user.ToSummary();
    });

  /// <summary>Проверяет права Владельца и загружает другого пользователя (себя менять нельзя).</summary>
  private async Task<Result<User>> LoadTargetAsync(Guid userId, CancellationToken ct) {
    var owner = RequireUser(r => r.CanManageUsers());
    if (owner.IsFailure) {
      return Result.Failure<User>(owner.Error!);
    }

    if (owner.Value == userId) {
      return Error.Conflict("users.self", "Нельзя менять права или блокировать собственный аккаунт.");
    }

    var user = await users.GetByIdAsync(userId, ct);
    return user is null || user.IsDeleted ? Errors.NotFound("user") : user;
  }
}
