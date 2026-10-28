namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

/// <summary>Назначение кода подтверждения.</summary>
public enum ConfirmationCodePurpose {
  /// <summary>Подтверждение регистрации.</summary>
  Registration = 0,

  /// <summary>Восстановление пароля.</summary>
  PasswordReset = 1,
}
