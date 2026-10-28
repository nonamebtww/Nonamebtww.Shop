using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Administration;

/// <summary>Процессы DFD «Изменение прав» и «Блокировка аккаунта». Доступно только Владельцу.</summary>
public interface IUserAdministrationService {
  Task<Result<PagedList<UserSummaryDto>>> GetUsersAsync(UserListQuery query, CancellationToken ct = default);
  Task<Result<UserSummaryDto>> ChangeRoleAsync(ChangeRoleRequest request, CancellationToken ct = default);
  Task<Result<UserSummaryDto>> BlockAsync(BlockUserRequest request, CancellationToken ct = default);
  Task<Result<UserSummaryDto>> UnblockAsync(Guid userId, CancellationToken ct = default);
}
