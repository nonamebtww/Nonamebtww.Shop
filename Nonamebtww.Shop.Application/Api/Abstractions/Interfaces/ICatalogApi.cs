using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface ICatalogApi {
  Task<Result<IReadOnlyList<CategoryNode>>> GetCategoryTreeAsync(CancellationToken ct = default);
  Task<Result<PagedList<CategorySimple>>> SearchCategoriesAsync(CategorySearch search, CancellationToken ct = default);
  Task<Result<CategoryDetailed>> GetCategoryAsync(Guid id, CancellationToken ct = default);
  Task<Result<PagedList<ProductSimple>>> SearchProductsAsync(ProductSearch search, CancellationToken ct = default);
  Task<Result<ProductDetailed>> GetProductAsync(Guid id, CancellationToken ct = default);
  Task<Result<IReadOnlyList<ReviewDto>>> GetReviewsAsync(Guid productId, CancellationToken ct = default);
  Task<Result<ReviewDto>> AddReviewAsync(AddReviewRequest request, CancellationToken ct = default);
}

/// <summary>Управление каталогом (Менеджер по продажам).</summary>
public interface ICatalogManagementApi {
  Task<Result<CategoryDetailed>> SaveCategoryAsync(SaveCategoryRequest request, CancellationToken ct = default);
  Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default);
  Task<Result<ProductDetailed>> SaveProductAsync(SaveProductRequest request, CancellationToken ct = default);
  Task<Result<ProductDetailed>> SetProductActiveAsync(Guid productId, bool isActive, CancellationToken ct = default);
  Task<Result<ProductDetailed>> SetStockAsync(Guid productId, int stock, CancellationToken ct = default);
}
