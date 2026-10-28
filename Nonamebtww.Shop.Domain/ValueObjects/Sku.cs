using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>Артикул товара (уникален в каталоге), хранится в верхнем регистре.</summary>
public sealed record Sku {
  public const int MaxLength = 64;

  public Sku(string value) {
    var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
    if (normalized.Length == 0 || normalized.Length > MaxLength || normalized.Any(char.IsWhiteSpace))
      throw new DomainException($"Артикул должен быть непустым, без пробелов и не длиннее {MaxLength} символов.");
    Value = normalized;
  }

  public string Value { get; }

  public override string ToString() => Value;
}
