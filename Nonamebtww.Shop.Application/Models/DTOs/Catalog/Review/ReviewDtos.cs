namespace Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;

public sealed record ReviewDto(Guid UserId, Guid ProductId, string AuthorName, int Rating, string? Text, DateTimeOffset CreatedAt);

/// <summary>Один отзыв на товар от пользователя (ключ IDEF1X: пользователь + товар); повторный — редактирует прежний.</summary>
public sealed record AddReviewRequest(Guid ProductId, int Rating, string? Text);
