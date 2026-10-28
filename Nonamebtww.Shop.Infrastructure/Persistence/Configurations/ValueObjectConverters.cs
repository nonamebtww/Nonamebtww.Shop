using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Configurations;

/// <summary>Преобразования однозначных value objects в столбцы и маппинг составных (Money, адрес).</summary>
internal static class ValueObjectConverters {
  public static readonly ValueConverter<Login, string> Login = new(v => v.Value, v => new Login(v));
  public static readonly ValueConverter<Email, string> Email = new(v => v.Value, v => new Email(v));
  public static readonly ValueConverter<PhoneNumber, string> Phone = new(v => v.Value, v => new PhoneNumber(v));
  public static readonly ValueConverter<Sku, string> Sku = new(v => v.Value, v => new Sku(v));
  public static readonly ValueConverter<Rating, int> Rating = new(v => v.Value, v => new Rating(v));

  /// <summary>Money как два столбца: &lt;prefix&gt;_amount и &lt;prefix&gt;_currency.</summary>
  public static void MapMoney(this ComplexPropertyBuilder<Money> money, string prefix) {
    money.Property(m => m.Amount).HasColumnName($"{prefix}_amount").IsRequired();
    money.Property(m => m.Currency).HasColumnName($"{prefix}_currency").HasMaxLength(3).IsRequired();
  }
}
