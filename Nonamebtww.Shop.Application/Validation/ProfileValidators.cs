using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Common.Validation;
using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;

namespace Nonamebtww.Shop.Application.Validation;

public sealed class UpdateProfileRequestValidator : IValidator<UpdateProfileRequest> {
  public static readonly UpdateProfileRequestValidator Instance = new();

  public Error? Validate(UpdateProfileRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrWhiteSpace(model.FirstName), nameof(model.FirstName), "Введите имя.")
    .Rule(model.FirstName.Length <= 64, nameof(model.FirstName), "Имя не длиннее 64 символов.")
    .Rule(model.LastName is null || model.LastName.Length <= 64, nameof(model.LastName), "Фамилия не длиннее 64 символов.")
    .Rule(!string.IsNullOrWhiteSpace(model.Email) || !string.IsNullOrWhiteSpace(model.Phone), nameof(model.Email), "Укажите email или телефон.")
    .Rule(string.IsNullOrWhiteSpace(model.Email) || Rules.IsEmail(model.Email), nameof(model.Email), "Введите корректный email.")
    .Rule(string.IsNullOrWhiteSpace(model.Phone) || Rules.IsPhone(model.Phone), nameof(model.Phone), "Введите телефон в формате +7XXXXXXXXXX.")
    .Build();
}

public sealed class ChangePasswordRequestValidator : IValidator<ChangePasswordRequest> {
  public static readonly ChangePasswordRequestValidator Instance = new();

  public Error? Validate(ChangePasswordRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrEmpty(model.CurrentPassword), nameof(model.CurrentPassword), "Введите текущий пароль.")
    .Rule(Rules.IsStrongPassword(model.NewPassword), nameof(model.NewPassword), $"Пароль: не короче {Rules.MinPasswordLength} символов, буквы и цифры.")
    .Rule(model.NewPassword != model.CurrentPassword, nameof(model.NewPassword), "Новый пароль совпадает с текущим.")
    .Build();
}

public sealed class DeleteAccountRequestValidator : IValidator<DeleteAccountRequest> {
  public static readonly DeleteAccountRequestValidator Instance = new();

  public Error? Validate(DeleteAccountRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrEmpty(model.Password), nameof(model.Password), "Введите пароль.")
    .Build();
}

public sealed class BlockUserRequestValidator : IValidator<BlockUserRequest> {
  public static readonly BlockUserRequestValidator Instance = new();

  public Error? Validate(BlockUserRequest model) => new ValidationBuilder()
    .Rule(model.UserId != Guid.Empty, nameof(model.UserId), "Не указан пользователь.")
    .Rule(!string.IsNullOrWhiteSpace(model.Reason), nameof(model.Reason), "Укажите причину блокировки.")
    .Rule(model.Reason.Length <= 512, nameof(model.Reason), "Причина не длиннее 512 символов.")
    .Build();
}
