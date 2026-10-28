namespace Nonamebtww.Shop.Application.Api;

/// <summary>Настройки будущего серверного API. В прототипе UseRemoteApi = false.</summary>
public sealed class ApiOptions {
  public bool UseRemoteApi { get; set; }
  public Uri? BaseAddress { get; set; }
  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
