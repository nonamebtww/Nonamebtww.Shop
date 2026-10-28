using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

public interface IRolesRepository : IRepository<Guid, Role> {
  public Task<Role?> GetByKindAsync(UserRole kind, CancellationToken cancellationToken = default);
}
