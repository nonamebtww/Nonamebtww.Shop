using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Common.Validation;
using Nonamebtww.Shop.Application.Models.DTOs.Delivery;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;

namespace Nonamebtww.Shop.Application.Validation;

public sealed class CheckoutRequestValidator : IValidator<CheckoutRequest> {
  public static readonly CheckoutRequestValidator Instance = new();

  public Error? Validate(CheckoutRequest model) => model.DeliveryType.IsToDoor()
    ? new ValidationBuilder()
      .Rule(model.Address is not null, nameof(model.Address), "Укажите адрес доставки.")
      .Rule(!string.IsNullOrWhiteSpace(model.Address?.City), "Address.City", "Укажите город.")
      .Rule(!string.IsNullOrWhiteSpace(model.Address?.Street), "Address.Street", "Укажите улицу.")
      .Rule(!string.IsNullOrWhiteSpace(model.Address?.House), "Address.House", "Укажите дом.")
      .Build()
    : new ValidationBuilder()
      .Rule(DeliveryPoints.Find(model.DeliveryPointId, model.DeliveryType) is not null, nameof(model.DeliveryPointId),
        model.DeliveryType == DeliveryType.Pickup ? "Выберите пункт самовывоза." : "Выберите постамат.")
      .Build();
}

public sealed class AssignCourierRequestValidator : IValidator<AssignCourierRequest> {
  public static readonly AssignCourierRequestValidator Instance = new();

  public Error? Validate(AssignCourierRequest model) => new ValidationBuilder()
    .Rule(model.OrderId != Guid.Empty, nameof(model.OrderId), "Не указан заказ.")
    .Rule(model.CourierId != Guid.Empty, nameof(model.CourierId), "Выберите курьера.")
    .Build();
}

public sealed class ReportDeliveryFailedRequestValidator : IValidator<ReportDeliveryFailedRequest> {
  public static readonly ReportDeliveryFailedRequestValidator Instance = new();

  public Error? Validate(ReportDeliveryFailedRequest model) => new ValidationBuilder()
    .Rule(model.OrderId != Guid.Empty, nameof(model.OrderId), "Не указан заказ.")
    .Rule(!string.IsNullOrWhiteSpace(model.Reason), nameof(model.Reason), "Укажите причину.")
    .Build();
}
