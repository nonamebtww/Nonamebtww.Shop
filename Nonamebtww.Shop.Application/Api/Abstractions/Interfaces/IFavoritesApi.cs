using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Favorites;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface IFavoritesApi {
  Task<Result<IReadOnlyList<FavoriteItemDto>>> GetAsync(CancellationToken ct = default);
  Task<Result> AddAsync(Guid productId, CancellationToken ct = default);
  Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default);
}
