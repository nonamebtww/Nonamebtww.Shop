using System.Text.RegularExpressions;
using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>Адрес электронной почты, хранится в нижнем регистре.</summary>
public sealed partial record Email {
  public const int MaxLength = 254;

  public Email(string value) {
    var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
    if (normalized.Length == 0 || normalized.Length > MaxLength || !Pattern().IsMatch(normalized))
      throw new DomainException("Некорректный адрес электронной почты.");
    Value = normalized;
  }

  public string Value { get; }

  public override string ToString() => Value;

  [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
  private static partial Regex Pattern();
}
