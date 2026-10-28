using System.Text.RegularExpressions;

namespace Nonamebtww.Shop.Application.Common.Validation;

/// <summary>Общие правила проверки полей ввода.</summary>
public static partial class Rules {
  public const int MinPasswordLength = 8;
  public const int MaxNameLength = 100;
  public const int MaxTextLength = 2000;

  public static bool IsEmail(string? value) =>
    !string.IsNullOrWhiteSpace(value) && EmailRegex().IsMatch(value);

  /// <summary>Телефон в формате +7XXXXXXXXXX или 8XXXXXXXXXX, допускаются пробелы, скобки и дефисы.</summary>
  public static bool IsPhone(string? value) =>
    !string.IsNullOrWhiteSpace(value) && PhoneRegex().IsMatch(NormalizePhone(value));

  public static bool IsLogin(string? value) =>
    !string.IsNullOrWhiteSpace(value) && LoginRegex().IsMatch(value);

  /// <summary>Не короче 8 символов, есть буква и цифра.</summary>
  public static bool IsStrongPassword(string? value) =>
    value is { Length: >= MinPasswordLength } && value.Any(char.IsLetter) && value.Any(char.IsDigit);

  public static bool IsConfirmationCode(string? value) =>
    value is { Length: 6 } && value.All(char.IsDigit);

  public static string NormalizePhone(string value) {
    var digits = new string(value.Where(c => char.IsDigit(c) || c == '+').ToArray());
    return digits.StartsWith('8') && digits.Length == 11 ? "+7" + digits[1..] : digits;
  }

  public static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

  [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
  private static partial Regex EmailRegex();

  [GeneratedRegex(@"^\+\d{10,15}$")]
  private static partial Regex PhoneRegex();

  [GeneratedRegex(@"^[A-Za-z0-9_.\-]{3,32}$")]
  private static partial Regex LoginRegex();
}
