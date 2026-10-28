using Nonamebtww.Shop.Application.Models.DTOs.Auth;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;

public interface ISessionStore {
  Task<StoredSession?> LoadAsync(CancellationToken ct = default);
  Task SaveAsync(StoredSession session, CancellationToken ct = default);
  Task ClearAsync(CancellationToken ct = default);
}
