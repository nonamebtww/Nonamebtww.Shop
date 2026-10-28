using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;
using Nonamebtww.Shop.Application.Services.Implementations.Auth;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Services.Implementations.Profile;

/// <summary>Управление профилем (DFD 4).</summary>
public sealed class ProfileService(
  CurrentUserService currentUser,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  IPasswordHasher passwordHasher,
  ISessionStore sessions,
  INotificationService notifications) : ApplicationService(currentUser), IProfileService {

  public Task<Result<ProfileDto>> GetAsync(CancellationToken ct = default) =>
    Run<ProfileDto>(async () => {
      var user = await LoadCurrentAsync(ct);
      return user.IsSuccess ? user.Value.ToProfile() : Result.Failure<ProfileDto>(user.Error!);
    });

  public Task<Result<ProfileDto>> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
    Run<ProfileDto>(async () => {
      if (UpdateProfileRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<ProfileDto>(invalid);
      }

      var loaded = await LoadCurrentAsync(ct);
      if (loaded.IsFailure) {
        return Result.Failure<ProfileDto>(loaded.Error!);
      }

      var user = loaded.Value;
      var email = new Email(request.Email);
      var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : new PhoneNumber(request.Phone);

      if (email != user.Email && await users.GetByEmailAsync(email, ct) is not null) {
        return Error.Validation(nameof(request.Email), "Этот email уже зарегистрирован.");
      }

      if (phone is not null && phone != user.Phone && await users.GetByPhoneAsync(phone, ct) is not null) {
        return Error.Validation(nameof(request.Phone), "Этот телефон уже зарегистрирован.");
      }

      user.UpdateProfile(request.FirstName, request.LastName, phone);
      user.ChangeEmail(email);

      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      currentUser.Update(user.ToCurrentUser());
      await notifications.SendAsync(new NotificationMessage(NotificationChannel.Email, user.Email.Value,
        "Профиль изменён", "Данные вашего профиля были обновлены.", user.Id), ct);

      return user.ToProfile();
    });

  public Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
    Execute(async () => {
      if (ChangePasswordRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure(invalid);
      }

      var loaded = await LoadCurrentAsync(ct);
      if (loaded.IsFailure) {
        return Result.Failure(loaded.Error!);
      }

      var user = loaded.Value;
      if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash)) {
        return Error.Validation(nameof(request.CurrentPassword), "Текущий пароль указан неверно.");
      }

      // Domain разлогинивает все устройства; текущее устройство выходит тоже — пользователь входит заново.
      user.ChangePassword(passwordHasher.Hash(request.NewPassword));
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await notifications.SendAsync(new NotificationMessage(NotificationChannel.Email, user.Email.Value,
        "Пароль изменён", "Пароль вашего аккаунта был изменён.", user.Id), ct);

      await sessions.ClearAsync(ct);
      currentUser.SignOut();
      return Result.Success();
    });

  public Task<Result<IReadOnlyList<DeviceDto>>> GetDevicesAsync(CancellationToken ct = default) =>
    Run<IReadOnlyList<DeviceDto>>(async () => {
      var loaded = await LoadCurrentAsync(ct);
      if (loaded.IsFailure) {
        return Result.Failure<IReadOnlyList<DeviceDto>>(loaded.Error!);
      }

      IReadOnlyList<DeviceDto> devices = loaded.Value.Devices
        .OrderByDescending(d => d.LastSignInAt)
        .Select(d => d.ToDto(CurrentUser.DeviceId))
        .ToList();

      return Result.Success(devices);
    });

  public Task<Result> SignOutDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
    Execute(async () => {
      var loaded = await LoadCurrentAsync(ct);
      if (loaded.IsFailure) {
        return Result.Failure(loaded.Error!);
      }

      var user = loaded.Value;
      if (user.Devices.All(d => d.Id != deviceId)) {
        return Errors.NotFound("device");
      }

      user.SignOut(deviceId);
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      if (deviceId == CurrentUser.DeviceId) {
        await sessions.ClearAsync(ct);
        currentUser.SignOut();
      }

      return Result.Success();
    });

  private async Task<Result<User>> LoadCurrentAsync(CancellationToken ct) {
    var id = RequireUser();
    if (id.IsFailure) {
      return Result.Failure<User>(id.Error!);
    }

    var user = await users.GetByIdAsync(id.Value, ct);
    return user is null ? Errors.NotFound("user") : user;
  }
}
