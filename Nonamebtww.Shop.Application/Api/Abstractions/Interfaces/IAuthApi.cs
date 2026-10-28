using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

/// <summary>Контракт будущего серверного API авторизации. В прототипе не используется use case-ами.</summary>
public interface IAuthApi {
  Task<Result<RegistrationPending>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
  Task<Result<AuthResult>> ConfirmRegistrationAsync(ConfirmRegistrationRequest request, CancellationToken ct = default);
  Task<Result<AuthResult>> LoginAsync(LoginRequest request, string deviceName, CancellationToken ct = default);
  Task<Result<AuthResult>> ExternalLoginAsync(ExternalLoginRequest request, string deviceName, CancellationToken ct = default);
  Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default);
  Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
  Task<Result<PasswordResetPending>> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken ct = default);
  Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
