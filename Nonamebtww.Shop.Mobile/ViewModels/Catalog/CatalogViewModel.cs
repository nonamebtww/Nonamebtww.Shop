using System.Collections.ObjectModel;
using System.Globalization;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Catalog;

/// <summary>Use case «Просмотр каталога»: categories, search, filters, sorting and paging.</summary>
public sealed partial class CatalogViewModel(
  ICategoryQueries categories,
  IProductQueries products,
  IFavoritesService favorites,
  ICartService cart,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private const int PageSize = 20;

  private bool _loaded;
  private int _page = 1;
  private bool _hasNextPage;
  private Guid? _categoryId;

  public ObservableCollection<CategoryChip> Categories { get; } = [];
  public ObservableCollection<CategoryChip> SubCategories { get; } = [];
  public ObservableCollection<ProductCardItem> Products { get; } = [];

  public IReadOnlyList<Option<ProductSort>> SortOptions { get; } =
    Enum.GetValues<ProductSort>().Select(x => new Option<ProductSort>(x, DisplayText.Of(x))).ToList();

  [Reactive]
  public partial string SearchText { get; set; } = string.Empty;

  [Reactive]
  public partial bool IsFiltersVisible { get; set; }

  [Reactive]
  public partial string MinPrice { get; set; } = string.Empty;

  [Reactive]
  public partial string MaxPrice { get; set; } = string.Empty;

  [Reactive]
  public partial bool OnlyInStock { get; set; }

  [Reactive]
  public partial Option<ProductSort>? SelectedSort { get; set; }

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  [Reactive]
  public partial bool HasSubCategories { get; set; }

  /// <summary>Search started from another screen (Profile title bar): «query» parameter.</summary>
  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    if (query.TryGetValue("query", out var value) && value is string text) {
      SearchText = text;
      if (_loaded) {
        _ = ReloadAsync();
      }
    }
  }

  public override async Task OnAppearingAsync() {
    if (_loaded) {
      return;
    }

    _loaded = true;
    SelectedSort = SortOptions[0];
    await LoadWithSkeletonAsync(async () => {
      await LoadCategoriesAsync();
      await LoadPageAsync();
    }, () => cache.Contains(CacheKeys.Categories) && cache.Contains(SearchKey(BuildSearch())));
  }

  [ReactiveCommand]
  private async Task RefreshAsync() {
    cache.Invalidate(CacheKeys.Catalog);
    await ReloadAsync(showSkeleton: false);
    IsRefreshing = false;
  }

  [ReactiveCommand]
  private Task SearchAsync() => ReloadAsync();

  [ReactiveCommand]
  private void ToggleFilters() => IsFiltersVisible = !IsFiltersVisible;

  [ReactiveCommand]
  private async Task ApplyFiltersAsync() {
    IsFiltersVisible = false;
    await ReloadAsync();
  }

  [ReactiveCommand]
  private async Task ResetFiltersAsync() {
    MinPrice = string.Empty;
    MaxPrice = string.Empty;
    OnlyInStock = false;
    SelectedSort = SortOptions[0];
    IsFiltersVisible = false;
    await ReloadAsync();
  }

  [ReactiveCommand]
  private async Task SelectCategoryAsync(CategoryChip chip) {
    foreach (var item in Categories) {
      item.IsSelected = item == chip;
    }

    SubCategories.Clear();
    foreach (var child in chip.Children) {
      SubCategories.Add(CategoryChip.From(child));
    }

    HasSubCategories = SubCategories.Count > 0;
    _categoryId = chip.Id;
    await ReloadAsync();
  }

  [ReactiveCommand]
  private async Task SelectSubCategoryAsync(CategoryChip chip) {
    var wasSelected = chip.IsSelected;
    foreach (var item in SubCategories) {
      item.IsSelected = false;
    }

    chip.IsSelected = !wasSelected;
    _categoryId = chip.IsSelected ? chip.Id : Categories.FirstOrDefault(x => x.IsSelected)?.Id;
    await ReloadAsync();
  }

  [ReactiveCommand]
  private async Task LoadMoreAsync() {
    if (!_hasNextPage || IsBusy || IsLoading) {
      return;
    }

    _page++;
    await LoadPageAsync();
  }

  [ReactiveCommand]
  private Task OpenProductAsync(ProductCardItem item) =>
    Navigation.GoToAsync(Routes.Product, new Dictionary<string, object> { [Routes.Params.Id] = item.Id });

  [ReactiveCommand]
  private async Task ToggleFavoriteAsync(ProductCardItem item) {
    var result = await favorites.ToggleAsync(item.Id);
    if (result.IsSuccess) {
      item.IsFavorite = result.Value;
      cache.Invalidate();
    } else {
      await ShowErrorAsync(result.Error);
    }
  }

  [ReactiveCommand]
  private async Task AddToCartAsync(ProductCardItem item) {
    var result = await ExecuteAsync(() => cart.AddAsync(item.Id), showBusy: false);
    if (result is not null) {
      cache.Invalidate();
      await Dialogs.AlertAsync("Корзина", $"«{item.Name}» добавлен в корзину.");
    }
  }

  private async Task LoadCategoriesAsync() {
    var tree = await ExecuteAsync(() => cache.GetAsync<IReadOnlyList<CategoryNode>>(CacheKeys.Categories, () => categories.GetTreeAsync()));
    Categories.Clear();
    var all = new CategoryChip(null, "Все", []) { IsSelected = true };
    Categories.Add(all);
    foreach (var node in tree ?? []) {
      Categories.Add(CategoryChip.From(node));
    }
  }

  private async Task ReloadAsync(bool showSkeleton = true) {
    _page = 1;
    var search = BuildSearch();
    if (showSkeleton && !cache.Contains(SearchKey(search))) {
      Products.Clear();
    }

    await LoadWithSkeletonAsync(() => LoadPageAsync(replace: true), () => !showSkeleton || cache.Contains(SearchKey(search)));
  }

  private async Task LoadPageAsync(bool replace = false) {
    var search = BuildSearch();
    var page = await ExecuteAsync(() => cache.GetAsync(SearchKey(search), () => products.SearchAsync(search)), showBusy: false);
    if (page is null) {
      _hasNextPage = false;
      return;
    }

    if (replace || _page == 1) {
      Products.Clear();
    }

    foreach (var product in page.Items) {
      Products.Add(new ProductCardItem(product));
    }

    _hasNextPage = page.HasNextPage;
  }

  private ProductSearch BuildSearch() => new(
    Query: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
    CategoryId: _categoryId,
    MinPrice: ParsePrice(MinPrice),
    MaxPrice: ParsePrice(MaxPrice),
    OnlyInStock: OnlyInStock,
    Sort: SelectedSort?.Value ?? ProductSort.Default,
    Page: _page,
    PageSize: PageSize);

  private static string SearchKey(ProductSearch search) => CacheKeys.Catalog + search;

  private static decimal? ParsePrice(string value) =>
    decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price >= 0
      ? price
      : null;
}
