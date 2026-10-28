namespace Nonamebtww.Shop.Application.Models.DTOs.Auth;

/// <summary>Пара токенов (на DFD «JWT»). Локально выпускается ITokenService, в будущем — сервером.</summary>
public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
