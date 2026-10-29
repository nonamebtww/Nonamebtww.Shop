using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Cart;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Favorites;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Catalog;

/// <summary>Use case «Просмотр каталога»: categories, search, filters, sorting and paging.</summary>
public sealed partial class CatalogViewModel(
  ICategoryQueries categories,
  IProductQueries products,
  IFavoritesService favorites,
  ICartService cart,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
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

  [ObservableProperty]
  public partial string SearchText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsFiltersVisible { get; set; }

  [ObservableProperty]
  public partial string MinPrice { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string MaxPrice { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool OnlyInStock { get; set; }

  [ObservableProperty]
  public partial Option<ProductSort>? SelectedSort { get; set; }

  [ObservableProperty]
  public partial bool IsRefreshing { get; set; }

  [ObservableProperty]
  public partial bool HasSubCategories { get; set; }

  public override async Task OnAppearingAsync() {
    if (_loaded) {
      return;
    }

    _loaded = true;
    SelectedSort = SortOptions[0];
    await LoadCategoriesAsync();
    await ReloadAsync();
  }

  [RelayCommand]
  private async Task RefreshAsync() {
    await ReloadAsync();
    IsRefreshing = false;
  }

  [RelayCommand]
  private Task SearchAsync() => ReloadAsync();

  [RelayCommand]
  private void ToggleFilters() => IsFiltersVisible = !IsFiltersVisible;

  [RelayCommand]
  private async Task ApplyFiltersAsync() {
    IsFiltersVisible = false;
    await ReloadAsync();
  }

  [RelayCommand]
  private async Task ResetFiltersAsync() {
    MinPrice = string.Empty;
    MaxPrice = string.Empty;
    OnlyInStock = false;
    SelectedSort = SortOptions[0];
    IsFiltersVisible = false;
    await ReloadAsync();
  }

  [RelayCommand]
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

  [RelayCommand]
  private async Task SelectSubCategoryAsync(CategoryChip chip) {
    var wasSelected = chip.IsSelected;
    foreach (var item in SubCategories) {
      item.IsSelected = false;
    }

    chip.IsSelected = !wasSelected;
    _categoryId = chip.IsSelected ? chip.Id : Categories.FirstOrDefault(x => x.IsSelected)?.Id;
    await ReloadAsync();
  }

  [RelayCommand]
  private async Task LoadMoreAsync() {
    if (!_hasNextPage || IsBusy) {
      return;
    }

    _page++;
    await LoadPageAsync();
  }

  [RelayCommand]
  private Task OpenProductAsync(ProductCardItem item) =>
    Navigation.GoToAsync(Routes.Product, new Dictionary<string, object> { [Routes.Params.Id] = item.Id });

  [RelayCommand]
  private async Task ToggleFavoriteAsync(ProductCardItem item) {
    var result = await favorites.ToggleAsync(item.Id);
    if (result.IsSuccess) {
      item.IsFavorite = result.Value;
    } else {
      await ShowErrorAsync(result.Error);
    }
  }

  [RelayCommand]
  private async Task AddToCartAsync(ProductCardItem item) {
    var result = await ExecuteAsync(() => cart.AddAsync(item.Id), showBusy: false);
    if (result is not null) {
      await Dialogs.AlertAsync("Корзина", $"«{item.Name}» добавлен в корзину.");
    }
  }

  private async Task LoadCategoriesAsync() {
    var tree = await ExecuteAsync(() => categories.GetTreeAsync());
    Categories.Clear();
    var all = new CategoryChip(null, "Все", []) { IsSelected = true };
    Categories.Add(all);
    foreach (var node in tree ?? []) {
      Categories.Add(CategoryChip.From(node));
    }
  }

  private async Task ReloadAsync() {
    _page = 1;
    Products.Clear();
    await LoadPageAsync();
  }

  private async Task LoadPageAsync() {
    var search = new ProductSearch(
      Query: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
      CategoryId: _categoryId,
      MinPrice: ParsePrice(MinPrice),
      MaxPrice: ParsePrice(MaxPrice),
      OnlyInStock: OnlyInStock,
      Sort: SelectedSort?.Value ?? ProductSort.Default,
      Page: _page,
      PageSize: PageSize);

    var page = await ExecuteAsync(() => products.SearchAsync(search));
    if (page is null) {
      _hasNextPage = false;
      return;
    }

    foreach (var product in page.Items) {
      Products.Add(new ProductCardItem(product));
    }

    _hasNextPage = page.HasNextPage;
  }

  private static decimal? ParsePrice(string value) =>
    decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price >= 0
      ? price
      : null;
}
