using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;

namespace Nonamebtww.Shop.Application.Api.Abstractions.Interfaces;

public interface IDeliveryApi {
  Task<Result<IReadOnlyList<DispatchOrderDto>>> GetOrdersToDispatchAsync(CancellationToken ct = default);
  Task<Result<IReadOnlyList<CourierDto>>> GetCouriersAsync(CancellationToken ct = default);
  Task<Result<DispatchOrderDto>> AssignCourierAsync(AssignCourierRequest request, CancellationToken ct = default);
  Task<Result<IReadOnlyList<DeliveryTaskDto>>> GetMyTasksAsync(bool activeOnly, CancellationToken ct = default);
  Task<Result<DeliveryTaskDto>> AcceptAsync(Guid orderId, CancellationToken ct = default);
  Task<Result<DeliveryTaskDto>> ConfirmDeliveredAsync(Guid orderId, CancellationToken ct = default);
  Task<Result<DeliveryTaskDto>> ReportFailedAsync(ReportDeliveryFailedRequest request, CancellationToken ct = default);
}
