using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Nonamebtww.Shop.Application.Models.DTOs.Orders;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Orders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Mobile.Converters;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Cart;

/// <summary>Delivery option of the checkout form.</summary>
public sealed record DeliveryOption(DeliveryType Type, string Caption, string Hint, string Icon);

/// <summary>
/// Use case «Оформить заказ»: delivery type — courier (standard / express), pickup or parcel locker —
/// and address or point (15.2), payment method (15.3), confirmation («Ожидает оплаты») and online payment (15.4).
/// </summary>
public sealed partial class CheckoutViewModel : ViewModelBase {
  private readonly ICartService _cart;
  private readonly IOrderService _orders;
  private readonly RemoteDataCache _cache;
  private readonly ObservableAsPropertyHelper<bool> _isToDoor;
  private readonly ObservableAsPropertyHelper<string> _pointsTitle;
  private Guid _orderId;

  public CheckoutViewModel(
    ICartService cart,
    IOrderService orders,
    RemoteDataCache cache,
    INavigationService navigation,
    IDialogService dialogs) : base(navigation, dialogs) {
    _cart = cart;
    _orders = orders;
    _cache = cache;
    SelectedDelivery = DeliveryOptions[0];

    var type = this.WhenAnyValue(x => x.SelectedDelivery).Select(x => x?.Type ?? DeliveryType.Standard);
    _isToDoor = type.Select(x => x.IsToDoor()).ToProperty(this, x => x.IsToDoor, initialValue: true);
    _pointsTitle = type.Select(x => x == DeliveryType.ParcelLocker ? "Постамат" : "Пункт самовывоза")
      .ToProperty(this, x => x.PointsTitle, initialValue: string.Empty);

    // Pickup / parcel locker: load the list of points for the chosen type.
    type.DistinctUntilChanged()
      .Where(x => !x.IsToDoor())
      .Subscribe(x => _ = LoadPointsAsync(x));
  }

  public IReadOnlyList<DeliveryOption> DeliveryOptions { get; } = [
    new(DeliveryType.Standard, "Курьером", "1–3 дня, до двери", Icons.Themed("hero_truck")),
    new(DeliveryType.Express, "Срочно курьером", "Сегодня или завтра", Icons.Themed("hero_truck")),
    new(DeliveryType.Pickup, "Самовывоз", "Из магазина или пункта выдачи", Icons.Themed("hero_building_storefront")),
    new(DeliveryType.ParcelLocker, "Постамат", "Круглосуточно, по коду из SMS", Icons.Themed("hero_archive_box"))
  ];

  public ObservableCollection<DeliveryPointDto> Points { get; } = [];

  [Reactive]
  public partial DeliveryOption? SelectedDelivery { get; set; }

  [Reactive]
  public partial DeliveryPointDto? SelectedPoint { get; set; }

  public bool IsToDoor => _isToDoor.Value;

  public string PointsTitle => _pointsTitle.Value;

  [Reactive]
  public partial string SummaryText { get; set; } = string.Empty;

  [Reactive]
  public partial string TotalText { get; set; } = string.Empty;

  private bool _isCard = true;

  public bool IsCard {
    get => _isCard;
    set {
      this.RaiseAndSetIfChanged(ref _isCard, value);
      this.RaisePropertyChanged(nameof(IsSbp));
      this.RaisePropertyChanged(nameof(PayButtonText));
    }
  }

  public bool IsSbp {
    get => !IsCard;
    set => IsCard = !value;
  }

  [Reactive]
  public partial string City { get; set; } = string.Empty;

  [Reactive]
  public partial string Street { get; set; } = string.Empty;

  [Reactive]
  public partial string House { get; set; } = string.Empty;

  [Reactive]
  public partial string Apartment { get; set; } = string.Empty;

  [Reactive]
  public partial string PostalCode { get; set; } = string.Empty;

  [Reactive]
  public partial string Comment { get; set; } = string.Empty;

  [Reactive]
  public partial bool IsPaymentStep { get; set; }

  [Reactive]
  public partial string OrderText { get; set; } = string.Empty;

  public string PayButtonText => IsCard ? "Оплатить картой" : "Оплатить через СБП";

  public override async Task OnAppearingAsync() {
    if (IsPaymentStep) {
      return;
    }

    var dto = await ExecuteAsync(() => _cart.GetAsync());
    if (dto is null) {
      return;
    }

    if (dto.IsEmpty) {
      await Dialogs.AlertAsync("Оформление заказа", "Корзина пуста.");
      await Navigation.GoBackAsync();
      return;
    }

    SummaryText = $"Товаров: {dto.TotalQuantity}";
    TotalText = DisplayText.Money(dto.TotalAmount);
  }

  [ReactiveCommand]
  private void SelectDelivery(DeliveryOption option) => SelectedDelivery = option;

  [ReactiveCommand]
  private async Task PlaceOrderAsync() {
    var type = SelectedDelivery?.Type ?? DeliveryType.Standard;
    if (!type.IsToDoor() && SelectedPoint is null) {
      await Dialogs.AlertAsync("Доставка", type == DeliveryType.Pickup ? "Выберите пункт самовывоза." : "Выберите постамат.");
      return;
    }

    var address = type.IsToDoor()
      ? new AddressDto(City.Trim(), Street.Trim(), House.Trim(), NullIfEmpty(Apartment), NullIfEmpty(PostalCode), NullIfEmpty(Comment))
      : SelectedPoint!.Address;

    var request = new CheckoutRequest(type, address, IsCard ? PaymentMethod.Card : PaymentMethod.Sbp, SelectedPoint?.Id);

    var order = await ExecuteAsync(() => _orders.CheckoutAsync(request));
    if (order is null) {
      return;
    }

    _cache.Invalidate();
    _orderId = order.Id;
    TotalText = DisplayText.Money(order.TotalAmount);
    OrderText = $"Заказ {DisplayText.ShortId(order.Id)} создан и ожидает оплаты."
      + (order.PaymentDeadline is { } deadline ? $" Оплатите до {deadline.ToLocalTime():HH:mm}, иначе заказ отменится." : string.Empty);
    IsPaymentStep = true;
  }

  [ReactiveCommand]
  private async Task PayAsync() {
    var order = await ExecuteAsync(() => _orders.PayAsync(_orderId));
    if (order is null) {
      return;
    }

    _cache.Invalidate();
    if (order.Status == OrderStatus.AwaitingPayment) {
      await Dialogs.AlertAsync("Оплата", "Платёж отклонён платёжной системой. Попробуйте ещё раз.");
      return;
    }

    var next = SelectedDelivery?.Type switch {
      DeliveryType.Pickup => "Мы сообщим, когда заказ будет готов к выдаче.",
      DeliveryType.ParcelLocker => "Код для постамата придёт, когда заказ доставят.",
      _ => "Мы сообщим, когда заказ передадут курьеру."
    };
    await Dialogs.AlertAsync("Заказ подтверждён", $"Оплата прошла. {next}");
    await OpenOrderAsync();
  }

  [ReactiveCommand]
  private Task PayLaterAsync() => OpenOrderAsync();

  private async Task LoadPointsAsync(DeliveryType type) {
    var points = await ExecuteAsync(() => _orders.GetDeliveryPointsAsync(type), showBusy: false);
    Points.Clear();
    foreach (var point in points ?? []) {
      Points.Add(point);
    }

    SelectedPoint = Points.FirstOrDefault();
  }

  private Task OpenOrderAsync() => Navigation.GoToAsync(
    $"//{Routes.Main}/{Routes.Orders}/{Routes.OrderDetails}",
    new Dictionary<string, object> { [Routes.Params.Id] = _orderId });

  private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
