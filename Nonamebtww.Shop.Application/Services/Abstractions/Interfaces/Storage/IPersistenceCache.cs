using Nonamebtww.Shop.Application.Models.DTOs.Common;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;

/// <summary>Персистентный кэш ответов (для офлайн-режима, когда появится API).</summary>
public interface IPersistenceCache {
  Task<Cached<T>?> GetAsync<T>(string key, CancellationToken ct = default);
  Task SetAsync<T>(string key, T value, TimeSpan? timeToLive = null, CancellationToken ct = default);
  Task RemoveAsync(string key, CancellationToken ct = default);
  Task ClearAsync(CancellationToken ct = default);
}
