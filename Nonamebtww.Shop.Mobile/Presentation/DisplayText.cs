using System.Globalization;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Mobile.Presentation;

/// <summary>Russian captions and colors for Application enums.</summary>
public static class DisplayText {
  private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

  public static string Money(decimal value) => string.Format(Ru, "{0:N0} ₽", value);

  public static string Date(DateTimeOffset value) => value.ToLocalTime().ToString("d MMMM yyyy, HH:mm", Ru);

  public static string ShortId(Guid id) => "№ " + id.ToString("N")[..8].ToUpperInvariant();

  public static string Stars(double? rating) => rating is null ? "нет оценок" : $"★ {rating.Value:0.0}";

  public static string Stars(int rating) => new string('★', Math.Clamp(rating, 0, 5)) + new string('☆', 5 - Math.Clamp(rating, 0, 5));

  public static string Of(UserRole role) => role.ToDisplayName();

  public static string Of(OrderStatus status) => status.ToDisplayName();

  public static Color ColorOf(OrderStatus status) => status switch {
    OrderStatus.Checkout or OrderStatus.AwaitingPayment => Color.FromArgb("#E0A100"),
    OrderStatus.Confirmed or OrderStatus.CourierAssigned => Color.FromArgb("#2F5BEA"),
    OrderStatus.InDelivery => Color.FromArgb("#7B4FE0"),
    OrderStatus.Completed => Color.FromArgb("#1E9E5A"),
    OrderStatus.Cancelled => Color.FromArgb("#8A8F98"),
    _ => Colors.Gray
  };

  public static string Of(DeliveryStatus status) => status switch {
    DeliveryStatus.Assigned => "Назначена курьеру",
    DeliveryStatus.Accepted => "Принята курьером",
    DeliveryStatus.Delivered => "Вручена",
    DeliveryStatus.Failed => "Вручение не состоялось",
    DeliveryStatus.Cancelled => "Отменена",
    _ => status.ToString()
  };

  public static string Of(DeliveryType type) => type switch {
    DeliveryType.Standard => "Курьером, обычная",
    DeliveryType.Express => "Курьером, срочная",
    _ => type.ToString()
  };

  public static string Of(PaymentMethod method) => method switch {
    PaymentMethod.Card => "Банковская карта",
    PaymentMethod.Sbp => "СБП",
    _ => method.ToString()
  };

  public static string Of(PaymentStatus status) => status switch {
    PaymentStatus.Pending => "Ожидает",
    PaymentStatus.Succeeded => "Оплачен",
    PaymentStatus.Declined => "Отклонён",
    PaymentStatus.Cancelled => "Аннулирован",
    PaymentStatus.Refunded => "Возвращён",
    _ => status.ToString()
  };

  public static string Of(ProductSort sort) => sort switch {
    ProductSort.Default => "По умолчанию",
    ProductSort.PriceAscending => "Сначала дешевле",
    ProductSort.PriceDescending => "Сначала дороже",
    ProductSort.NameAscending => "По названию",
    ProductSort.RatingDescending => "По рейтингу",
    _ => sort.ToString()
  };

  public static string Of(ExternalAuthProvider provider) => provider switch {
    ExternalAuthProvider.Google => "Google",
    ExternalAuthProvider.Apple => "Apple ID",
    ExternalAuthProvider.Yandex => "Yandex ID",
    _ => provider.ToString()
  };
}

/// <summary>Picker item: caption plus enum value.</summary>
public sealed record Option<T>(T Value, string Caption) {
  public override string ToString() => Caption;
}
