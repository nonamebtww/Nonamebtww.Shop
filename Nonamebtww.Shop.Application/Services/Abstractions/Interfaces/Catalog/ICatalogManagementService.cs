using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;

/// <summary>Процесс DFD «Управление каталогом». Доступно Менеджеру по продажам и Владельцу.</summary>
public interface ICatalogManagementService {
  Task<Result<CategoryDetailed>> SaveCategoryAsync(SaveCategoryRequest request, CancellationToken ct = default);

  /// <summary>Удаляет пустую категорию (без подкатегорий и товаров).</summary>
  Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default);

  Task<Result<ProductDetailed>> SaveProductAsync(SaveProductRequest request, CancellationToken ct = default);
  Task<Result<ProductDetailed>> SetProductActiveAsync(Guid productId, bool isActive, CancellationToken ct = default);
  Task<Result<ProductDetailed>> SetStockAsync(Guid productId, int stock, CancellationToken ct = default);
}
