namespace Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

/// <summary>
/// Единица работы: атомарно сохраняет изменения всех репозиториев
/// и публикует накопленные доменные события.
/// </summary>
public interface IUnitOfWork {
  public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
