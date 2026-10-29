using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Catalog;

/// <summary>
/// Use case «Просмотр детальной информации о товаре»: photos, description, characteristics, reviews;
/// extends to «Добавить в избранное», «Добавить в корзину» (with quantity) and «Поделиться».
/// </summary>
public sealed partial class ProductViewModel(
  IProductQueries products,
  IReviewService reviews,
  IFavoritesService favorites,
  ICartService cart,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private Guid _productId;
  private ProductDetailed _product;

  public ObservableCollection<string> Photos { get; } = [];
  public ObservableCollection<SpecItem> Characteristics { get; } = [];
  public ObservableCollection<ReviewItem> Reviews { get; } = [];

  [ObservableProperty]
  public partial bool IsLoaded { get; set; }

  [ObservableProperty]
  public partial string Name { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string CategoryName { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Description { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string PriceText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string StockText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string SkuText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string RatingText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string ReviewsTitle { get; set; } = "Отзывы";

  [ObservableProperty]
  public partial bool InStock { get; set; }

  [ObservableProperty]
  public partial bool IsFavorite { get; set; }

  [ObservableProperty]
  public partial bool CanReview { get; set; }

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CartButtonText))]
  public partial int Quantity { get; set; } = 1;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CartButtonText))]
  public partial int InCartQuantity { get; set; }

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(Star1), nameof(Star2), nameof(Star3), nameof(Star4), nameof(Star5))]
  public partial int ReviewRating { get; set; } = 5;

  [ObservableProperty]
  public partial string ReviewText { get; set; } = string.Empty;

  public string CartButtonText => !InStock
    ? "Нет в наличии"
    : InCartQuantity > 0
      ? $"В корзину ещё {Quantity} (уже {InCartQuantity})"
      : $"В корзину · {DisplayText.Money(_product.Price * Quantity)}";

  public string Star1 => StarGlyph(1);
  public string Star2 => StarGlyph(2);
  public string Star3 => StarGlyph(3);
  public string Star4 => StarGlyph(4);
  public string Star5 => StarGlyph(5);

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    if (query.TryGetValue(Routes.Params.Id, out var id) && id is Guid productId) {
      _productId = productId;
      IsLoaded = false;
    }
  }

  public override Task OnAppearingAsync() => LoadAsync();

  [RelayCommand]
  private void Increase() {
    if (Quantity < Math.Max(1, _product.Stock - InCartQuantity)) {
      Quantity++;
    }
  }

  [RelayCommand]
  private void Decrease() {
    if (Quantity > 1) {
      Quantity--;
    }
  }

  [RelayCommand]
  private async Task AddToCartAsync() {
    var result = await ExecuteAsync(() => cart.AddAsync(_productId, Quantity));
    if (result is null) {
      return;
    }

    InCartQuantity = result.Items.FirstOrDefault(x => x.ProductId == _productId)?.Quantity ?? InCartQuantity + Quantity;
    Quantity = 1;
    await Dialogs.AlertAsync("Корзина", $"«{Name}» в корзине: {InCartQuantity} шт.");
  }

  [RelayCommand]
  private async Task ToggleFavoriteAsync() {
    var result = await favorites.ToggleAsync(_productId);
    if (result.IsSuccess) {
      IsFavorite = result.Value;
    } else {
      await ShowErrorAsync(result.Error);
    }
  }

  [RelayCommand]
  private Task ShareAsync() => Share.Default.RequestAsync(new ShareTextRequest {
    Title = Name,
    Text = $"{Name} — {PriceText}. Артикул {_product.Sku}, Nonamebtww Shop"
  });

  [RelayCommand]
  private void SetRating(string value) {
    if (int.TryParse(value, out var rating)) {
      ReviewRating = rating;
    }
  }

  [RelayCommand]
  private async Task SubmitReviewAsync() {
    var text = string.IsNullOrWhiteSpace(ReviewText) ? null : ReviewText.Trim();
    var review = await ExecuteAsync(() => reviews.AddAsync(new AddReviewRequest(_productId, ReviewRating, text)));
    if (review is null) {
      return;
    }

    ReviewText = string.Empty;
    await Dialogs.AlertAsync("Спасибо!", "Отзыв опубликован.");
    await LoadAsync();
  }

  private async Task LoadAsync() {
    if (_productId == Guid.Empty) {
      return;
    }

    var result = await products.GetAsync(_productId);
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
    PriceText = DisplayText.Money(product.Price);
    InStock = product.InStock && product.IsActive;
    StockText = !product.IsActive ? "Снят с продажи" : product.InStock ? $"В наличии: {product.Stock} шт." : "Нет в наличии";
    SkuText = $"Артикул: {product.Sku}";
    RatingText = $"{DisplayText.Stars(product.Rating)} · {product.Reviews.Count} отзывов";
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

    Reviews.Clear();
    foreach (var review in product.Reviews.OrderByDescending(x => x.CreatedAt)) {
      Reviews.Add(ReviewItem.From(review));
    }

    OnPropertyChanged(nameof(CartButtonText));
    IsLoaded = true;
  }

  private string StarGlyph(int position) => position <= ReviewRating ? "★" : "☆";
}
