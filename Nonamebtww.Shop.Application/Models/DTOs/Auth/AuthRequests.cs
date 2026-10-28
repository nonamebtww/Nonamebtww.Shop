using Nonamebtww.Shop.Application.Models.Enums;

namespace Nonamebtww.Shop.Application.Models.DTOs.Auth;

/// <summary>«Данные регистрации».</summary>
public sealed record RegisterRequest(
  string Login,
  string Email,
  string? Phone,
  string Password,
  string FirstName,
  string? LastName);

/// <summary>Регистрация начата: код подтверждения отправлен на SentTo.</summary>
public sealed record RegistrationPending(Guid UserId, string SentTo);

public sealed record ConfirmRegistrationRequest(Guid UserId, string Code);

/// <summary>«Данные входа». Identifier — логин, email или телефон.</summary>
public sealed record LoginRequest(string Identifier, string Password);

public sealed record ExternalLoginRequest(ExternalAuthProvider Provider, string? IdToken = null);

/// <summary>«Данные восстановления», шаг 1: куда отправить код.</summary>
public sealed record PasswordResetRequest(string Identifier);

public sealed record PasswordResetPending(string SentTo);

/// <summary>«Данные восстановления», шаг 2: код и новый пароль.</summary>
public sealed record ResetPasswordRequest(string Identifier, string Code, string NewPassword);
