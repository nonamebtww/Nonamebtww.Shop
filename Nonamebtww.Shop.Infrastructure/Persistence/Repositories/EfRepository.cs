using Microsoft.EntityFrameworkCore;
using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Repositories;

/// <summary>
/// Базовый репозиторий агрегата поверх EF Core. Изменения не сохраняются сами:
/// их фиксирует IUnitOfWork.SaveChangesAsync (общий DbContext в пределах scope).
/// Дочерние сущности агрегатов подгружаются автоматически (AutoInclude в конфигурациях).
/// </summary>
internal abstract class EfRepository<TEntity> : IRepository<Guid, TEntity>
  where TEntity : class, IEntity<Guid>, IAggregateRoot {
  protected EfRepository(ShopDbContext db) {
    Db = db;
  }

  protected ShopDbContext Db { get; }
  protected DbSet<TEntity> Set => Db.Set<TEntity>();

  public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await Set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

  public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await Set.ToListAsync(cancellationToken);

  public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
    Set.AnyAsync(x => x.Id == id, cancellationToken);

  public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(entity);
    Set.Add(entity);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(entity);

    // Агрегат, загруженный этим же контекстом, уже отслеживается: EF сам найдёт изменения,
    // включая новые дочерние сущности. Update() нужен только для «отсоединённого» агрегата.
    if (Db.Entry(entity).State == EntityState.Detached) {
      Set.Update(entity);
    }

    return Task.CompletedTask;
  }

  public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(entity);
    Set.Remove(entity);
    return Task.CompletedTask;
  }
}
