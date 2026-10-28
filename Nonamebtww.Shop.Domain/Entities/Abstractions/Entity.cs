using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Entities.Abstractions;

/// <summary>Базовая сущность: равенство по типу и идентификатору.</summary>
public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>> where TKey : struct {
  public TKey Id { get; init; }

  protected Entity() { }

  protected Entity(TKey id) {
    Id = id;
  }

  private bool IsTransient => Id.Equals(default(TKey));

  public bool Equals(Entity<TKey>? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (other.GetType() != GetType() || IsTransient || other.IsTransient) return false;
    return Id.Equals(other.Id);
  }

  public override bool Equals(object? obj) => obj is Entity<TKey> other && Equals(other);

  public override int GetHashCode() => HashCode.Combine(GetType(), Id);

  public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right) => Equals(left, right);

  public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) => !Equals(left, right);
}
