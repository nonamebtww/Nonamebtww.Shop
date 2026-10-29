using System.Collections.Concurrent;
using Nonamebtww.Shop.Application.Common.Results;

namespace Nonamebtww.Shop.Mobile.Services.Data;

/// <summary>
/// Simulates «load from the API and save to the cache»: a cache miss waits for a network-like delay,
/// calls the use case and keeps the successful result in memory for <see cref="TimeToLive"/>.
/// Screens show skeletons only on a miss. Any data change invalidates the cache (see <see cref="Invalidate"/>).
/// </summary>
public sealed class RemoteDataCache {
  private static readonly TimeSpan MinLatency = TimeSpan.FromMilliseconds(600);
  private static readonly TimeSpan MaxLatency = TimeSpan.FromMilliseconds(1200);

  private readonly ConcurrentDictionary<string, Entry> _entries = new();

  public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(2);

  /// <summary>True when a fresh cached value exists for the key.</summary>
  public bool Contains(string key) => _entries.TryGetValue(key, out var entry) && !entry.IsExpired(TimeToLive);

  public async Task<Result<T>> GetAsync<T>(string key, Func<Task<Result<T>>> fetch, bool forceRefresh = false) {
    if (!forceRefresh && _entries.TryGetValue(key, out var entry) && !entry.IsExpired(TimeToLive) && entry.Value is T cached) {
      return Result.Success(cached);
    }

    // «Network» round trip of the future backend API.
    var latency = Random.Shared.NextDouble() * (MaxLatency - MinLatency).TotalMilliseconds + MinLatency.TotalMilliseconds;
    await Task.Delay(TimeSpan.FromMilliseconds(latency));

    var result = await fetch();
    if (result.IsSuccess) {
      _entries[key] = new Entry(result.Value, DateTimeOffset.UtcNow);
    }

    return result;
  }

  /// <summary>Drops cached entries whose key starts with <paramref name="prefix"/> (all entries by default).</summary>
  public void Invalidate(string prefix = "") {
    foreach (var key in _entries.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))) {
      _entries.TryRemove(key, out _);
    }
  }

  private sealed record Entry(object? Value, DateTimeOffset StoredAt) {
    public bool IsExpired(TimeSpan ttl) => DateTimeOffset.UtcNow - StoredAt > ttl;
  }
}

/// <summary>Cache keys of the screens that load data through <see cref="RemoteDataCache"/>.</summary>
public static class CacheKeys {
  public const string Catalog = "catalog:";
  public const string Categories = "categories";
  public const string Product = "product:";
  public const string Orders = "orders:";
  public const string Profile = "profile";
  public const string Devices = "devices";
  public const string Users = "users:";
  public const string ManagedCatalog = "manage:";
}
