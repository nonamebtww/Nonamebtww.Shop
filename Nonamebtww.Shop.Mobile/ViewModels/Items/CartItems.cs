using Nonamebtww.Shop.Application.Models.DTOs.Cart;
using Nonamebtww.Shop.Mobile.Presentation;

namespace Nonamebtww.Shop.Mobile.ViewModels.Items;

public sealed record CartLineItem(Guid ProductId, string Name, string Photo, int Quantity, string PriceText, string LineTotalText, bool IsAvailable, string? Warning) {
  public static CartLineItem From(CartItemDto item) => new(
    item.ProductId,
    item.Name,
    ImageSources.Photo(item.PhotoUrl),
    item.Quantity,
    DisplayText.Money(item.Price) + " / шт.",
    DisplayText.Money(item.LineTotal),
    item.IsAvailable,
    !item.IsActive
      ? "Товар снят с продажи"
      : item.AvailableStock < item.Quantity
        ? $"Доступно только {item.AvailableStock} шт."
        : null);
}
