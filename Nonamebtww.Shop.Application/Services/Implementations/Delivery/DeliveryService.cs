using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Delivery;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Services.Implementations.Delivery;

/// <summary>Распределение доставки (DFD 14, Логист) и Доставка заказа (DFD 15, Курьер).</summary>
public sealed class DeliveryService(
  ICurrentUserService currentUser,
  IOrdersRepository orders,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  INotificationService notifications,
  IClock clock) : ApplicationService(currentUser), IDeliveryService {

  private static readonly OrderStatus[] DispatchStatuses = [OrderStatus.Confirmed, OrderStatus.CourierAssigned];

  #region Логист

  public Task<Result<IReadOnlyList<DispatchOrderDto>>> GetOrdersToDispatchAsync(CancellationToken ct = default) =>
    Run<IReadOnlyList<DispatchOrderDto>>(async () => {
      var logist = RequireUser(r => r.CanAssignCouriers());
      if (logist.IsFailure) {
        return Result.Failure<IReadOnlyList<DispatchOrderDto>>(logist.Error!);
      }

      var list = await orders.GetByStatusAsync(DispatchStatuses, ct);
      var people = await LoadUsersAsync(list.SelectMany(o => new[] { o.UserId, o.Delivery?.CourierId ?? Guid.Empty }), ct);

      IReadOnlyList<DispatchOrderDto> result = list
        .OrderBy(o => o.Status)
        .ThenBy(o => o.ConfirmedAt ?? o.PlacedAt)
        .Select(o => ToDispatch(o, people))
        .ToList();

      return Result.Success(result);
    });

  public Task<Result<IReadOnlyList<CourierDto>>> GetCouriersAsync(CancellationToken ct = default) =>
    Run<IReadOnlyList<CourierDto>>(async () => {
      var logist = RequireUser(r => r.CanAssignCouriers());
      if (logist.IsFailure) {
        return Result.Failure<IReadOnlyList<CourierDto>>(logist.Error!);
      }

      var couriers = await users.GetByRoleAsync(UserRole.Courier, ct);
      var result = new List<CourierDto>();

      foreach (var courier in couriers.Where(c => !c.IsBlocked).OrderBy(c => c.FullName)) {
        var active = (await orders.GetByCourierAsync(courier.Id, ct)).Count(IsActiveTask);
        result.Add(new CourierDto(courier.Id, courier.FullName, courier.Phone?.Value, active));
      }

      return result;
    });

  public Task<Result<DispatchOrderDto>> AssignCourierAsync(AssignCourierRequest request, CancellationToken ct = default) =>
    Run<DispatchOrderDto>(async () => {
      var logist = RequireUser(r => r.CanAssignCouriers());
      if (logist.IsFailure) {
        return Result.Failure<DispatchOrderDto>(logist.Error!);
      }

      if (AssignCourierRequestValidator.Instance.Validate(request) is { } invalid) {
        return invalid;
      }

      var order = await orders.GetByIdAsync(request.OrderId, ct);
      if (order is null) {
        return Errors.NotFound("order");
      }

      var courier = await users.GetByIdAsync(request.CourierId, ct);
      if (courier is null || courier.Role != UserRole.Courier) {
        return Errors.NotFound("courier");
      }

      if (courier.IsBlocked) {
        return Errors.Conflict("courier.blocked", "Курьер заблокирован.");
      }

      // 13.3–13.4 назначить курьера и зарегистрировать доставку; 12.1 сформировать задание курьеру.
      order.AssignCourier(courier.Id, clock.UtcNow);
      await orders.UpdateAsync(order, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await notifications.SendAsync(new NotificationMessage(NotificationChannel.InApp, null,
        "Новое задание на доставку", $"Вам назначен заказ на {order.Total}. Адрес: {order.DeliveryAddress}.", courier.Id), ct);

      var people = await LoadUsersAsync([order.UserId, courier.Id], ct);
      return ToDispatch(order, people);
    });

  #endregion

  #region Курьер

  public Task<Result<IReadOnlyList<DeliveryTaskDto>>> GetMyTasksAsync(bool activeOnly = true, CancellationToken ct = default) =>
    Run<IReadOnlyList<DeliveryTaskDto>>(async () => {
      var courierId = RequireUser(r => r == UserRole.Courier);
      if (courierId.IsFailure) {
        return Result.Failure<IReadOnlyList<DeliveryTaskDto>>(courierId.Error!);
      }

      var list = (await orders.GetByCourierAsync(courierId.Value, ct))
        .Where(o => o.Delivery?.CourierId == courierId.Value)
        .Where(o => !activeOnly || IsActiveTask(o))
        .OrderBy(o => o.Status == OrderStatus.InDelivery ? 0 : 1)
        .ThenByDescending(o => o.Delivery!.AssignedAt)
        .ToList();

      var customers = await LoadUsersAsync(list.Select(o => o.UserId), ct);
      IReadOnlyList<DeliveryTaskDto> result = list.Select(o => ToTask(o, customers)).ToList();
      return Result.Success(result);
    });

  public Task<Result<DeliveryTaskDto>> AcceptAsync(Guid orderId, CancellationToken ct = default) =>
    ChangeTaskAsync(orderId, (order, courierId, now) => order.AcceptByCourier(courierId, now),
      "Заказ передан в доставку", "Курьер забрал ваш заказ и уже в пути.", ct);

  public Task<Result<DeliveryTaskDto>> ConfirmDeliveredAsync(Guid orderId, CancellationToken ct = default) =>
    ChangeTaskAsync(orderId, (order, courierId, now) => order.ConfirmDelivered(courierId, now),
      "Заказ вручён", "Спасибо за покупку! Будем рады вашему отзыву о товарах.", ct);

  public Task<Result<DeliveryTaskDto>> ReportFailedAsync(ReportDeliveryFailedRequest request, CancellationToken ct = default) {
    if (ReportDeliveryFailedRequestValidator.Instance.Validate(request) is { } invalid) {
      return Task.FromResult(Result.Failure<DeliveryTaskDto>(invalid));
    }

    return ChangeTaskAsync(request.OrderId, (order, courierId, _) => order.ReportDeliveryFailed(courierId, request.Reason),
      "Доставка не состоялась", $"Не удалось вручить заказ: {request.Reason}. Мы назначим новую доставку.", ct);
  }

  private Task<Result<DeliveryTaskDto>> ChangeTaskAsync(
    Guid orderId,
    Action<Order, Guid, DateTimeOffset> change,
    string subject,
    string body,
    CancellationToken ct) =>
    Run<DeliveryTaskDto>(async () => {
      var courierId = RequireUser(r => r == UserRole.Courier);
      if (courierId.IsFailure) {
        return Result.Failure<DeliveryTaskDto>(courierId.Error!);
      }

      var order = await orders.GetByIdAsync(orderId, ct);
      if (order is null || order.Delivery?.CourierId != courierId.Value) {
        return Errors.NotFound("order");
      }

      change(order, courierId.Value, clock.UtcNow);
      await orders.UpdateAsync(order, ct);
      await unitOfWork.SaveChangesAsync(ct);

      // 12.4 уведомить покупателя.
      await notifications.SendAsync(new NotificationMessage(NotificationChannel.InApp, null, subject, body, order.UserId), ct);

      var customers = await LoadUsersAsync([order.UserId], ct);
      return ToTask(order, customers);
    });

  #endregion

  private static bool IsActiveTask(Order order) =>
    order.Status is OrderStatus.CourierAssigned or OrderStatus.InDelivery;

  private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken ct) {
    var result = new Dictionary<Guid, User>();
    foreach (var id in ids.Where(id => id != Guid.Empty).Distinct()) {
      if (await users.GetByIdAsync(id, ct) is { } user) {
        result[id] = user;
      }
    }

    return result;
  }

  private static DispatchOrderDto ToDispatch(Order order, IReadOnlyDictionary<Guid, User> people) {
    var customer = people.GetValueOrDefault(order.UserId);
    var courier = order.Delivery is { } d ? people.GetValueOrDefault(d.CourierId) : null;

    return new DispatchOrderDto(order.Id, order.PlacedAt, customer?.FullName ?? "Покупатель", customer?.Phone?.Value,
      order.DeliveryAddress!.ToDto(), order.Total.Amount, order.TotalQuantity, order.Status,
      order.Delivery?.CourierId, courier?.FullName);
  }

  private static DeliveryTaskDto ToTask(Order order, IReadOnlyDictionary<Guid, User> customers) {
    var customer = customers.GetValueOrDefault(order.UserId);

    return new DeliveryTaskDto(order.Id, customer?.FullName ?? "Покупатель", customer?.Phone?.Value,
      order.DeliveryAddress!.ToDto(), order.Total.Amount, order.SucceededPayment is not null, order.TotalQuantity,
      order.Delivery!.Status, order.Delivery.AssignedAt, order.Delivery.DeliveredAt);
  }
}
