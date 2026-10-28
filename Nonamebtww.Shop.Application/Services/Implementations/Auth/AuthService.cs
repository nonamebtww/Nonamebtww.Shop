using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Services.Implementations.Auth;

/// <summary>Регистрация (DFD 1), Вход (DFD 2), Восстановление пароля (DFD 3).</summary>
public sealed class AuthService(
  CurrentUserService currentUser,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  IPasswordHasher passwordHasher,
  ITokenService tokens,
  ISessionStore sessions,
  INotificationService notifications,
  IDeviceInfoProvider device,
  IClock clock) : ApplicationService(currentUser), IAuthService {

  public Task<Result<RegistrationPending>> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
    Run<RegistrationPending>(async () => {
      if (RegisterRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<RegistrationPending>(invalid);
      }

      var login = new Login(request.Login);
      var email = new Email(request.Email);
      var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : new PhoneNumber(request.Phone);

      if (await users.GetByLoginAsync(login, ct) is not null) {
        return Error.Validation(nameof(request.Login), "Этот логин уже занят.");
      }

      if (await users.GetByEmailAsync(email, ct) is not null) {
        return Error.Validation(nameof(request.Email), "Этот email уже зарегистрирован.");
      }

      if (phone is not null && await users.GetByPhoneAsync(phone, ct) is not null) {
        return Error.Validation(nameof(request.Phone), "Этот телефон уже зарегистрирован.");
      }

      var now = clock.UtcNow;
      var user = User.Register(login, email, phone, passwordHasher.Hash(request.Password),
        request.FirstName, request.LastName, now);
      var code = user.IssueCode(ConfirmationCodePurpose.Registration, ConfirmationCodes.Generate(), now, ConfirmationCodes.Lifetime);

      await users.AddAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);
      await SendCodeAsync(user, code.Code, "Подтверждение регистрации", ct);

      return new RegistrationPending(user.Id, ConfirmationCodes.MaskEmail(user.Email.Value));
    });

  public Task<Result<AuthResult>> ConfirmRegistrationAsync(ConfirmRegistrationRequest request, CancellationToken ct = default) =>
    Run<AuthResult>(async () => {
      if (ConfirmRegistrationRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<AuthResult>(invalid);
      }

      var user = await users.GetByIdAsync(request.UserId, ct);
      if (user is null) {
        return Errors.NotFound("user");
      }

      user.UseCode(ConfirmationCodePurpose.Registration, request.Code, clock.UtcNow);
      var result = await SignInAsync(user, ct);

      await notifications.SendAsync(new NotificationMessage(NotificationChannel.Email, user.Email.Value,
        "Добро пожаловать!", $"{user.FirstName}, регистрация завершена.", user.Id), ct);

      return result;
    });

  public Task<Result<RegistrationPending>> ResendRegistrationCodeAsync(Guid userId, CancellationToken ct = default) =>
    Run<RegistrationPending>(async () => {
      var user = await users.GetByIdAsync(userId, ct);
      if (user is null) {
        return Errors.NotFound("user");
      }

      var code = user.IssueCode(ConfirmationCodePurpose.Registration, ConfirmationCodes.Generate(), clock.UtcNow, ConfirmationCodes.Lifetime);
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);
      await SendCodeAsync(user, code.Code, "Подтверждение регистрации", ct);

      return new RegistrationPending(user.Id, ConfirmationCodes.MaskEmail(user.Email.Value));
    });

  public Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
    Run<AuthResult>(async () => {
      if (LoginRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<AuthResult>(invalid);
      }

      var user = await users.FindByIdentifierAsync(request.Identifier, ct);
      if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash)) {
        return Errors.InvalidCredentials;
      }

      if (!user.IsConfirmed && !user.IsBlocked) {
        // Аккаунт не подтверждён: отправляем свежий код, Mobile открывает экран ввода кода по Fields["userId"].
        var code = user.IssueCode(ConfirmationCodePurpose.Registration, ConfirmationCodes.Generate(), clock.UtcNow, ConfirmationCodes.Lifetime);
        await users.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await SendCodeAsync(user, code.Code, "Подтверждение регистрации", ct);

        return Error.Unauthorized("auth.not_confirmed", "Аккаунт не подтверждён. Мы отправили новый код.") with {
          Fields = new Dictionary<string, string[]> { ["userId"] = [user.Id.ToString()] }
        };
      }

      return await SignInAsync(user, ct);
    });

  public Task<Result<AuthResult>> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default) =>
    Run<AuthResult>(async () => {
      // Прототип: вместо OAuth провайдера используется локальный демо-аккаунт этого провайдера.
      var name = request.Provider.ToString().ToLowerInvariant();
      var login = new Login($"{name}_user");
      var user = await users.GetByLoginAsync(login, ct);
      var isNew = user is null;

      if (user is null) {
        user = User.Register(login, new Email($"{name}.user@{name}.example"), null,
          passwordHasher.Hash(Guid.NewGuid().ToString("N")), request.Provider.ToString(), "Пользователь",
          clock.UtcNow, confirmed: true);
      }

      return await SignInAsync(user, ct, isNew);
    });

  public Task<Result<CurrentUserDto?>> RestoreSessionAsync(CancellationToken ct = default) =>
    Run<CurrentUserDto?>(async () => {
      var session = await sessions.LoadAsync(ct);
      if (session is null) {
        return Result.Success<CurrentUserDto?>(null);
      }

      var user = await users.GetByIdAsync(session.UserId, ct);
      var deviceRecord = user?.Devices.FirstOrDefault(d => d.Id == session.DeviceId);

      var isValid = user is { IsBlocked: false, IsConfirmed: true }
        && deviceRecord is { IsActive: true }
        && deviceRecord.RefreshToken == session.Tokens.RefreshToken;

      if (!isValid) {
        await sessions.ClearAsync(ct);
        currentUser.SignOut();
        return Result.Success<CurrentUserDto?>(null);
      }

      var dto = user!.ToCurrentUser();

      if (tokens.ValidateAccessToken(session.Tokens.AccessToken) is null) {
        // Access-токен истёк: обновляем пару по refresh-токену устройства.
        var refreshed = tokens.Issue(user.Id, user.Role);
        user.RecordSignIn(deviceRecord!.Name, device.IpAddress, refreshed.RefreshToken, clock.UtcNow);
        await users.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await sessions.SaveAsync(session with { Tokens = refreshed }, ct);
      }

      currentUser.SignIn(dto, session.DeviceId);
      return Result.Success<CurrentUserDto?>(dto);
    });

  public Task<Result> LogoutAsync(CancellationToken ct = default) =>
    Execute(async () => {
      var session = await sessions.LoadAsync(ct);

      if (session is not null && await users.GetByIdAsync(session.UserId, ct) is { } user
          && user.Devices.Any(d => d.Id == session.DeviceId)) {
        user.SignOut(session.DeviceId);
        await users.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
      }

      await sessions.ClearAsync(ct);
      currentUser.SignOut();
      return Result.Success();
    });

  public Task<Result<PasswordResetPending>> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken ct = default) =>
    Run<PasswordResetPending>(async () => {
      if (PasswordResetRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<PasswordResetPending>(invalid);
      }

      var user = await users.FindByIdentifierAsync(request.Identifier, ct);
      if (user is null) {
        return Errors.NotFound("user");
      }

      var code = user.IssueCode(ConfirmationCodePurpose.PasswordReset, ConfirmationCodes.Generate(), clock.UtcNow, ConfirmationCodes.Lifetime);
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);
      await SendCodeAsync(user, code.Code, "Восстановление пароля", ct);

      return new PasswordResetPending(ConfirmationCodes.MaskEmail(user.Email.Value));
    });

  public Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default) =>
    Execute(async () => {
      if (ResetPasswordRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure(invalid);
      }

      var user = await users.FindByIdentifierAsync(request.Identifier, ct);
      if (user is null) {
        return Errors.NotFound("user");
      }

      user.UseCode(ConfirmationCodePurpose.PasswordReset, request.Code, clock.UtcNow);
      user.ChangePassword(passwordHasher.Hash(request.NewPassword));
      await users.UpdateAsync(user, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await notifications.SendAsync(new NotificationMessage(NotificationChannel.Email, user.Email.Value,
        "Пароль изменён", "Пароль вашего аккаунта был изменён. Все устройства вышли из аккаунта.", user.Id), ct);

      if (CurrentUser.UserId == user.Id) {
        await sessions.ClearAsync(ct);
        currentUser.SignOut();
      }

      return Result.Success();
    });

  /// <summary>Общий шаг входа: устройство, токены («JWT»), сохранение сессии, уведомление.</summary>
  private async Task<Result<AuthResult>> SignInAsync(User user, CancellationToken ct, bool isNew = false) {
    var issued = tokens.Issue(user.Id, user.Role);
    var signedInDevice = user.RecordSignIn(device.DeviceName, device.IpAddress, issued.RefreshToken, clock.UtcNow);

    if (isNew) {
      await users.AddAsync(user, ct);
    } else {
      await users.UpdateAsync(user, ct);
    }

    await unitOfWork.SaveChangesAsync(ct);
    await sessions.SaveAsync(new StoredSession(user.Id, signedInDevice.Id, issued), ct);

    var dto = user.ToCurrentUser();
    currentUser.SignIn(dto, signedInDevice.Id);

    await notifications.SendAsync(new NotificationMessage(NotificationChannel.InApp, null,
      "Вход в аккаунт", $"Выполнен вход с устройства «{signedInDevice.Name}».", user.Id), ct);

    return new AuthResult(dto, issued);
  }

  private Task SendCodeAsync(User user, string code, string subject, CancellationToken ct) =>
    notifications.SendAsync(new NotificationMessage(NotificationChannel.Email, user.Email.Value, subject,
      $"Ваш код: {code}. Код действует {ConfirmationCodes.Lifetime.TotalMinutes:0} минут.", user.Id), ct);
}
