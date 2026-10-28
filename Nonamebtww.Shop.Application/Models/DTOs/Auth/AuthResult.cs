namespace Nonamebtww.Shop.Application.Models.DTOs.Auth;

/// <summary>Результат входа или регистрации: пользователь и «JWT».</summary>
public sealed record AuthResult(CurrentUserDto User, AuthTokens Tokens);
