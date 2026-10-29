using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Services.Implementations.Catalog;

/// <summary>Отзывы о товаре (хранилище DFD «Отзывы», используется в «Просмотре товара»).</summary>
public sealed class ReviewService(
  ICurrentUserService currentUser,
  IReviewsRepository reviews,
  IProductsRepository products,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  IClock clock) : ApplicationService(currentUser), IReviewService {

  public Task<Result<IReadOnlyList<ReviewDto>>> GetForProductAsync(Guid productId, CancellationToken ct = default) =>
    Run<IReadOnlyList<ReviewDto>>(async () => {
      var list = await reviews.GetByProductAsync(productId, ct);
      var result = new List<ReviewDto>(list.Count);

      foreach (var review in list.OrderByDescending(r => r.EditedAt ?? r.CreatedAt)) {
        var author = await users.GetByIdAsync(review.UserId, ct);
        result.Add(review.ToDto(author?.FullName ?? "Покупатель"));
      }

      return result;
    });

  public Task<Result<ReviewDto>> AddAsync(AddReviewRequest request, CancellationToken ct = default) =>
    Run<ReviewDto>(async () => {
      var userId = RequireUser(r => r.CanShop());
      if (userId.IsFailure) {
        return Result.Failure<ReviewDto>(userId.Error!);
      }

      if (AddReviewRequestValidator.Instance.Validate(request) is { } invalid) {
        return Result.Failure<ReviewDto>(invalid);
      }

      if (await products.GetByIdAsync(request.ProductId, ct) is null) {
        return Errors.NotFound("product");
      }

      var rating = new Rating(request.Rating);
      var review = await reviews.GetByUserAndProductAsync(userId.Value, request.ProductId, ct);

      if (review is null) {
        review = Review.Create(userId.Value, request.ProductId, rating, request.Text, clock.UtcNow);
        await reviews.AddAsync(review, ct);
      } else {
        review.Edit(rating, request.Text, clock.UtcNow);
        await reviews.UpdateAsync(review, ct);
      }

      await unitOfWork.SaveChangesAsync(ct);
      return review.ToDto(CurrentUser.User?.FullName ?? "Покупатель");
    });
}
