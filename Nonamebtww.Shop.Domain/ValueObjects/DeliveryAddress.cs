using Nonamebtww.Shop.Domain.Common;

namespace Nonamebtww.Shop.Domain.ValueObjects;

/// <summary>Адрес доставки заказа.</summary>
public sealed record DeliveryAddress {
  public DeliveryAddress(
    string city,
    string street,
    string house,
    string? apartment = null,
    string? postalCode = null,
    string? comment = null) {
    City = Guard.NotEmpty(city, "Город", 128);
    Street = Guard.NotEmpty(street, "Улица", 256);
    House = Guard.NotEmpty(house, "Дом", 32);
    Apartment = Guard.Optional(apartment, "Квартира", 32);
    PostalCode = Guard.Optional(postalCode, "Индекс", 16);
    Comment = Guard.Optional(comment, "Комментарий курьеру", 512);
  }

  public string City { get; }
  public string Street { get; }
  public string House { get; }
  public string? Apartment { get; }
  public string? PostalCode { get; }
  public string? Comment { get; }

  public override string ToString() {
    var parts = new List<string> { City, Street, $"д. {House}" };
    if (Apartment is not null) parts.Add($"кв. {Apartment}");
    if (PostalCode is not null) parts.Insert(0, PostalCode);
    return string.Join(", ", parts);
  }
}
