namespace Nonamebtww.Shop.Application.Entities;

/// <summary>Запись локального кэша ответов (сериализованный DTO).</summary>
public class Cache {
  public required string Key { get; init; }
  public required string Payload { get; set; }
  public DateTimeOffset StoredAt { get; set; }
  public DateTimeOffset? ExpiresAt { get; set; }

  public bool IsFresh(DateTimeOffset now) => ExpiresAt is null || ExpiresAt > now;
}
