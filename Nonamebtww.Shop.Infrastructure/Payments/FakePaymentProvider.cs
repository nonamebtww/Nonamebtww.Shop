using System.Collections.Concurrent;
using Nonamebtww.Shop.Application.Models.DTOs.Payments;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Payments;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Infrastructure.Options;

namespace Nonamebtww.Shop.Infrastructure.Payments;

/// <summary>
/// Заглушка «Платёжной системы» (DFD). Деньги не списываются, сети нет.
/// По умолчанию оплата всегда проходит; отказ можно включить флагом
/// InfrastructureOptions.DeclinePayments, чтобы проверить ветку «оплата отклонена».
/// </summary>
public sealed class FakePaymentProvider : IPaymentProvider {
  private readonly InfrastructureOptions _options;
  private readonly ConcurrentDictionary<string, Transaction> _transactions = new();

  public FakePaymentProvider(InfrastructureOptions options) {
    _options = options;
  }

  public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default) {
    ArgumentNullException.ThrowIfNull(request);
    await Task.Delay(_options.FakeApiLatency, ct);

    if (request.Amount <= 0) {
      return new PaymentResult(PaymentStatus.Declined, null, "Сумма платежа должна быть больше нуля.");
    }

    if (_options.DeclinePayments) {
      return new PaymentResult(PaymentStatus.Declined, null, "Платёж отклонён банком (тестовый режим).");
    }

    var transactionId = $"FAKE-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
    _transactions[transactionId] = new Transaction(request.OrderId, request.Amount, PaymentStatus.Succeeded);

    return new PaymentResult(PaymentStatus.Succeeded, transactionId, "Оплата прошла успешно (тестовый режим).");
  }

  public async Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default) {
    await Task.Delay(_options.FakeApiLatency, ct);

    if (!_transactions.TryGetValue(transactionId, out var transaction)) {
      // После перезапуска приложения память пуста: в прототипе считаем возврат успешным.
      return new PaymentResult(PaymentStatus.Refunded, transactionId, "Возврат оформлен (тестовый режим).");
    }

    if (amount <= 0 || amount > transaction.Amount) {
      return new PaymentResult(PaymentStatus.Declined, transactionId, "Некорректная сумма возврата.");
    }

    _transactions[transactionId] = transaction with { Status = PaymentStatus.Refunded };
    return new PaymentResult(PaymentStatus.Refunded, transactionId, "Возврат оформлен (тестовый режим).");
  }

  public async Task<PaymentResult> CancelAsync(string transactionId, CancellationToken ct = default) {
    await Task.Delay(_options.FakeApiLatency, ct);

    if (_transactions.TryGetValue(transactionId, out var transaction)) {
      _transactions[transactionId] = transaction with { Status = PaymentStatus.Cancelled };
    }

    return new PaymentResult(PaymentStatus.Cancelled, transactionId, "Платёж аннулирован (тестовый режим).");
  }

  private sealed record Transaction(Guid OrderId, decimal Amount, PaymentStatus Status);
}
