using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>Use case «Управление каталогом» (Менеджер по продажам, Владелец): lists of products and categories.</summary>
public sealed partial class CatalogManagementViewModel(
  IProductQueries products,
  ICategoryQueries categories,
  ICatalogManagementService management,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<ManagedProductItem> Products { get; } = [];
  public ObservableCollection<CategoryRow> Categories { get; } = [];

  [Reactive]
  public partial bool IsProductsTab { get; set; } = true;

  [Reactive]
  public partial string SearchText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => LoadAsync();

  [ReactiveCommand]
  private async Task RefreshAsync() {
    cache.Invalidate(CacheKeys.ManagedCatalog);
    await LoadAsync(showSkeleton: false);
    IsRefreshing = false;
  }

  [Reactive]
  public partial bool IsRefreshing { get; set; }

  [ReactiveCommand]
  private Task ShowProductsAsync() {
    IsProductsTab = true;
    return LoadAsync();
  }

  [ReactiveCommand]
  private Task ShowCategoriesAsync() {
    IsProductsTab = false;
    return LoadAsync();
  }

  [ReactiveCommand]
  private Task SearchAsync() => LoadAsync();

  [ReactiveCommand]
  private Task AddAsync() => Navigation.GoToAsync(IsProductsTab ? Routes.AdminProductEdit : Routes.AdminCategoryEdit);

  [ReactiveCommand]
  private Task EditProductAsync(ManagedProductItem item) =>
    Navigation.GoToAsync(Routes.AdminProductEdit, new Dictionary<string, object> { [Routes.Params.Id] = item.Id });

  [ReactiveCommand]
  private Task EditCategoryAsync(CategoryRow row) =>
    Navigation.GoToAsync(Routes.AdminCategoryEdit, new Dictionary<string, object> { [Routes.Params.Id] = row.Id });

  [ReactiveCommand]
  private async Task ToggleActiveAsync(ManagedProductItem item) {
    // Taking a product off sale hides it from buyers: ask first.
    if (item.IsActive && !await Dialogs.ConfirmAsync(
          "Снять с продажи?",
          $"«{item.Name}» пропадёт из каталога у покупателей и его нельзя будет заказать. Вернуть товар в продажу можно в любой момент.",
          "Снять с продажи",
          "Отмена")) {
      return;
    }

    if (await ExecuteValueAsync(() => management.SetProductActiveAsync(item.Id, !item.IsActive)) is not null) {
      cache.Invalidate();
      await LoadAsync(showSkeleton: false);
    }
  }

  [ReactiveCommand]
  private async Task ChangeStockAsync(ManagedProductItem item) {
    var value = await Dialogs.PromptAsync("Остаток", $"Новый остаток для «{item.Name}», шт.", item.Stock.ToString(), Keyboard.Numeric);
    if (value is null) {
      return;
    }

    if (!int.TryParse(value, out var stock) || stock < 0) {
      await Dialogs.AlertAsync("Остаток", "Введите целое неотрицательное число.");
      return;
    }

    if (await ExecuteValueAsync(() => management.SetStockAsync(item.Id, stock)) is not null) {
      cache.Invalidate();
      await LoadAsync(showSkeleton: false);
    }
  }

  private Task LoadAsync(bool showSkeleton = true) {
    if (IsProductsTab) {
      var query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
      var key = CacheKeys.ManagedCatalog + "products:" + query;
      return LoadWithSkeletonAsync(async () => {
        var page = await ExecuteAsync(() => cache.GetAsync<PagedList<ProductSimple>>(key,
          () => products.SearchAsync(new ProductSearch(Query: query, PageSize: 200, IncludeInactive: true))), showBusy: false);
        if (page is null) {
          return;
        }

        Products.Clear();
        foreach (var product in page.Items) {
          Products.Add(ManagedProductItem.From(product));
        }
      }, () => !showSkeleton || cache.Contains(key));
    }

    var categoriesKey = CacheKeys.ManagedCatalog + "categories";
    return LoadWithSkeletonAsync(async () => {
      var tree = await ExecuteAsync(() => cache.GetAsync<IReadOnlyList<CategoryNode>>(categoriesKey, () => categories.GetTreeAsync()), showBusy: false);
      if (tree is null) {
        return;
      }

      Categories.Clear();
      foreach (var row in CategoryRow.Flatten(tree)) {
        Categories.Add(row);
      }
    }, () => !showSkeleton || cache.Contains(categoriesKey));
  }
}
