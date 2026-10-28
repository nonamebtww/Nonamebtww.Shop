using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Cart;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface ICartApi {
  Task<Result<CartDto>> GetAsync(CancellationToken ct = default);

  /// <summary>Задаёт количество товара в корзине; 0 — удалить позицию.</summary>
  Task<Result<CartDto>> SetQuantityAsync(Guid productId, int quantity, CancellationToken ct = default);

  Task<Result<CartDto>> ClearAsync(CancellationToken ct = default);
}
