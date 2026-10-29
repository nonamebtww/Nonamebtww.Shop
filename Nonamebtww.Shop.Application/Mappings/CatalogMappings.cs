using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;

namespace Nonamebtww.Shop.Application.Mappings;

internal static class CatalogMappings {
  public static CategorySimple ToSimple(this Category category, int productCount) =>
    new(category.Id, category.ParentId, category.Name, category.Photo, productCount);

  public static ProductSimple ToSimple(this Product product, RatingInfo rating, bool isFavorite) =>
    new(product.Id, product.CategoryId, product.Sku.Value, product.Name, product.Price.Amount, product.Photo,
      product.Stock, product.IsActive, rating.Average, rating.Count, isFavorite);

  public static ReviewDto ToDto(this Review review, string authorName) =>
    new(review.UserId, review.ProductId, authorName, review.Rating.Value, review.Text, review.EditedAt ?? review.CreatedAt);
}

internal readonly record struct RatingInfo(double? Average, int Count) {
  public static RatingInfo From(IEnumerable<Review> reviews) {
    var values = reviews.Select(r => r.Rating.Value).ToList();
    return values.Count == 0 ? default : new RatingInfo(Math.Round(values.Average(), 1), values.Count);
  }
}
