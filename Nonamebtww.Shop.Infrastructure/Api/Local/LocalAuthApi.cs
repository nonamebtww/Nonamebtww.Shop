using Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Infrastructure.Api.Local;

/// <summary>
/// Локальная fake-реализация API авторизации. Название устройства берётся из IDeviceInfoProvider
/// (параметр deviceName нужен будущему серверу). Обновление токена выполняется по устройствам пользователя в SQLite.
/// </summary>
internal sealed class LocalAuthApi(
  IAuthService auth,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  ITokenService tokens,
  IClock clock) : IAuthApi {
  public Task<Result<RegistrationPending>> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
    auth.RegisterAsync(request, ct);

  public Task<Result<AuthResult>> ConfirmRegistrationAsync(ConfirmRegistrationRequest request, CancellationToken ct = default) =>
    auth.ConfirmRegistrationAsync(request, ct);

  public Task<Result<AuthResult>> LoginAsync(LoginRequest request, string deviceName, CancellationToken ct = default) =>
    auth.LoginAsync(request, ct);

  public Task<Result<AuthResult>> ExternalLoginAsync(ExternalLoginRequest request, string deviceName, CancellationToken ct = default) =>
    auth.ExternalLoginAsync(request, ct);

  public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default) {
    if (string.IsNullOrWhiteSpace(refreshToken)) {
      return Unauthorized();
    }

    // Пользователей в прототипе единицы, поэтому ищем устройство перебором.
    var all = await users.GetAllAsync(ct);
    var match = all
      .SelectMany(u => u.Devices.Select(d => (User: u, Device: d)))
      .FirstOrDefault(x => x.Device.IsActive && x.Device.RefreshToken == refreshToken);

    if (match.User is null) {
      return Unauthorized();
    }

    try {
      var issued = tokens.Issue(match.User.Id, match.User.Role);
      match.User.RecordSignIn(match.Device.Name, match.Device.IpAddress, issued.RefreshToken, clock.UtcNow);
      await users.UpdateAsync(match.User, ct);
      await unitOfWork.SaveChangesAsync(ct);
      return issued;
    } catch (DomainException ex) {
      // Аккаунт заблокирован или не подтверждён.
      return Error.Unauthorized("auth.refresh_denied", ex.Message);
    }
  }

  public Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default) =>
    auth.LogoutAsync(ct);

  public Task<Result<PasswordResetPending>> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken ct = default) =>
    auth.RequestPasswordResetAsync(request, ct);

  public Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default) =>
    auth.ResetPasswordAsync(request, ct);

  private static Error Unauthorized() =>
    Error.Unauthorized("auth.invalid_refresh_token", "Сессия истекла. Войдите снова.");
}
