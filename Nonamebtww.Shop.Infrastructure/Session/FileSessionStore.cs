using System.Text.Json;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Session;

/// <summary>Данные сохранённой сессии пользователя.</summary>
public sealed record StoredSession(Guid UserId, string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Хранит текущую сессию в JSON-файле в папке приложения.
/// Слой Infrastructure не зависит от MAUI, поэтому SecureStorage здесь не используется:
/// Mobile при желании может зарегистрировать свою реализацию поверх SecureStorage.
/// </summary>
public sealed class FileSessionStore {
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly string _path;
  private readonly SemaphoreSlim _lock = new(1, 1);
  private StoredSession? _cached;
  private bool _loaded;

  public FileSessionStore(InfrastructureOptions options) {
    var directory = options.ResolveDataDirectory();
    Directory.CreateDirectory(directory);
    _path = Path.Combine(directory, "session.json");
  }

  public async Task<StoredSession?> GetAsync(CancellationToken cancellationToken = default) {
    await _lock.WaitAsync(cancellationToken);

    try {
      if (!_loaded) {
        _cached = await ReadAsync(cancellationToken);
        _loaded = true;
      }

      if (_cached is not null && _cached.ExpiresAt <= DateTimeOffset.UtcNow) {
        _cached = null;
        File.Delete(_path);
      }

      return _cached;
    } finally {
      _lock.Release();
    }
  }

  public async Task SaveAsync(StoredSession session, CancellationToken cancellationToken = default) {
    await _lock.WaitAsync(cancellationToken);

    try {
      await using var stream = File.Create(_path);
      await JsonSerializer.SerializeAsync(stream, session, JsonOptions, cancellationToken);
      _cached = session;
      _loaded = true;
    } finally {
      _lock.Release();
    }
  }

  public async Task ClearAsync(CancellationToken cancellationToken = default) {
    await _lock.WaitAsync(cancellationToken);

    try {
      File.Delete(_path);
      _cached = null;
      _loaded = true;
    } finally {
      _lock.Release();
    }
  }

  private async Task<StoredSession?> ReadAsync(CancellationToken cancellationToken) {
    if (!File.Exists(_path)) {
      return null;
    }

    try {
      await using var stream = File.OpenRead(_path);
      return await JsonSerializer.DeserializeAsync<StoredSession>(stream, JsonOptions, cancellationToken);
    } catch (JsonException) {
      return null;
    }
  }
}
