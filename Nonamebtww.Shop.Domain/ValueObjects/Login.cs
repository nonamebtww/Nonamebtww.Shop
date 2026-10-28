using System.Text.RegularExpressions;
using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>Логин: 3–32 символа, латиница, цифры, «_», «.», «-». Сравнение без учёта регистра.</summary>
public sealed partial record Login {
  public const int MinLength = 3;
  public const int MaxLength = 32;

  public Login(string value) {
    var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
    if (!Pattern().IsMatch(normalized))
      throw new DomainException(
        $"Логин должен содержать от {MinLength} до {MaxLength} символов: латинские буквы, цифры, «_», «.» или «-».");
    Value = normalized;
  }

  public string Value { get; }

  public override string ToString() => Value;

  [GeneratedRegex(@"^[a-z0-9_.\-]{3,32}$")]
  private static partial Regex Pattern();
}
