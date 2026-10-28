using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Favorites;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;

/// <summary>Процесс DFD «Избранное».</summary>
public interface IFavoritesService {
  Task<Result<IReadOnlyList<FavoriteItemDto>>> GetAsync(CancellationToken ct = default);
  Task<Result> AddAsync(Guid productId, CancellationToken ct = default);
  Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default);

  /// <summary>Возвращает новое состояние: true — товар в избранном.</summary>
  Task<Result<bool>> ToggleAsync(Guid productId, CancellationToken ct = default);

  Task<Result<bool>> IsFavoriteAsync(Guid productId, CancellationToken ct = default);
}
