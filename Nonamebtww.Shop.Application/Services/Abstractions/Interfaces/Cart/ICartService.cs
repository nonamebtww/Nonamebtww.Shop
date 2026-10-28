using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Cart;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;

/// <summary>Процесс DFD «Корзина». Все изменения возвращают актуальное содержимое корзины.</summary>
public interface ICartService {
  Task<Result<CartDto>> GetAsync(CancellationToken ct = default);
  Task<Result<CartDto>> AddAsync(Guid productId, int quantity = 1, CancellationToken ct = default);
  Task<Result<CartDto>> ChangeQuantityAsync(Guid productId, int quantity, CancellationToken ct = default);
  Task<Result<CartDto>> RemoveAsync(Guid productId, CancellationToken ct = default);
  Task<Result<CartDto>> ClearAsync(CancellationToken ct = default);

  /// <summary>Срабатывает после любого изменения корзины (для бейджа на вкладке).</summary>
  event EventHandler<CartDto>? Changed;
}
