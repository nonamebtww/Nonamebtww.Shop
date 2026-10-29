using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate.Events;
using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;

/// <summary>
/// Отзыв о товаре (IDEF1X «Отзыв»). Пользователь может оставить на товар несколько отзывов.
/// </summary>
public class Review : AggregateRoot<Guid> {
  public const int MaxTextLength = 4000;

  private Review() { }

  private Review(Guid id, Guid userId, Guid productId, Rating rating, string? text, DateTimeOffset createdAt) : base(id) {
    UserId = Guard.NotEmpty(userId, "id_пользователя");
    ProductId = Guard.NotEmpty(productId, "id_товара");
    Rating = rating;
    Text = Guard.Optional(text, "Текст отзыва", MaxTextLength);
    CreatedAt = createdAt;
  }

  public Guid UserId { get; private set; }
  public Guid ProductId { get; private set; }
  public Rating Rating { get; private set; } = null!;
  public string? Text { get; private set; }

  /// <summary>Дата отзыва.</summary>
  public DateTimeOffset CreatedAt { get; private set; }

  public DateTimeOffset? EditedAt { get; private set; }

  public static Review Create(Guid userId, Guid productId, Rating rating, string? text, DateTimeOffset now) {
    var review = new Review(Guid.NewGuid(), userId, productId, rating, text, now);
    review.Raise(new ReviewCreated(review.Id, userId, productId, rating.Value));
    return review;
  }

  public void Edit(Rating rating, string? text, DateTimeOffset now) {
    var old = Rating;
    Rating = rating;
    Text = Guard.Optional(text, "Текст отзыва", MaxTextLength);
    EditedAt = now;
    Raise(new ReviewEdited(Id, ProductId, old.Value, rating.Value));
  }
}
