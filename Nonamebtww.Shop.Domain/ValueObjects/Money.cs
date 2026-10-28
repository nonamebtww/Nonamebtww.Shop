using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>Денежная сумма. Отрицательные суммы запрещены, округление до копеек.</summary>
public sealed record Money : IComparable<Money> {
  public const string DefaultCurrency = "RUB";

  public Money(decimal amount, string currency = DefaultCurrency) {
    if (amount < 0) throw new DomainException("Сумма не может быть отрицательной.");
    if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
      throw new DomainException("Код валюты должен состоять из трёх букв (ISO 4217).");

    Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    Currency = currency.Trim().ToUpperInvariant();
  }

  public decimal Amount { get; }
  public string Currency { get; }

  public static Money Zero(string currency = DefaultCurrency) => new(0, currency);

  public static Money Rub(decimal amount) => new(amount);

  public bool IsZero => Amount == 0;

  public Money Add(Money other) {
    EnsureSameCurrency(other);
    return new Money(Amount + other.Amount, Currency);
  }

  public Money Subtract(Money other) {
    EnsureSameCurrency(other);
    return new Money(Amount - other.Amount, Currency);
  }

  public Money Multiply(int quantity) {
    if (quantity < 0) throw new DomainException("Количество не может быть отрицательным.");
    return new Money(Amount * quantity, Currency);
  }

  public int CompareTo(Money? other) {
    if (other is null) return 1;
    EnsureSameCurrency(other);
    return Amount.CompareTo(other.Amount);
  }

  public static Money operator +(Money left, Money right) => left.Add(right);

  public static Money operator -(Money left, Money right) => left.Subtract(right);

  public static Money operator *(Money money, int quantity) => money.Multiply(quantity);

  public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

  public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

  public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

  public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

  public override string ToString() => $"{Amount:0.00} {Currency}";

  private void EnsureSameCurrency(Money other) {
    if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
      throw new DomainException($"Нельзя сочетать суммы в разных валютах: {Currency} и {other.Currency}.");
  }
}
