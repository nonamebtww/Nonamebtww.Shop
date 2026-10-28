using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

public interface IUsersRepository : IRepository<Guid, User> {
  public Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default);

  public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

  public Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken = default);

  public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}
