using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Payments;

/// <summary>«Данные платежа», которые передаются в платёжную систему (15.4).</summary>
public sealed record PaymentRequest(Guid OrderId, decimal Amount, string Currency, PaymentMethod Method);
