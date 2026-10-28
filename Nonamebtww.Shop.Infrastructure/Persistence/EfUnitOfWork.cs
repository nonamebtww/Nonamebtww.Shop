using Microsoft.EntityFrameworkCore;
using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Infrastructure.Events;

namespace Nonamebtww.Shop.Infrastructure.Persistence;

/// <summary>
/// Единица работы: одна транзакция SaveChanges на все изменённые агрегаты контекста,
/// затем публикация накопленных доменных событий.
/// </summary>
internal sealed class EfUnitOfWork : IUnitOfWork {
  private readonly ShopDbContext _db;
  private readonly DomainEventDispatcher _dispatcher;

  public EfUnitOfWork(ShopDbContext db, DomainEventDispatcher dispatcher) {
    _db = db;
    _dispatcher = dispatcher;
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
    var aggregates = _db.ChangeTracker
      .Entries()
      .Select(e => e.Entity)
      .OfType<IAggregateRoot>()
      .Where(a => a.DomainEvents.Count > 0)
      .ToList();

    var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

    int saved;

    try {
      saved = await _db.SaveChangesAsync(cancellationToken);
    } catch (DbUpdateException ex) {
      // Контекст живёт долго (в MAUI scope = всё приложение), поэтому несохранённые изменения
      // выбрасываем, чтобы они не «доехали» в БД со следующим сохранением.
      _db.ChangeTracker.Clear();

      if (IsUniqueViolation(ex)) {
        // Нарушение альтернативного ключа (логин, email, артикул...) — бизнес-ошибка, а не сбой.
        throw new DomainException("Запись с такими данными уже существует.", ex);
      }

      throw;
    }

    foreach (var aggregate in aggregates) {
      aggregate.ClearDomainEvents();
    }

    await _dispatcher.DispatchAsync(events, cancellationToken);
    return saved;
  }

  private static bool IsUniqueViolation(DbUpdateException ex) =>
    ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
}
