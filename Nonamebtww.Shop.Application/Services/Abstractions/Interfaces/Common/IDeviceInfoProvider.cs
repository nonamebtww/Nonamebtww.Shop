namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;

/// <summary>Сведения о текущем устройстве (IDEF1X «Устройство»).</summary>
public interface IDeviceInfoProvider {
  string DeviceName { get; }
  string? IpAddress { get; }
}
