namespace Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

/// <summary>Сущность с датой создания и последнего изменения.</summary>
public interface IAuditable {
  public DateTimeOffset CreatedAt { get; }
  public DateTimeOffset? UpdatedAt { get; }
}
