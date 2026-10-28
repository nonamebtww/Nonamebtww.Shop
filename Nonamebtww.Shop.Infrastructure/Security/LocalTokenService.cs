using System.Security.Cryptography;
using System.Text;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Security;

/// <summary>
/// Локальный выпуск токенов (замена «JWT» с DFD, пока нет сервера).
/// Access-токен: base64url(userId|role|expiresAtUnix|nonce).base64url(HMAC-SHA256).
/// Ключ подписи создаётся один раз и хранится в папке приложения.
/// Когда появится backend, токены будет выдавать сервер, а этот класс уйдёт.
/// </summary>
public sealed class LocalTokenService : ITokenService {
  private readonly IClock _clock;
  private readonly InfrastructureOptions _options;
  private readonly Lazy<byte[]> _key;

  public LocalTokenService(IClock clock, InfrastructureOptions options) {
    _clock = clock;
    _options = options;
    _key = new Lazy<byte[]>(LoadOrCreateKey, LazyThreadSafetyMode.ExecutionAndPublication);
  }

  public AuthTokens Issue(Guid userId, UserRole role) {
    var expiresAt = _clock.UtcNow.Add(_options.AccessTokenLifetime);
    var nonce = Base64Url(RandomNumberGenerator.GetBytes(8));
    var payload = $"{userId:N}|{(int)role}|{expiresAt.ToUnixTimeSeconds()}|{nonce}";
    var payloadPart = Base64Url(Encoding.UTF8.GetBytes(payload));
    var accessToken = $"{payloadPart}.{Sign(payloadPart)}";
    var refreshToken = Base64Url(RandomNumberGenerator.GetBytes(32));

    return new AuthTokens(accessToken, refreshToken, expiresAt);
  }

  public Guid? ValidateAccessToken(string accessToken) {
    if (string.IsNullOrWhiteSpace(accessToken)) {
      return null;
    }

    var parts = accessToken.Split('.');

    if (parts.Length != 2) {
      return null;
    }

    var expected = Encoding.ASCII.GetBytes(Sign(parts[0]));
    var actual = Encoding.ASCII.GetBytes(parts[1]);

    if (!CryptographicOperations.FixedTimeEquals(expected, actual)) {
      return null;
    }

    string payload;

    try {
      payload = Encoding.UTF8.GetString(FromBase64Url(parts[0]));
    } catch (FormatException) {
      return null;
    }

    var fields = payload.Split('|');

    if (fields.Length != 4
        || !Guid.TryParseExact(fields[0], "N", out var userId)
        || !long.TryParse(fields[2], out var expiresAtUnix)) {
      return null;
    }

    return DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix) > _clock.UtcNow ? userId : null;
  }

  private string Sign(string payloadPart) =>
    Base64Url(HMACSHA256.HashData(_key.Value, Encoding.ASCII.GetBytes(payloadPart)));

  private byte[] LoadOrCreateKey() {
    var directory = _options.ResolveDataDirectory();
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "token.key");

    if (File.Exists(path)) {
      var existing = File.ReadAllBytes(path);

      if (existing.Length >= 32) {
        return existing;
      }
    }

    var key = RandomNumberGenerator.GetBytes(64);
    File.WriteAllBytes(path, key);
    return key;
  }

  private static string Base64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

  private static byte[] FromBase64Url(string value) {
    var base64 = value.Replace('-', '+').Replace('_', '/');
    return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
  }
}
