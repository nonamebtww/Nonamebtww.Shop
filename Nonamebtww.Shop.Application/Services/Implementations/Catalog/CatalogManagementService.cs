using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Application.Services.Implementations.Catalog;

/// <summary>Управление каталогом (DFD 11). Менеджер по продажам и Владелец.</summary>
public sealed class CatalogManagementService(
  ICurrentUserService currentUser,
  ICategoriesRepository categories,
  IProductsRepository products,
  IReviewsRepository reviews,
  IFavoritesRepository favorites,
  IUnitOfWork unitOfWork,
  ProductQueries productQueries) : ApplicationService(currentUser), ICatalogManagementService {

  public Task<Result<CategoryDetailed>> SaveCategoryAsync(SaveCategoryRequest request, CancellationToken ct = default) =>
    Run<CategoryDetailed>(async () => {
      if (RequireManager() is { } denied) {
        return denied;
      }

      if (SaveCategoryRequestValidator.Instance.Validate(request) is { } invalid) {
        return invalid;
      }

      var all = await categories.GetAllAsync(ct);
      var parentId = request.ParentId == Guid.Empty ? null : request.ParentId;

      if (parentId is { } pid && all.All(c => c.Id != pid)) {
        return Error.Validation(nameof(request.ParentId), "Родительская категория не найдена.");
      }

      Category category;

      if (request.Id is { } id) {
        category = all.FirstOrDefault(c => c.Id == id)!;
        if (category is null) {
          return Errors.NotFound("category");
        }

        if (parentId is not null && CreatesCycle(all, id, parentId.Value)) {
          return Error.Validation(nameof(request.ParentId), "Нельзя вложить категорию в её же подкатегорию.");
        }

        category.Update(request.Name, request.Description, request.Photo);
        category.MoveTo(parentId);
        await categories.UpdateAsync(category, ct);
      } else {
        category = Category.Create(request.Name, parentId, request.Description, request.Photo);
        await categories.AddAsync(category, ct);
      }

      await unitOfWork.SaveChangesAsync(ct);

      var snapshot = await CatalogSnapshot.LoadAsync(categories, products, reviews, favorites, null, true, ct);
      return CategoryQueries.ToDetailed(snapshot, snapshot.FindCategory(category.Id) ?? category);
    });

  public Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
    Execute(async () => {
      if (RequireManager() is { } denied) {
        return denied;
      }

      var category = await categories.GetByIdAsync(categoryId, ct);
      if (category is null) {
        return Errors.NotFound("category");
      }

      if ((await categories.GetChildrenAsync(categoryId, ct)).Count > 0) {
        return Errors.Conflict("category.has_children", "Сначала удалите или перенесите подкатегории.");
      }

      if ((await products.GetByCategoryAsync(categoryId, onlyActive: false, ct)).Count > 0) {
        return Errors.Conflict("category.has_products", "В категории есть товары. Перенесите их в другую категорию.");
      }

      await categories.RemoveAsync(category, ct);
      await unitOfWork.SaveChangesAsync(ct);
      return Result.Success();
    });

  public Task<Result<ProductDetailed>> SaveProductAsync(SaveProductRequest request, CancellationToken ct = default) =>
    Run<ProductDetailed>(async () => {
      if (RequireManager() is { } denied) {
        return denied;
      }

      if (SaveProductRequestValidator.Instance.Validate(request) is { } invalid) {
        return invalid;
      }

      if (await categories.GetByIdAsync(request.CategoryId, ct) is null) {
        return Error.Validation(nameof(request.CategoryId), "Категория не найдена.");
      }

      var sku = Parse(nameof(request.Sku), () => new Sku(request.Sku));
      if (sku.IsFailure) {
        return sku.Error!;
      }

      var sameSku = await products.GetBySkuAsync(sku.Value, ct);
      if (sameSku is not null && sameSku.Id != request.Id) {
        return Error.Validation(nameof(request.Sku), "Товар с таким артикулом уже есть.");
      }

      var price = new Money(request.Price);
      var characteristics = Characteristics.Format(request.Characteristics);
      Product product;

      if (request.Id is { } id) {
        product = (await products.GetByIdAsync(id, ct))!;
        if (product is null) {
          return Errors.NotFound("product");
        }

        product.UpdateDetails(request.Name, request.Description, characteristics, request.Photo);
        product.ChangeSku(sku.Value);
        product.ChangePrice(price);
        product.MoveToCategory(request.CategoryId);
        product.SetStock(request.Stock);
        SetActive(product, request.IsActive);
        await products.UpdateAsync(product, ct);
      } else {
        product = Product.Create(request.CategoryId, sku.Value, request.Name, price, request.Stock,
          request.Description, characteristics, request.Photo);
        SetActive(product, request.IsActive);
        await products.AddAsync(product, ct);
      }

      await unitOfWork.SaveChangesAsync(ct);
      return await productQueries.BuildDetailedAsync(product, ct);
    });

  public Task<Result<ProductDetailed>> SetProductActiveAsync(Guid productId, bool isActive, CancellationToken ct = default) =>
    ChangeProductAsync(productId, p => SetActive(p, isActive), ct);

  public Task<Result<ProductDetailed>> SetStockAsync(Guid productId, int stock, CancellationToken ct = default) =>
    stock < 0
      ? Task.FromResult(Result.Failure<ProductDetailed>(Error.Validation("Stock", "Остаток не может быть отрицательным.")))
      : ChangeProductAsync(productId, p => p.SetStock(stock), ct);

  private Task<Result<ProductDetailed>> ChangeProductAsync(Guid productId, Action<Product> change, CancellationToken ct) =>
    Run<ProductDetailed>(async () => {
      if (RequireManager() is { } denied) {
        return denied;
      }

      var product = await products.GetByIdAsync(productId, ct);
      if (product is null) {
        return Errors.NotFound("product");
      }

      change(product);
      await products.UpdateAsync(product, ct);
      await unitOfWork.SaveChangesAsync(ct);
      return await productQueries.BuildDetailedAsync(product, ct);
    });

  private Error? RequireManager() {
    var user = RequireUser(r => r.CanManageCatalog());
    return user.IsFailure ? user.Error : null;
  }

  private static void SetActive(Product product, bool isActive) {
    if (isActive) {
      product.Activate();
    } else {
      product.Deactivate();
    }
  }

  private static bool CreatesCycle(IReadOnlyList<Category> all, Guid categoryId, Guid newParentId) {
    var byId = all.ToDictionary(c => c.Id);
    Guid? current = newParentId;
    var guard = 0;

    while (current is { } id && guard++ < all.Count + 1) {
      if (id == categoryId) {
        return true;
      }

      current = byId.TryGetValue(id, out var parent) ? parent.ParentId : null;
    }

    return false;
  }
}
