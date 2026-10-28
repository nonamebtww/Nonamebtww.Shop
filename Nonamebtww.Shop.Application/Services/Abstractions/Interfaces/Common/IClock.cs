namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;

public interface IClock {
  DateTimeOffset UtcNow { get; }
}
