using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Delivery;

/// <summary>Процессы DFD «Распределение доставки» (Логист) и «Доставка заказа» (Курьер).</summary>
public interface IDeliveryService {
  /// <summary>«Заказы к доставке»: подтверждённые заказы без курьера и уже назначенные (для переназначения).</summary>
  Task<Result<IReadOnlyList<DispatchOrderDto>>> GetOrdersToDispatchAsync(CancellationToken ct = default);

  Task<Result<IReadOnlyList<CourierDto>>> GetCouriersAsync(CancellationToken ct = default);

  /// <summary>«Назначение курьера» (13.3–13.4): заказ переходит в «Курьер назначен».</summary>
  Task<Result<DispatchOrderDto>> AssignCourierAsync(AssignCourierRequest request, CancellationToken ct = default);

  /// <summary>«Задания на доставку» текущего курьера.</summary>
  Task<Result<IReadOnlyList<DeliveryTaskDto>>> GetMyTasksAsync(bool activeOnly = true, CancellationToken ct = default);

  /// <summary>Курьер принял заказ (12.2): «В доставке».</summary>
  Task<Result<DeliveryTaskDto>> AcceptAsync(Guid orderId, CancellationToken ct = default);

  /// <summary>Курьер подтвердил вручение (12.3): «Завершён».</summary>
  Task<Result<DeliveryTaskDto>> ConfirmDeliveredAsync(Guid orderId, CancellationToken ct = default);

  /// <summary>Вручение не состоялось: заказ возвращается в «Подтверждён», назначение снимается.</summary>
  Task<Result<DeliveryTaskDto>> ReportFailedAsync(ReportDeliveryFailedRequest request, CancellationToken ct = default);
}
