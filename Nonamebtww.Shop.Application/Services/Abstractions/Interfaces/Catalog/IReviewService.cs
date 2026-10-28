using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;

/// <summary>Отзывы (хранилище DFD «Отзывы»). Отзыв оставляет покупатель; повторный отзыв на тот же товар редактирует прежний.</summary>
public interface IReviewService {
  Task<Result<IReadOnlyList<ReviewDto>>> GetForProductAsync(Guid productId, CancellationToken ct = default);
  Task<Result<ReviewDto>> AddAsync(AddReviewRequest request, CancellationToken ct = default);
}
