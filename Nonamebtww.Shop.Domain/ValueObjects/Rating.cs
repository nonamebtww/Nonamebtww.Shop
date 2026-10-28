using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>Оценка товара в отзыве: от 1 до 5.</summary>
public sealed record Rating {
  public const int Min = 1;
  public const int Max = 5;

  public Rating(int value) {
    if (value is < Min or > Max) throw new DomainException($"Оценка должна быть от {Min} до {Max}.");
    Value = value;
  }

  public int Value { get; }

  public override string ToString() => Value.ToString();
}
