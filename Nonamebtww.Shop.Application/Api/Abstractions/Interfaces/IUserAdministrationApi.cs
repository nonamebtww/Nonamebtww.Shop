using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface IUserAdministrationApi {
  Task<Result<PagedList<UserSummaryDto>>> GetUsersAsync(UserListQuery query, CancellationToken ct = default);
  Task<Result<UserSummaryDto>> ChangeRoleAsync(ChangeRoleRequest request, CancellationToken ct = default);
  Task<Result<UserSummaryDto>> BlockAsync(BlockUserRequest request, CancellationToken ct = default);
  Task<Result<UserSummaryDto>> UnblockAsync(Guid userId, CancellationToken ct = default);
}
