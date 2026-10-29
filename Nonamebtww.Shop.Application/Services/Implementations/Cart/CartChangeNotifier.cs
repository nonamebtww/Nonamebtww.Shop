using Nonamebtww.Shop.Application.Models.DTOs.Cart;

namespace Nonamebtww.Shop.Application.Services.Implementations.Cart;

/// <summary>Singleton-шина изменений корзины: сервисы корзины транзиентны, а подписчики (UI) живут долго.</summary>
public sealed class CartChangeNotifier {
  public event EventHandler<CartDto>? Changed;

  internal void Publish(object sender, CartDto cart) => Changed?.Invoke(sender, cart);
}
