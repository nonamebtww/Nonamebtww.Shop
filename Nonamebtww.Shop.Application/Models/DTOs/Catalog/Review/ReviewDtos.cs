namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;

public sealed record ReviewDto(Guid UserId, Guid ProductId, string AuthorName, int Rating, string? Text, DateTimeOffset CreatedAt);

/// <summary>Новый отзыв на товар. Каждый отзыв сохраняется отдельной записью (IDEF1X «Отзыв», ключ id_отзыва).</summary>
public sealed record AddReviewRequest(Guid ProductId, int Rating, string? Text);
