using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;

/// <summary>Процессы DFD «Регистрация», «Вход», «Восстановление пароля».</summary>
public interface IAuthService {
  /// <summary>Регистрация, шаг 1: создаёт неподтверждённого пользователя и отправляет код.</summary>
  Task<Result<RegistrationPending>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

  /// <summary>Регистрация, шаг 2: проверяет код, подтверждает аккаунт и выполняет вход.</summary>
  Task<Result<AuthResult>> ConfirmRegistrationAsync(ConfirmRegistrationRequest request, CancellationToken ct = default);

  Task<Result<RegistrationPending>> ResendRegistrationCodeAsync(Guid userId, CancellationToken ct = default);

  /// <summary>Вход по логину, email или телефону.</summary>
  Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default);

  /// <summary>Вход через Google / Apple / Яндекс. В прототипе — локальная заглушка с демо-аккаунтом провайдера.</summary>
  Task<Result<AuthResult>> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default);

  /// <summary>Восстанавливает сессию, сохранённую на устройстве (вызывать при старте приложения).</summary>
  Task<Result<CurrentUserDto?>> RestoreSessionAsync(CancellationToken ct = default);

  Task<Result> LogoutAsync(CancellationToken ct = default);

  /// <summary>Восстановление пароля, шаг 1: отправляет код на email или телефон.</summary>
  Task<Result<PasswordResetPending>> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken ct = default);

  /// <summary>Восстановление пароля, шаг 2: проверяет код и задаёт новый пароль. Все устройства разлогиниваются.</summary>
  Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
