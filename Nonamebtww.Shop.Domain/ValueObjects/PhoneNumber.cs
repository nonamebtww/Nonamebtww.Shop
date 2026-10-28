using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>
/// Телефон в формате E.164 (+79991234567). Номер, начинающийся с 8 и из 11 цифр,
/// считается российским и приводится к +7.
/// </summary>
public sealed record PhoneNumber {
  public PhoneNumber(string value) {
    var raw = value?.Trim() ?? string.Empty;
    var digits = new string(raw.Where(char.IsAsciiDigit).ToArray());

    if (!raw.StartsWith('+') && digits.Length == 11 && digits[0] == '8') digits = "7" + digits[1..];
    if (digits.Length is < 10 or > 15) throw new DomainException("Некорректный номер телефона.");

    Value = "+" + digits;
  }

  public string Value { get; }

  public override string ToString() => Value;
}
