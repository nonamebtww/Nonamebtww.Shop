using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;

namespace Nonamebtww.Shop.Infrastructure.Common;

public sealed class SystemClock : IClock {
  public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
