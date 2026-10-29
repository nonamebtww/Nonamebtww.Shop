using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Catalog;

/// <summary>
/// Use case «Просмотр детальной информации о товаре»: photos, description, characteristics, reviews;
/// extends to «Добавить в избранное», «Добавить в корзину» / «Удалить из корзины» (with quantity) and «Поделиться».
/// </summary>
public sealed partial class ProductViewModel(
  IProductQueries products,
  IReviewService reviews,
  IFavoritesService favorites,
  ICartService cart,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private Guid _productId;
  private ProductDetailed _product;

  private ObservableAsPropertyHelper<string>? _cartButtonText;
  private ObservableAsPropertyHelper<bool>? _isInCart;
  private ObservableAsPropertyHelper<string>? _star1;
  private ObservableAsPropertyHelper<string>? _star2;
  private ObservableAsPropertyHelper<string>? _star3;
  private ObservableAsPropertyHelper<string>? _star4;
  private ObservableAsPropertyHelper<string>? _star5;

  public ObservableCollection<string> Photos { get; } = [];
  public ObservableCollection<SpecItem> Characteristics { get; } = [];
  public ObservableCollection<ReviewItem> Reviews { get; } = [];

  [Reactive]
  public partial bool IsLoaded { get; set; }

  [Reactive]
  public partial string Name { get; set; } = string.Empty;

  [Reactive]
  public partial string CategoryName { get; set; } = string.Empty;

  [Reactive]
  public partial string Description { get; set; } = string.Empty;

  [Reactive]
  public partial decimal Price { get; set; }

  [Reactive]
  public partial string PriceText { get; set; } = string.Empty;

  [Reactive]
  public partial string StockText { get; set; } = string.Empty;

  [Reactive]
  public partial string SkuText { get; set; } = string.Empty;

  [Reactive]
  public partial string RatingText { get; set; } = string.Empty;

  [Reactive]
  public partial string ReviewsTitle { get; set; } = "Отзывы";

  [Reactive]
  public partial bool InStock { get; set; }

  [Reactive]
  public partial int Stock { get; set; }

  [Reactive]
  public partial bool IsFavorite { get; set; }

  [Reactive]
  public partial bool CanReview { get; set; }

  [Reactive]
  public partial bool HasCharacteristics { get; set; }

  /// <summary>Quantity to add (when the product is not in the cart yet).</summary>
  [Reactive]
  public partial int Quantity { get; set; } = 1;

  /// <summary>Quantity already in the cart; the buy bar edits it directly.</summary>
  [Reactive]
  public partial int InCartQuantity { get; set; }

  [Reactive]
  public partial int ReviewRating { get; set; } = 5;

  [Reactive]
  public partial string ReviewText { get; set; } = string.Empty;

  public bool IsInCart => Derived(ref _isInCart, this.WhenAnyValue(x => x.InCartQuantity).Select(q => q > 0));

  public string CartButtonText => Derived(ref _cartButtonText, this.WhenAnyValue(
    x => x.InStock, x => x.Quantity, x => x.Price,
    (inStock, quantity, price) => inStock ? $"В корзину · {DisplayText.Money(price * quantity)}" : "Нет в наличии"));

  public string Star1 => Derived(ref _star1, StarGlyph(1));
  public string Star2 => Derived(ref _star2, StarGlyph(2));
  public string Star3 => Derived(ref _star3, StarGlyph(3));
  public string Star4 => Derived(ref _star4, StarGlyph(4));
  public string Star5 => Derived(ref _star5, StarGlyph(5));

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    // Guid from in-app navigation, or text from a deep link ("product?id=…").
    if (query.TryGetValue(Routes.Params.Id, out var id)
        && (id is Guid productId || Guid.TryParse(id as string, out productId))) {
      _productId = productId;
      IsLoaded = false;
    }
  }

  public override Task OnAppearingAsync() =>
    LoadWithSkeletonAsync(() => LoadAsync(), () => IsLoaded || cache.Contains(Key));

  [ReactiveCommand]
  private async Task IncreaseAsync() {
    if (IsInCart) {
      if (InCartQuantity >= Stock) {
        await ShowStockLimitAsync();
        return;
      }

      await ChangeCartQuantityAsync(InCartQuantity + 1);
      return;
    }

    if (Quantity >= Stock) {
      await ShowStockLimitAsync();
      return;
    }

    Quantity++;
  }

  [ReactiveCommand]
  private async Task DecreaseAsync() {
    if (IsInCart) {
      if (InCartQuantity <= 1) {
        await RemoveFromCartAsync();
      } else {
        await ChangeCartQuantityAsync(InCartQuantity - 1);
      }

      return;
    }

    if (Quantity > 1) {
      Quantity--;
    }
  }

  [ReactiveCommand]
  private async Task AddToCartAsync() {
    if (Quantity > Stock) {
      await ShowStockLimitAsync();
      return;
    }

    var result = await ExecuteAsync(() => cart.AddAsync(_productId, Quantity));
    if (result is null) {
      return;
    }

    cache.Invalidate();
    InCartQuantity = result.Items.FirstOrDefault(x => x.ProductId == _productId)?.Quantity ?? InCartQuantity + Quantity;
    Quantity = 1;
  }

  [ReactiveCommand]
  private async Task RemoveFromCartAsync() {
    if (!await Dialogs.ConfirmAsync("Корзина", $"Удалить «{Name}» из корзины?")) {
      return;
    }

    if (await ExecuteAsync(() => cart.RemoveAsync(_productId), showBusy: false) is not null) {
      cache.Invalidate();
      InCartQuantity = 0;
      Quantity = 1;
    }
  }

  [ReactiveCommand]
  private Task GoToCartAsync() => Navigation.GoToRootAsync($"{Routes.Main}/{Routes.Cart}");

  [ReactiveCommand]
  private async Task ToggleFavoriteAsync() {
    var result = await favorites.ToggleAsync(_productId);
    if (result.IsSuccess) {
      IsFavorite = result.Value;
      cache.Invalidate();
    } else {
      await ShowErrorAsync(result.Error);
    }
  }

  [ReactiveCommand]
  private Task ShareAsync() => Share.Default.RequestAsync(new ShareTextRequest {
    Title = Name,
    Text = $"{Name} — {PriceText}. Артикул {_product.Sku}, Nonamebtww Shop"
  });

  [ReactiveCommand]
  private void SetRating(string value) {
    if (int.TryParse(value, out var rating)) {
      ReviewRating = rating;
    }
  }

  [ReactiveCommand]
  private async Task SubmitReviewAsync() {
    var text = string.IsNullOrWhiteSpace(ReviewText) ? null : ReviewText.Trim();
    var review = await ExecuteAsync(() => reviews.AddAsync(new AddReviewRequest(_productId, ReviewRating, text)));
    if (review is null) {
      return;
    }

    ReviewText = string.Empty;
    ReviewRating = 5;
    cache.Invalidate();
    await LoadAsync(forceRefresh: true);
    await Dialogs.AlertAsync("Спасибо!", "Отзыв опубликован.");
  }

  private string Key => CacheKeys.Product + _productId;

  private async Task ChangeCartQuantityAsync(int quantity) {
    var result = await ExecuteAsync(() => cart.ChangeQuantityAsync(_productId, quantity), showBusy: false);
    if (result is not null) {
      cache.Invalidate();
      InCartQuantity = result.Items.FirstOrDefault(x => x.ProductId == _productId)?.Quantity ?? quantity;
    }
  }

  private Task ShowStockLimitAsync() =>
    Dialogs.AlertAsync("Количество", $"В наличии только {Stock} шт. Больше добавить нельзя.");

  private async Task LoadAsync(bool forceRefresh = false) {
    if (_productId == Guid.Empty) {
      return;
    }

    var result = await cache.GetAsync(Key, () => products.GetAsync(_productId), forceRefresh);
    if (result.IsFailure) {
      await ShowErrorAsync(result.Error);
      await Navigation.GoBackAsync();
      return;
    }

    Apply(result.Value);
  }

  private void Apply(ProductDetailed product) {
    _product = product;
    Title = product.Name;
    Name = product.Name;
    CategoryName = product.CategoryName;
    Description = string.IsNullOrWhiteSpace(product.Description) ? "Описание пока не заполнено." : product.Description;
    Price = product.Price;
    PriceText = DisplayText.Money(product.Price);
    Stock = product.Stock;
    InStock = product.InStock && product.IsActive;
    StockText = !product.IsActive ? "Снят с продажи" : product.InStock ? $"В наличии: {product.Stock} шт." : "Нет в наличии";
    SkuText = $"Артикул: {product.Sku}";
    RatingText = product.Reviews.Count == 0
      ? "Нет отзывов"
      : $"{DisplayText.Stars(product.Rating)} · {product.Reviews.Count} {DisplayText.Plural(product.Reviews.Count, "отзыв", "отзыва", "отзывов")}";
    ReviewsTitle = $"Отзывы ({product.Reviews.Count})";
    IsFavorite = product.IsFavorite;
    InCartQuantity = product.InCartQuantity;
    CanReview = product.CanReview;
    Quantity = 1;

    Photos.Clear();
    Photos.Add(ImageSources.Photo(product.Photo));

    Characteristics.Clear();
    foreach (var (name, value) in product.Characteristics) {
      Characteristics.Add(new SpecItem(name, value));
    }

    HasCharacteristics = Characteristics.Count > 0;

    Reviews.Clear();
    foreach (var review in product.Reviews.OrderByDescending(x => x.CreatedAt)) {
      Reviews.Add(ReviewItem.From(review));
    }

    IsLoaded = true;
  }

  private IObservable<string> StarGlyph(int position) =>
    this.WhenAnyValue(x => x.ReviewRating).Select(rating => position <= rating ? "★" : "☆");
}
