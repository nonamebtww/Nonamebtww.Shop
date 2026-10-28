using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;

public interface ICategoriesRepository : IRepository<Guid, Category> {
  /// <summary>Категории верхнего уровня.</summary>
  public Task<IReadOnlyList<Category>> GetRootsAsync(CancellationToken cancellationToken = default);

  /// <summary>Прямые подкатегории.</summary>
  public Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
}
