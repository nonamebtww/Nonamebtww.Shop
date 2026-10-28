namespace Nonamebtww.Shop.Application.Models.DTOs.Orders;

/// <summary>Адрес доставки (VO DeliveryAddress в Domain).</summary>
public sealed record AddressDto(
  string City,
  string Street,
  string House,
  string? Apartment = null,
  string? PostalCode = null,
  string? Comment = null) {
  public override string ToString() {
    var line = $"{City}, {Street}, д. {House}";
    return string.IsNullOrWhiteSpace(Apartment) ? line : $"{line}, кв. {Apartment}";
  }
}
