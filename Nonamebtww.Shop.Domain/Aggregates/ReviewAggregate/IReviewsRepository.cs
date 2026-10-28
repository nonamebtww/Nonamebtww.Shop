using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;

public interface IReviewsRepository : IRepository<Guid, Review> {
  public Task<IReadOnlyList<Review>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);

  public Task<IReadOnlyList<Review>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

  public Task<Review?> GetByUserAndProductAsync(
    Guid userId,
    Guid productId,
    CancellationToken cancellationToken = default);
}
