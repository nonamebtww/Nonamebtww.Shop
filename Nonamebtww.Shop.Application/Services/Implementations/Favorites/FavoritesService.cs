using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Favorites;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using FavoritesList = Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate.Favorites;

namespace Nonamebtww.Shop.Application.Services.Implementations.Favorites;

/// <summary>Избранное (DFD 9). Только покупатель.</summary>
public sealed class FavoritesService(
  ICurrentUserService currentUser,
  IFavoritesRepository favorites,
  IProductsRepository products,
  IReviewsRepository reviews,
  IUnitOfWork unitOfWork,
  IClock clock) : ApplicationService(currentUser), IFavoritesService {

  public Task<Result<IReadOnlyList<FavoriteItemDto>>> GetAsync(CancellationToken ct = default) =>
    Run<IReadOnlyList<FavoriteItemDto>>(async () => {
      var userId = RequireUser(r => r.CanShop());
      if (userId.IsFailure) {
        return Result.Failure<IReadOnlyList<FavoriteItemDto>>(userId.Error!);
      }

      var list = await favorites.GetByUserIdAsync(userId.Value, ct);
      if (list is null || list.Items.Count == 0) {
        return Result.Success<IReadOnlyList<FavoriteItemDto>>([]);
      }

      var byId = (await products.GetByIdsAsync(list.Items.Select(i => i.ProductId), ct)).ToDictionary(p => p.Id);
      var result = new List<FavoriteItemDto>();

      foreach (var item in list.Items.OrderByDescending(i => i.AddedAt)) {
        if (!byId.TryGetValue(item.ProductId, out var product)) {
          continue;
        }

        var rating = RatingInfo.From(await reviews.GetByProductAsync(product.Id, ct));
        result.Add(new FavoriteItemDto(product.ToSimple(rating, isFavorite: true), item.AddedAt));
      }

      return result;
    });

  public Task<Result> AddAsync(Guid productId, CancellationToken ct = default) =>
    Execute(async () => {
      var change = await ChangeAsync(productId, (f, now) => f.Add(productId, now), ct);
      return change.IsSuccess ? Result.Success() : Result.Failure(change.Error!);
    });

  public Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default) =>
    Execute(async () => {
      var change = await ChangeAsync(productId, (f, _) => f.Remove(productId), ct, requireProduct: false);
      return change.IsSuccess ? Result.Success() : Result.Failure(change.Error!);
    });

  public Task<Result<bool>> ToggleAsync(Guid productId, CancellationToken ct = default) =>
    Run<bool>(async () => {
      var change = await ChangeAsync(productId, (f, now) => f.Toggle(productId, now), ct);
      return change.IsSuccess ? change.Value.Contains(productId) : Result.Failure<bool>(change.Error!);
    });

  public Task<Result<bool>> IsFavoriteAsync(Guid productId, CancellationToken ct = default) =>
    Run<bool>(async () => {
      if (CurrentUser.UserId is not { } userId || CurrentUser.Role is not { } role || !role.CanShop()) {
        return false;
      }

      return (await favorites.GetByUserIdAsync(userId, ct))?.Contains(productId) ?? false;
    });

  private async Task<Result<FavoritesList>> ChangeAsync(
    Guid productId,
    Action<FavoritesList, DateTimeOffset> change,
    CancellationToken ct,
    bool requireProduct = true) {
    var userId = RequireUser(r => r.CanShop());
    if (userId.IsFailure) {
      return Result.Failure<FavoritesList>(userId.Error!);
    }

    if (requireProduct && await products.GetByIdAsync(productId, ct) is null) {
      return Errors.NotFound("product");
    }

    var list = await favorites.GetByUserIdAsync(userId.Value, ct);
    var isNew = list is null;
    list ??= FavoritesList.CreateFor(userId.Value);

    change(list, clock.UtcNow);

    if (isNew) {
      await favorites.AddAsync(list, ct);
    } else {
      await favorites.UpdateAsync(list, ct);
    }

    await unitOfWork.SaveChangesAsync(ct);
    return list;
  }
}
