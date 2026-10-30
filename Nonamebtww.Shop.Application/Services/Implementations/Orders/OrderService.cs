using Nonamebtww.Shop.Application.Common;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Mappings;
using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Models.DTOs.Payments;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Payments;
using Nonamebtww.Shop.Application.Services.Implementations.Cart;
using Nonamebtww.Shop.Application.Validation;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;
using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Services.Implementations.Orders;

/// <summary>Оформление заказа (DFD 12) и Просмотр заказов (DFD 13). Только покупатель.</summary>
public sealed class OrderService(
  ICurrentUserService currentUser,
  IOrdersRepository orders,
  ICartsRepository carts,
  IProductsRepository products,
  IUsersRepository users,
  IUnitOfWork unitOfWork,
  IPaymentProvider paymentProvider,
  INotificationService notifications,
  CartChangeNotifier cartNotifier,
  IClock clock) : ApplicationService(currentUser), IOrderService {

  public Task<Result<OrderDetailsDto>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default) =>
    Run<OrderDetailsDto>(async () => {
      var userId = RequireUser(r => r.CanShop());
      if (userId.IsFailure) {
        return Result.Failure<OrderDetailsDto>(userId.Error!);
      }

      if (CheckoutRequestValidator.Instance.Validate(request) is { } invalid) {
        return invalid;
      }

      var cart = await carts.GetByUserIdAsync(userId.Value, ct);
      if (cart is null || cart.IsEmpty) {
        return Errors.Conflict("cart.empty", "Корзина пуста.");
      }

      var byId = await LoadProductsAsync(cart.Items.Select(i => i.ProductId), ct);
      foreach (var item in cart.Items) {
        if (!byId.TryGetValue(item.ProductId, out var product)) {
          return Errors.Conflict("product.removed", "Товар из корзины больше не продаётся. Удалите его из корзины.");
        }

        if (!product.IsActive || !product.HasEnough(item.Quantity)) {
          return Errors.Conflict("product.out_of_stock",
            $"Недостаточно товара «{product.Name}»: доступно {product.Stock} шт. Измените количество в корзине.");
        }
      }

      var now = clock.UtcNow;
      // 15.1 создать заказ из корзины → 15.2 доставка и адрес → 15.3 способ оплаты → «Ожидает оплаты».
      var order = Order.CreateFromCart(cart, id => byId[id].Price, now);
      // Самовывоз и постамат: адрес — адрес выбранного пункта, а не введённый покупателем.
      var address = request.DeliveryType.IsToDoor()
        ? request.Address!
        : DeliveryPoints.Find(request.DeliveryPointId, request.DeliveryType)!.Address;
      order.SetDelivery(request.DeliveryType, address.ToDomain());
      order.SetPaymentMethod(request.PaymentMethod);
      order.Submit(now);

      // Резерв: товар списывается с остатка при оформлении и возвращается при отмене.
      foreach (var line in order.Lines) {
        byId[line.ProductId].DecreaseStock(line.Quantity);
        await products.UpdateAsync(byId[line.ProductId], ct);
      }

      await orders.AddAsync(order, ct);
      await unitOfWork.SaveChangesAsync(ct);

      await NotifyCustomerAsync(order, "Заказ оформлен",
        $"Заказ на {order.Total} ожидает оплаты. Оплатите его в течение {Order.PaymentTimeout.TotalMinutes:0} минут.", ct);

      return order.ToDetails(byId, null);
    });

  public Task<Result<IReadOnlyList<DeliveryPointDto>>> GetDeliveryPointsAsync(DeliveryType type, CancellationToken ct = default) =>
    Task.FromResult(Result.Success(DeliveryPoints.Of(type)));

  public Task<Result<OrderDetailsDto>> PayAsync(Guid orderId, CancellationToken ct = default) =>
    Run<OrderDetailsDto>(async () => {
      var loaded = await LoadOwnOrderAsync(orderId, ct);
      if (loaded.IsFailure) {
        return Result.Failure<OrderDetailsDto>(loaded.Error!);
      }

      var order = loaded.Value;
      var now = clock.UtcNow;

      if (order.Status != OrderStatus.AwaitingPayment) {
        return Errors.Conflict("order.not_awaiting_payment", "Заказ не ожидает оплаты.");
      }

      if (order.IsPaymentExpired(now)) {
        await CancelAndRestockAsync(order, OrderCancellationReason.PaymentTimeout, ct);
        return Errors.Conflict("order.payment_expired", "Время на оплату истекло, заказ отменён.");
      }

      // 15.4 передать данные платежа в платёжную систему.
      var payment = await paymentProvider.ChargeAsync(
        new PaymentRequest(order.Id, order.Total.Amount, order.Total.Currency, order.PaymentMethod!.Value), ct);

      if (payment.Status != PaymentStatus.Succeeded || string.IsNullOrWhiteSpace(payment.TransactionId)) {
        order.DeclinePayment(payment.TransactionId, payment.Message, now);
        await orders.UpdateAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await NotifyCustomerAsync(order, "Оплата не прошла",
          payment.Message ?? "Платёж отклонён. Попробуйте ещё раз или выберите другую карту.", ct);
        return Error.Failure("payment.declined", payment.Message ?? "Платёж отклонён платёжной системой.");
      }

      order.MarkPaid(payment.TransactionId, now);
      await orders.UpdateAsync(order, ct);

      // 15.5 очистить корзину и отправить подтверждение заказа.
      var cart = await carts.GetByUserIdAsync(order.UserId, ct);
      if (cart is not null) {
        cart.Clear();
        await carts.UpdateAsync(cart, ct);
      }

      await unitOfWork.SaveChangesAsync(ct);

      if (cart is not null) {
        cartNotifier.Publish(this, await CartService.ToDtoAsync(products, cart, ct));
      }

      await NotifyCustomerAsync(order, "Заказ подтверждён",
        $"Оплата {order.Total} получена. Заказ передан в обработку.", ct);

      return await ToDetailsAsync(order, ct);
    });

  public Task<Result<OrderDetailsDto>> CancelAsync(Guid orderId, CancellationToken ct = default) =>
    Run<OrderDetailsDto>(async () => {
      var loaded = await LoadOwnOrderAsync(orderId, ct);
      if (loaded.IsFailure) {
        return Result.Failure<OrderDetailsDto>(loaded.Error!);
      }

      var order = loaded.Value;
      if (!order.Status.IsCancellable()) {
        return Errors.Conflict("order.not_cancellable",
          $"Заказ в состоянии «{order.Status.ToDisplayName()}» отменить нельзя.");
      }

      await CancelAndRestockAsync(order, OrderCancellationReason.CustomerCancelled, ct);
      return await ToDetailsAsync(order, ct);
    });

  public Task<Result<PagedList<OrderSummaryDto>>> GetMyOrdersAsync(
    OrderListFilter filter = OrderListFilter.All,
    int page = 1,
    int pageSize = 20,
    CancellationToken ct = default) =>
    Run<PagedList<OrderSummaryDto>>(async () => {
      var userId = RequireUser(r => r.CanShop());
      if (userId.IsFailure) {
        return Result.Failure<PagedList<OrderSummaryDto>>(userId.Error!);
      }

      var mine = await orders.GetByUserAsync(userId.Value, ct);
      await CancelExpiredAsync(mine, ct);

      var filtered = mine
        .Where(o => filter switch {
          OrderListFilter.Active => o.Status.IsActive(),
          OrderListFilter.Completed => o.Status.IsFinished(),
          _ => true
        })
        .OrderByDescending(o => o.PlacedAt)
        .ToList();

      var (p, size) = Paging.Normalize(page, pageSize);
      return new PagedList<OrderSummaryDto>(Paging.Slice(filtered, p, size).Select(o => o.ToSummary()).ToList(),
        p, size, filtered.Count);
    });

  public Task<Result<OrderDetailsDto>> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
    Run<OrderDetailsDto>(async () => {
      var loaded = await LoadOwnOrderAsync(orderId, ct);
      if (loaded.IsFailure) {
        return Result.Failure<OrderDetailsDto>(loaded.Error!);
      }

      await CancelExpiredAsync([loaded.Value], ct);
      return await ToDetailsAsync(loaded.Value, ct);
    });

  public Task<Result<int>> CancelExpiredUnpaidAsync(CancellationToken ct = default) =>
    Run<int>(async () => {
      var awaiting = await orders.GetByStatusAsync([OrderStatus.AwaitingPayment], ct);
      return await CancelExpiredAsync(awaiting, ct);
    });

  /// <summary>Переход «Ожидает оплаты» → «Отменён» по after(30 мин).</summary>
  private async Task<int> CancelExpiredAsync(IEnumerable<Order> candidates, CancellationToken ct) {
    var now = clock.UtcNow;
    var count = 0;

    foreach (var order in candidates.Where(o => o.IsPaymentExpired(now)).ToList()) {
      await CancelAndRestockAsync(order, OrderCancellationReason.PaymentTimeout, ct);
      count++;
    }

    return count;
  }

  /// <summary>Отмена: вернуть товары в остаток, вернуть оплату, уведомить покупателя.</summary>
  private async Task CancelAndRestockAsync(Order order, OrderCancellationReason reason, CancellationToken ct) {
    var paid = order.SucceededPayment;
    var previous = order.Status;

    order.Cancel(clock.UtcNow, reason);

    // Товар списан при оформлении (до «Ожидает оплаты»), значит возвращаем для любого отменяемого состояния кроме «Оформление».
    if (previous != OrderStatus.Checkout) {
      var byId = await LoadProductsAsync(order.Lines.Select(l => l.ProductId), ct);
      foreach (var line in order.Lines) {
        if (byId.TryGetValue(line.ProductId, out var product)) {
          product.IncreaseStock(line.Quantity);
          await products.UpdateAsync(product, ct);
        }
      }
    }

    await orders.UpdateAsync(order, ct);
    await unitOfWork.SaveChangesAsync(ct);

    if (paid?.TransactionId is { } transactionId && paid.Status == PaymentStatus.Refunded) {
      await paymentProvider.RefundAsync(transactionId, paid.Amount.Amount, ct);
    }

    var message = reason == OrderCancellationReason.PaymentTimeout
      ? "Заказ отменён: время на оплату истекло."
      : paid is not null ? "Заказ отменён, деньги вернутся на карту." : "Заказ отменён.";
    await NotifyCustomerAsync(order, "Заказ отменён", message, ct);
  }

  private async Task<Result<Order>> LoadOwnOrderAsync(Guid orderId, CancellationToken ct) {
    var userId = RequireUser(r => r.CanShop());
    if (userId.IsFailure) {
      return Result.Failure<Order>(userId.Error!);
    }

    var order = await orders.GetByIdAsync(orderId, ct);
    return order is null || order.UserId != userId.Value ? Errors.NotFound("order") : order;
  }

  private async Task<OrderDetailsDto> ToDetailsAsync(Order order, CancellationToken ct) {
    var byId = await LoadProductsAsync(order.Items.Select(i => i.ProductId), ct);
    var courier = order.Delivery is { } d ? await users.GetByIdAsync(d.CourierId, ct) : null;
    return order.ToDetails(byId, courier?.FullName);
  }

  private async Task<Dictionary<Guid, Product>> LoadProductsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
    (await products.GetByIdsAsync(ids.Distinct().ToList(), ct)).ToDictionary(p => p.Id);

  private async Task NotifyCustomerAsync(Order order, string subject, string body, CancellationToken ct) {
    var customer = await users.GetByIdAsync(order.UserId, ct);
    await notifications.SendAsync(new NotificationMessage(NotificationChannel.InApp, null, subject, body, order.UserId), ct);

    if (customer is not null) {
      await notifications.SendToContactAsync(customer, subject, body, ct);
    }
  }
}
