using Nonamebtww.Shop.Application.Models.DTOs.Payments;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Payments;

/// <summary>Внешняя сущность DFD «Платёжная система». В прототипе — fake.</summary>
public interface IPaymentProvider {
  Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
  Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default);
  Task<PaymentResult> CancelAsync(string transactionId, CancellationToken ct = default);
}
