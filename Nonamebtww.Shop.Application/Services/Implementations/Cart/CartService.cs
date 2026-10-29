using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;
using ShoppingCart = Nonamebtww.Shop.Domain.Aggregates.CartAggregate.Cart;

namespace Nonamebtww.Shop.Application.Services.Implementations.Cart;

/// <summary>Корзина (DFD 10). Только покупатель.</summary>
public sealed class CartService(
  ICurrentUserService currentUser,
  ICartsRepository carts,
  IProductsRepository products,
  IUnitOfWork unitOfWork,
  IClock clock,
  CartChangeNotifier notifier) : ApplicationService(currentUser), ICartService {

  public event EventHandler<CartDto>? Changed {
    add => notifier.Changed += value;
    remove => notifier.Changed -= value;
  }

  public Task<Result<CartDto>> GetAsync(CancellationToken ct = default) =>
    Run<CartDto>(async () => {
      var userId = RequireUser(r => r.CanShop());
      if (userId.IsFailure) {
        return Result.Failure<CartDto>(userId.Error!);
      }

      var cart = await carts.GetByUserIdAsync(userId.Value, ct);
      return cart is null ? CartDto.Empty : await ToDtoAsync(products, cart, ct);
    });

  public Task<Result<CartDto>> AddAsync(Guid productId, int quantity = 1, CancellationToken ct = default) =>
    ChangeAsync(async (cart, ct2) => {
      var product = await products.GetByIdAsync(productId, ct2);
      if (product is null) {
        return Errors.NotFound("product");
      }

      var desired = (cart.Items.FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0) + quantity;
      if (StockError(product, desired) is { } error) {
        return error;
      }

      cart.AddItem(productId, quantity, clock.UtcNow);
      return null;
    }, ct);

  public Task<Result<CartDto>> ChangeQuantityAsync(Guid productId, int quantity, CancellationToken ct = default) =>
    ChangeAsync(async (cart, ct2) => {
      if (quantity > 0) {
        var product = await products.GetByIdAsync(productId, ct2);
        if (product is null) {
          return Errors.NotFound("product");
        }

        if (StockError(product, quantity) is { } error) {
          return error;
        }
      }

      cart.ChangeQuantity(productId, quantity);
      return null;
    }, ct);

  public Task<Result<CartDto>> RemoveAsync(Guid productId, CancellationToken ct = default) =>
    ChangeAsync((cart, _) => {
      cart.RemoveItem(productId);
      return Task.FromResult<Error?>(null);
    }, ct);

  public Task<Result<CartDto>> ClearAsync(CancellationToken ct = default) =>
    ChangeAsync((cart, _) => {
      cart.Clear();
      return Task.FromResult<Error?>(null);
    }, ct);

  internal static async Task<CartDto> ToDtoAsync(IProductsRepository products, ShoppingCart cart, CancellationToken ct) {
    if (cart.IsEmpty) {
      return CartDto.Empty;
    }

    var byId = (await products.GetByIdsAsync(cart.Items.Select(i => i.ProductId), ct)).ToDictionary(p => p.Id);
    var items = cart.Items
      .OrderBy(i => i.AddedAt)
      .Select(i => byId.TryGetValue(i.ProductId, out var p)
        ? new CartItemDto(p.Id, p.Name, p.Photo, p.Price.Amount, i.Quantity, p.Stock, p.IsActive, i.AddedAt)
        : new CartItemDto(i.ProductId, "Товар удалён", null, 0m, i.Quantity, 0, false, i.AddedAt))
      .ToList();

    return new CartDto(items, items.Sum(i => i.Quantity), items.Where(i => i.IsAvailable).Sum(i => i.LineTotal));
  }

  private static Error? StockError(Product product, int desiredQuantity) {
    if (!product.IsActive) {
      return Errors.Conflict("product.inactive", $"Товар «{product.Name}» снят с продажи.");
    }

    return product.Stock < desiredQuantity
      ? Errors.Conflict("product.out_of_stock", $"Недостаточно товара «{product.Name}»: доступно {product.Stock} шт.")
      : null;
  }

  private Task<Result<CartDto>> ChangeAsync(Func<ShoppingCart, CancellationToken, Task<Error?>> change, CancellationToken ct) =>
    Run<CartDto>(async () => {
      var userId = RequireUser(r => r.CanShop());
      if (userId.IsFailure) {
        return Result.Failure<CartDto>(userId.Error!);
      }

      var cart = await carts.GetByUserIdAsync(userId.Value, ct);
      var isNew = cart is null;
      cart ??= ShoppingCart.CreateFor(userId.Value);

      if (await change(cart, ct) is { } error) {
        return error;
      }

      if (isNew) {
        await carts.AddAsync(cart, ct);
      } else {
        await carts.UpdateAsync(cart, ct);
      }

      await unitOfWork.SaveChangesAsync(ct);

      var dto = await ToDtoAsync(products, cart, ct);
      notifier.Publish(this, dto);
      return dto;
    });
}
