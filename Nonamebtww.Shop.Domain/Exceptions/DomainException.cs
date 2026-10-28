namespace Nonamebtww.Shop.Domain.Exceptions;

/// <summary>Нарушение бизнес-правила предметной области.</summary>
public class DomainException : Exception {
  public DomainException(string message) : base(message) { }

  public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
