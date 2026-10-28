using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

public interface IOrdersRepository : IRepository<Guid, Order> {
  /// <summary>Заказы покупателя, новые первыми.</summary>
  public Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

  public Task<IReadOnlyList<Order>> GetByStatusAsync(
    IReadOnlyCollection<OrderStatus> statuses,
    CancellationToken cancellationToken = default);

  /// <summary>Заказы, назначенные курьеру.</summary>
  public Task<IReadOnlyList<Order>> GetByCourierAsync(Guid courierId, CancellationToken cancellationToken = default);
}
