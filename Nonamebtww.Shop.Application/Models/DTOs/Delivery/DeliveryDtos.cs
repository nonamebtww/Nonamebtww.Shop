using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Delivery;

/// <summary>«Заказы к доставке» для логиста.</summary>
public sealed record DispatchOrderDto(
  Guid OrderId,
  DateTimeOffset CreatedAt,
  string CustomerName,
  string? CustomerPhone,
  AddressDto Address,
  decimal TotalAmount,
  int ItemsCount,
  OrderStatus Status,
  Guid? CourierId,
  string? CourierName);

public sealed record CourierDto(Guid Id, string FullName, string? Phone, int ActiveTasks);

/// <summary>«Назначение курьера».</summary>
public sealed record AssignCourierRequest(Guid OrderId, Guid CourierId);

/// <summary>«Задание на доставку» для курьера.</summary>
public sealed record DeliveryTaskDto(
  Guid OrderId,
  string CustomerName,
  string? CustomerPhone,
  AddressDto Address,
  decimal TotalAmount,
  bool IsPaid,
  int ItemsCount,
  DeliveryStatus Status,
  DateTimeOffset AssignedAt,
  DateTimeOffset? DeliveredAt);

/// <summary>«Статус доставки»: вручение не состоялось.</summary>
public sealed record ReportDeliveryFailedRequest(Guid OrderId, string Reason);
