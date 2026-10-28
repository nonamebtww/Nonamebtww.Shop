namespace Nonamebtww.Shop.Application.Models.DTOs.Cart;

/// <summary>«Содержимое корзины».</summary>
public sealed record CartDto(IReadOnlyList<CartItemDto> Items, int TotalQuantity, decimal TotalAmount) {
  public bool IsEmpty => Items.Count == 0;
  public bool HasUnavailableItems => Items.Any(x => !x.IsAvailable);

  public static CartDto Empty { get; } = new([], 0, 0m);
}

public sealed record CartItemDto(
  Guid ProductId,
  string Name,
  string? PhotoUrl,
  decimal Price,
  int Quantity,
  int AvailableStock,
  bool IsActive,
  DateTimeOffset AddedAt) {
  public decimal LineTotal => Price * Quantity;
  public bool IsAvailable => IsActive && AvailableStock >= Quantity;
}
