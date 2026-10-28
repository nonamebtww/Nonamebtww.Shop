using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

/// <summary>Код подтверждения (IDEF1X «Код подтверждения»), часть агрегата <see cref="User"/>.</summary>
public class ConfirmationCode : Entity<Guid> {
  private ConfirmationCode() { }

  internal ConfirmationCode(ConfirmationCodePurpose purpose, string code, DateTimeOffset createdAt, TimeSpan lifetime)
    : base(Guid.NewGuid()) {
    Purpose = purpose;
    Code = Guard.NotEmpty(code, "Код", 32);
    CreatedAt = createdAt;
    ExpiresAt = createdAt + lifetime;
  }

  public string Code { get; private set; } = null!;
  public ConfirmationCodePurpose Purpose { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }

  /// <summary>Срок действия.</summary>
  public DateTimeOffset ExpiresAt { get; private set; }

  public bool IsUsed { get; private set; }

  public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

  public bool IsValid(DateTimeOffset now) => !IsUsed && !IsExpired(now);

  internal bool Matches(string code) => string.Equals(Code, code.Trim(), StringComparison.Ordinal);

  internal void MarkUsed() => IsUsed = true;
}
