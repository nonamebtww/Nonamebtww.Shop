using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Models.Enums;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;

/// <summary>Процессы DFD «Оформление заказа» и «Просмотр заказов».</summary>
public interface IOrderService {
  /// <summary>
  /// Создаёт заказ из корзины (15.1), задаёт доставку (15.2) и способ оплаты (15.3), переводит в «Ожидает оплаты».
  /// Товары резервируются (списываются с остатка).
  /// </summary>
  Task<Result<OrderDetailsDto>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default);

  /// <summary>Оплата через платёжную систему (15.4). Успех — «Подтверждён», корзина очищается (15.5).</summary>
  Task<Result<OrderDetailsDto>> PayAsync(Guid orderId, CancellationToken ct = default);

  /// <summary>Отмена покупателем. Остаток возвращается, оплата возвращается.</summary>
  Task<Result<OrderDetailsDto>> CancelAsync(Guid orderId, CancellationToken ct = default);

  Task<Result<PagedList<OrderSummaryDto>>> GetMyOrdersAsync(
    OrderListFilter filter = OrderListFilter.All,
    int page = 1,
    int pageSize = 20,
    CancellationToken ct = default);

  Task<Result<OrderDetailsDto>> GetOrderAsync(Guid orderId, CancellationToken ct = default);

  /// <summary>Отменяет заказы, не оплаченные за 30 минут. Вызывать при старте и при открытии списка заказов.</summary>
  Task<Result<int>> CancelExpiredUnpaidAsync(CancellationToken ct = default);
}
