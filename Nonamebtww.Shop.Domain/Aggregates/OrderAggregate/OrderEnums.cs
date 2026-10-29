namespace Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Тип доставки. Курьер везёт заказ либо покупателю (Standard, Express),
/// либо в пункт выдачи / постамат (Pickup, ParcelLocker), откуда покупатель забирает его сам.
/// </summary>
public enum DeliveryType {
  /// <summary>Обычная курьерская доставка.</summary>
  Standard = 0,

  /// <summary>Срочная курьерская доставка.</summary>
  Express = 1,

  /// <summary>Самовывоз из пункта выдачи магазина.</summary>
  Pickup = 2,

  /// <summary>Доставка в постамат.</summary>
  ParcelLocker = 3,
}

public static class DeliveryTypeExtensions {
  /// <summary>Заказ везут на адрес покупателя (а не в пункт выдачи или постамат).</summary>
  public static bool IsToDoor(this DeliveryType type) => type is DeliveryType.Standard or DeliveryType.Express;
}

/// <summary>Способ (тип) оплаты. Оплата онлайн до подтверждения заказа.</summary>
public enum PaymentMethod {
  /// <summary>Банковская карта.</summary>
  Card = 0,

  /// <summary>Система быстрых платежей.</summary>
  Sbp = 1,
}

/// <summary>Статус платежа (IDEF1X «Платёж.статус_оплаты»).</summary>
public enum PaymentStatus {
  Pending = 0,
  Succeeded = 1,
  Declined = 2,

  /// <summary>Аннулирован до списания.</summary>
  Cancelled = 3,

  /// <summary>Деньги возвращены покупателю.</summary>
  Refunded = 4,
}

/// <summary>Статус доставки (IDEF1X «Доставка.статус_доставки»).</summary>
public enum DeliveryStatus {
  /// <summary>Курьер назначен логистом.</summary>
  Assigned = 0,

  /// <summary>Курьер принял заказ.</summary>
  Accepted = 1,

  /// <summary>Заказ вручён.</summary>
  Delivered = 2,

  /// <summary>Вручение не состоялось, назначение снято.</summary>
  Failed = 3,

  /// <summary>Доставка отменена вместе с заказом.</summary>
  Cancelled = 4,
}

/// <summary>Причина отмены заказа.</summary>
public enum OrderCancellationReason {
  /// <summary>Покупатель отказался от оформления.</summary>
  CustomerRefusedCheckout = 0,

  /// <summary>Покупатель отменил заказ.</summary>
  CustomerCancelled = 1,

  /// <summary>Заказ не оплачен за отведённое время (after 30 мин).</summary>
  PaymentTimeout = 2,
}
