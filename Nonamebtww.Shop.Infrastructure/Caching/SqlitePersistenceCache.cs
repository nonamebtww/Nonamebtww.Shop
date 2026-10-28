using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nonamebtww.Shop.Application.Entities;
using Nonamebtww.Shop.Application.Models.DTOs.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;
using Nonamebtww.Shop.Infrastructure.Persistence;

namespace Nonamebtww.Shop.Infrastructure.Caching;

/// <summary>
/// Персистентный кэш ответов API в той же SQLite (таблица Cache).
/// Пока API локальный, кэш почти не нужен; он пригодится для офлайн-режима с реальным backend.
/// </summary>
public sealed class SqlitePersistenceCache : IPersistenceCache {
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly IDbContextFactory<ShopDbContext> _contextFactory;
  private readonly IClock _clock;

  public SqlitePersistenceCache(IDbContextFactory<ShopDbContext> contextFactory, IClock clock) {
    _contextFactory = contextFactory;
    _clock = clock;
  }

  public async Task<Cached<T>?> GetAsync<T>(string key, CancellationToken ct = default) {
    await using var db = await _contextFactory.CreateDbContextAsync(ct);
    var entry = await db.CacheEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);

    if (entry is null) {
      return null;
    }

    T? value;

    try {
      value = JsonSerializer.Deserialize<T>(entry.Payload, JsonOptions);
    } catch (JsonException) {
      return null;
    }

    return value is null ? null : new Cached<T>(value, entry.StoredAt, entry.IsFresh(_clock.UtcNow));
  }

  public async Task SetAsync<T>(string key, T value, TimeSpan? timeToLive = null, CancellationToken ct = default) {
    await using var db = await _contextFactory.CreateDbContextAsync(ct);
    var now = _clock.UtcNow;
    var payload = JsonSerializer.Serialize(value, JsonOptions);
    var entry = await db.CacheEntries.FirstOrDefaultAsync(x => x.Key == key, ct);

    if (entry is null) {
      db.CacheEntries.Add(new Cache { Key = key, Payload = payload, StoredAt = now, ExpiresAt = now + timeToLive });
    } else {
      entry.Payload = payload;
      entry.StoredAt = now;
      entry.ExpiresAt = now + timeToLive;
    }

    await db.SaveChangesAsync(ct);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default) {
    await using var db = await _contextFactory.CreateDbContextAsync(ct);
    await db.CacheEntries.Where(x => x.Key == key).ExecuteDeleteAsync(ct);
  }

  public async Task ClearAsync(CancellationToken ct = default) {
    await using var db = await _contextFactory.CreateDbContextAsync(ct);
    await db.CacheEntries.ExecuteDeleteAsync(ct);
  }
}
