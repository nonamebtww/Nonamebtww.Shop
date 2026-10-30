using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;
using Nonamebtww.Shop.Application.Services.Implementations.Auth;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Services.Implementations.Profile;

/// <summary>Управление профилем (DFD 4, декомпозиция «3. Управление профилем»).</summary>
public sealed class ProfileService(
  CurrentUserService currentUser,
  IUsersRepository users,
  IOrdersRepository orders,
  IUnitOfWork unitOfWork,
  IPasswordHasher passwordHasher,
  ISessionStore sessions,
  INotificationService notifications,
  IClock clock) : ApplicationService(currentUser), IProfileService {

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
      var email = string.IsNullOrWhiteSpace(request.Email) ? null : new Email(request.Email);
      var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : new PhoneNumber(request.Phone);

      if (email is not null && email != user.Email && await users.GetByEmailAsync(email, ct) is not null) {
        return Error.Validation(nameof(request.Email), "Этот email уже зарегистрирован.");
      }

      if (phone is not null && phone != user.Phone && await users.GetByPhoneAsync(phone, ct) is not null) {
        return Error.Validation(nameof(request.Phone), "Этот телефон уже зарегистрирован.");
      }

      user.UpdateProfile(request.FirstName, request.LastName);
      user.ChangeContacts(email, phone);

      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      currentUser.Update(user.ToCurrentUser());
      await notifications.SendToContactAsync(user, "Профиль изменён", "Данные вашего профиля были обновлены.", ct);

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

      await notifications.SendToContactAsync(user, "Пароль изменён", "Пароль вашего аккаунта был изменён.", ct);

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

  /// <summary>
  /// Удаление аккаунта (3.5). Нужен пароль; нельзя удалить аккаунт владельца и аккаунт
  /// с незавершёнными заказами или доставками. Запись пользователя остаётся для истории заказов и отзывов,
  /// но логин, почта, телефон и имя стираются, все устройства выходят из аккаунта.
  /// </summary>
  public Task<Result> DeleteAccountAsync(DeleteAccountRequest request, CancellationToken ct = default) =>
    Execute(async () => {
      if (DeleteAccountRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure(invalid);
      }

      var loaded = await LoadCurrentAsync(ct);
      if (loaded.IsFailure) {
        return Result.Failure(loaded.Error!);
      }

      var user = loaded.Value;
      if (!passwordHasher.Verify(request.Password, user.PasswordHash)) {
        return Error.Validation(nameof(request.Password), "Пароль указан неверно.");
      }

      if ((await orders.GetByUserAsync(user.Id, ct)).Any(o => o.Status.IsActive())) {
        return Error.Conflict("profile.active_orders",
          "У вас есть незавершённые заказы. Удалить аккаунт можно, когда они будут получены или отменены.");
      }

      if ((await orders.GetByCourierAsync(user.Id, ct)).Any(o => o.Status.IsActive())) {
        return Error.Conflict("profile.active_deliveries",
          "За вами закреплены незавершённые доставки. Удалить аккаунт можно после их завершения.");
      }

      // Письмо или SMS уходит на прежний контакт, поэтому собираем его до того, как контакты будут стёрты.
      var farewell = user.ToContact("Аккаунт удалён", "Ваш аккаунт удалён, личные данные стёрты.");

      user.Delete(clock.UtcNow);
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      if (farewell is not null) {
        await notifications.SendAsync(farewell, ct);
      }

      await sessions.ClearAsync(ct);
      currentUser.SignOut();
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
