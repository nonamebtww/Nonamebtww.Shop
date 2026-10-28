namespace Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

public interface IEntity<TKey> where TKey : struct {
  public TKey Id { get; init; }
}