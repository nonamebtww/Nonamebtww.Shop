using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Models.Enums;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface IOrderApi {
  Task<Result<OrderDetailsDto>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
  Task<Result<OrderDetailsDto>> PayAsync(Guid orderId, CancellationToken ct = default);
  Task<Result<OrderDetailsDto>> CancelAsync(Guid orderId, CancellationToken ct = default);
  Task<Result<PagedList<OrderSummaryDto>>> GetMyOrdersAsync(OrderListFilter filter, int page, int pageSize, CancellationToken ct = default);
  Task<Result<OrderDetailsDto>> GetOrderAsync(Guid orderId, CancellationToken ct = default);
}
