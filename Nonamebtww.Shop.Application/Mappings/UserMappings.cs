using Nonamebtww.Shop.Application.Models.DTOs.Administration;
using Nonamebtww.Shop.Application.Models.DTOs.Auth;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Application.Mappings;

internal static class UserMappings {
  public static CurrentUserDto ToCurrentUser(this User user) =>
    new(user.Id, user.Login.Value, user.Email?.Value, user.Phone?.Value, user.FirstName, user.LastName, user.Role);

  public static ProfileDto ToProfile(this User user) =>
    new(user.Id, user.Login.Value, user.Email?.Value, user.Phone?.Value, user.FirstName, user.LastName, user.Role,
      user.RegisteredAt, user.IsConfirmed);

  public static UserSummaryDto ToSummary(this User user) =>
    new(user.Id, user.Login.Value, user.Email?.Value, user.Phone?.Value, user.FullName, user.Role, user.IsBlocked,
      user.BlockReason, user.BlockedAt, user.RegisteredAt);

  public static DeviceDto ToDto(this Device device, Guid? currentDeviceId) =>
    new(device.Id, device.Name, device.IpAddress, device.LastSignInAt, device.IsActive, device.Id == currentDeviceId);
}
