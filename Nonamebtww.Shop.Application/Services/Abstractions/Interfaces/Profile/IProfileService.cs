using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;

/// <summary>Процесс DFD «Управление профилем».</summary>
public interface IProfileService {
  Task<Result<ProfileDto>> GetAsync(CancellationToken ct = default);
  Task<Result<ProfileDto>> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default);
  Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
  Task<Result<IReadOnlyList<DeviceDto>>> GetDevicesAsync(CancellationToken ct = default);
  Task<Result> SignOutDeviceAsync(Guid deviceId, CancellationToken ct = default);

  /// <summary>Удаление аккаунта (3.5): после успеха пользователь выходит из аккаунта.</summary>
  Task<Result> DeleteAccountAsync(DeleteAccountRequest request, CancellationToken ct = default);
}
