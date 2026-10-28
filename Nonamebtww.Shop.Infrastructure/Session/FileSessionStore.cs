using System.Text.Json;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Session;

/// <summary>
/// Хранит текущую сессию в JSON-файле в папке приложения.
/// Слой Infrastructure не зависит от MAUI, поэтому SecureStorage здесь не используется:
/// Mobile при желании может зарегистрировать свою реализацию ISessionStore поверх SecureStorage.
/// </summary>
public sealed class FileSessionStore : ISessionStore {
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

  public async Task<StoredSession?> LoadAsync(CancellationToken ct = default) {
    await _lock.WaitAsync(ct);

    try {
      if (!_loaded) {
        _cached = await ReadAsync(ct);
        _loaded = true;
      }

      return _cached;
    } finally {
      _lock.Release();
    }
  }

  public async Task SaveAsync(StoredSession session, CancellationToken ct = default) {
    ArgumentNullException.ThrowIfNull(session);
    await _lock.WaitAsync(ct);

    try {
      await using (var stream = File.Create(_path)) {
        await JsonSerializer.SerializeAsync(stream, session, JsonOptions, ct);
      }

      _cached = session;
      _loaded = true;
    } finally {
      _lock.Release();
    }
  }

  public async Task ClearAsync(CancellationToken ct = default) {
    await _lock.WaitAsync(ct);

    try {
      File.Delete(_path);
      _cached = null;
      _loaded = true;
    } finally {
      _lock.Release();
    }
  }

  private async Task<StoredSession?> ReadAsync(CancellationToken ct) {
    if (!File.Exists(_path)) {
      return null;
    }

    try {
      await using var stream = File.OpenRead(_path);
      return await JsonSerializer.DeserializeAsync<StoredSession>(stream, JsonOptions, ct);
    } catch (JsonException) {
      // Повреждённый файл сессии — считаем, что сессии нет.
      return null;
    }
  }
}
