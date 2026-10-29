using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Application.Common;

/// <summary>Справочник пунктов самовывоза и постаматов магазина (в прототипе — статический).</summary>
public static class DeliveryPoints {
  public static readonly IReadOnlyList<DeliveryPointDto> All = [
    new("pickup-tverskaya", DeliveryType.Pickup, "Магазин на Тверской",
      new AddressDto("Москва", "ул. Тверская", "12", PostalCode: "125009", Comment: "Пункт выдачи «Магазин на Тверской»"),
      "Ежедневно 10:00–22:00"),
    new("pickup-arbat", DeliveryType.Pickup, "Пункт выдачи на Арбате",
      new AddressDto("Москва", "ул. Арбат", "24", PostalCode: "119002", Comment: "Пункт выдачи на Арбате"),
      "Пн–Сб 09:00–21:00"),
    new("pickup-nevsky", DeliveryType.Pickup, "Магазин на Невском",
      new AddressDto("Санкт-Петербург", "Невский пр-т", "48", PostalCode: "191025", Comment: "Пункт выдачи «Магазин на Невском»"),
      "Ежедневно 10:00–22:00"),
    new("locker-belorusskaya", DeliveryType.ParcelLocker, "Постамат у м. Белорусская",
      new AddressDto("Москва", "Ленинградский пр-т", "1", PostalCode: "125047", Comment: "Постамат у м. Белорусская"),
      "Круглосуточно"),
    new("locker-aviapark", DeliveryType.ParcelLocker, "Постамат в ТЦ «Авиапарк»",
      new AddressDto("Москва", "Ходынский б-р", "4", PostalCode: "125167", Comment: "Постамат в ТЦ «Авиапарк», 1 этаж"),
      "10:00–22:00"),
    new("locker-moskovsky", DeliveryType.ParcelLocker, "Постамат у м. Московская",
      new AddressDto("Санкт-Петербург", "Московский пр-т", "189", PostalCode: "196066", Comment: "Постамат у м. Московская"),
      "Круглосуточно"),
  ];

  public static IReadOnlyList<DeliveryPointDto> Of(DeliveryType type) => All.Where(x => x.Type == type).ToList();

  public static DeliveryPointDto? Find(string? id, DeliveryType type) =>
    All.FirstOrDefault(x => x.Type == type && string.Equals(x.Id, id, StringComparison.Ordinal));
}
