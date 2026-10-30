using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Common.Validation;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;

namespace Nonamebtww.Shop.Application.Validation;

public sealed class RegisterRequestValidator : IValidator<RegisterRequest> {
  public static readonly RegisterRequestValidator Instance = new();

  public Error? Validate(RegisterRequest model) => new ValidationBuilder()
    .Rule(Rules.IsLogin(model.Login), nameof(model.Login), "Логин: 3–32 символа, латиница, цифры, «_», «.», «-».")
    .Rule(!string.IsNullOrWhiteSpace(model.Email) || !string.IsNullOrWhiteSpace(model.Phone), nameof(model.Email), "Укажите email или телефон.")
    .Rule(string.IsNullOrWhiteSpace(model.Email) || Rules.IsEmail(model.Email), nameof(model.Email), "Введите корректный email.")
    .Rule(string.IsNullOrWhiteSpace(model.Phone) || Rules.IsPhone(model.Phone), nameof(model.Phone), "Введите телефон в формате +7XXXXXXXXXX.")
    .Rule(Rules.IsStrongPassword(model.Password), nameof(model.Password), $"Пароль: не короче {Rules.MinPasswordLength} символов, буквы и цифры.")
    .Rule(!string.IsNullOrWhiteSpace(model.FirstName), nameof(model.FirstName), "Введите имя.")
    .Rule(model.FirstName.Length <= 64, nameof(model.FirstName), "Имя не длиннее 64 символов.")
    .Rule(model.LastName is null || model.LastName.Length <= 64, nameof(model.LastName), "Фамилия не длиннее 64 символов.")
    .Build();
}

public sealed class ConfirmRegistrationRequestValidator : IValidator<ConfirmRegistrationRequest> {
  public static readonly ConfirmRegistrationRequestValidator Instance = new();

  public Error? Validate(ConfirmRegistrationRequest model) => new ValidationBuilder()
    .Rule(model.UserId != Guid.Empty, nameof(model.UserId), "Не указан пользователь.")
    .Rule(Rules.IsConfirmationCode(model.Code), nameof(model.Code), "Код состоит из 6 цифр.")
    .Build();
}

public sealed class LoginRequestValidator : IValidator<LoginRequest> {
  public static readonly LoginRequestValidator Instance = new();

  public Error? Validate(LoginRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrWhiteSpace(model.Identifier), nameof(model.Identifier), "Введите логин, email или телефон.")
    .Rule(!string.IsNullOrEmpty(model.Password), nameof(model.Password), "Введите пароль.")
    .Build();
}

public sealed class PasswordResetRequestValidator : IValidator<PasswordResetRequest> {
  public static readonly PasswordResetRequestValidator Instance = new();

  public Error? Validate(PasswordResetRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrWhiteSpace(model.Identifier), nameof(model.Identifier), "Введите логин, email или телефон.")
    .Build();
}

public sealed class ResetPasswordRequestValidator : IValidator<ResetPasswordRequest> {
  public static readonly ResetPasswordRequestValidator Instance = new();

  public Error? Validate(ResetPasswordRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrWhiteSpace(model.Identifier), nameof(model.Identifier), "Введите логин, email или телефон.")
    .Rule(Rules.IsConfirmationCode(model.Code), nameof(model.Code), "Код состоит из 6 цифр.")
    .Rule(Rules.IsStrongPassword(model.NewPassword), nameof(model.NewPassword), $"Пароль: не короче {Rules.MinPasswordLength} символов, буквы и цифры.")
    .Build();
}
