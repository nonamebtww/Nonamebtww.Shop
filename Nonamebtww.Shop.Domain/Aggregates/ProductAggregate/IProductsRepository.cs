using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;

public interface IProductsRepository : IRepository<Guid, Product> {
  public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);

  public Task<IReadOnlyList<Product>> GetByIdsAsync(
    IEnumerable<Guid> ids,
    CancellationToken cancellationToken = default);

  public Task<IReadOnlyList<Product>> GetByCategoryAsync(
    Guid categoryId,
    bool onlyActive = true,
    CancellationToken cancellationToken = default);

  /// <summary>Поиск по названию, артикулу и описанию.</summary>
  public Task<IReadOnlyList<Product>> SearchAsync(
    string text,
    bool onlyActive = true,
    CancellationToken cancellationToken = default);
}
