using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

public interface IUserManager : IReadOnlyRepository<Guid, User> {
  // TODO
}