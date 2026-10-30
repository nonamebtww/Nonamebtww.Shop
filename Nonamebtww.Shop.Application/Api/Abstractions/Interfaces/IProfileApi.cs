using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface IProfileApi {
  Task<Result<ProfileDto>> GetAsync(CancellationToken ct = default);
  Task<Result<ProfileDto>> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default);
  Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
  Task<Result<IReadOnlyList<DeviceDto>>> GetDevicesAsync(CancellationToken ct = default);
  Task<Result> SignOutDeviceAsync(Guid deviceId, CancellationToken ct = default);
  Task<Result> DeleteAccountAsync(DeleteAccountRequest request, CancellationToken ct = default);
}
