using System.Security.Cryptography;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;

namespace Nonamebtww.Shop.Infrastructure.Security;

/// <summary>
/// Хэширование пароля PBKDF2-SHA256 с солью.
/// Формат строки: "pbkdf2-sha256$&lt;итерации&gt;$&lt;соль base64&gt;$&lt;хэш base64&gt;".
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher {
  private const string Prefix = "pbkdf2-sha256";
  private const int SaltSize = 16;
  private const int HashSize = 32;
  private const int Iterations = 100_000;

  public string Hash(string password) {
    ArgumentException.ThrowIfNullOrEmpty(password);

    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

    return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
  }

  public bool Verify(string password, string hash) {
    if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) {
      return false;
    }

    var parts = hash.Split('$');

    if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations)) {
      return false;
    }

    try {
      var salt = Convert.FromBase64String(parts[2]);
      var expected = Convert.FromBase64String(parts[3]);
      var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

      return CryptographicOperations.FixedTimeEquals(actual, expected);
    } catch (FormatException) {
      return false;
    }
  }
}
