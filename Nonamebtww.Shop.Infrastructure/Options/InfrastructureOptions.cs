namespace Nonamebtww.Shop.Infrastructure.Options;

/// <summary>Настройки слоя Infrastructure, задаются в AddInfrastructure().</summary>
public sealed class InfrastructureOptions {
  /// <summary>Полный путь к файлу SQLite. На устройстве: Path.Combine(FileSystem.AppDataDirectory, "shop.db3").</summary>
  public string DatabasePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "shop.db3");

  /// <summary>Папка для файлов слоя (сессия и т.п.). По умолчанию рядом с БД.</summary>
  public string? DataDirectory { get; set; }

  /// <summary>Заполнять пустую БД демо-данными при инициализации.</summary>
  public bool SeedDemoData { get; set; } = true;

  /// <summary>Удалять и пересоздавать БД при каждом запуске (удобно при разработке прототипа).</summary>
  public bool RecreateDatabaseOnStartup { get; set; }

  /// <summary>
  /// Точка подмены: false — локальные fake-реализации API поверх SQLite (прототип),
  /// true — HTTP-реализации, которые будут ходить на backend по <see cref="ApiBaseUrl"/>.
  /// </summary>
  public bool UseRemoteApi { get; set; }

  /// <summary>Базовый адрес backend API (используется только при UseRemoteApi = true).</summary>
  public Uri? ApiBaseUrl { get; set; }

  /// <summary>Срок жизни локальной сессии.</summary>
  public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromDays(30);

  /// <summary>Имитация задержки сети в fake-API, чтобы UI показывал индикаторы загрузки.</summary>
  public TimeSpan FakeApiLatency { get; set; } = TimeSpan.FromMilliseconds(150);

  internal string ResolveDataDirectory() =>
    DataDirectory ?? Path.GetDirectoryName(Path.GetFullPath(DatabasePath)) ?? AppContext.BaseDirectory;
}
