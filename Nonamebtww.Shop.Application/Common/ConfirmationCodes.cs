using System.Security.Cryptography;

namespace Nonamebtww.Shop.Application.Common;

internal static class ConfirmationCodes {
  public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

  public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

  /// <summary>Маскирует email для показа: p***l@yandex.ru.</summary>
  public static string MaskEmail(string email) {
    var at = email.IndexOf('@');
    if (at <= 1) {
      return email;
    }

    return $"{email[0]}***{email[at - 1]}{email[at..]}";
  }

  /// <summary>Маскирует телефон для показа: +7 *** ***-**-67.</summary>
  public static string MaskPhone(string phone) =>
    phone.Length < 6 ? phone : $"{phone[..2]} *** ***-**-{phone[^2..]}";
}
