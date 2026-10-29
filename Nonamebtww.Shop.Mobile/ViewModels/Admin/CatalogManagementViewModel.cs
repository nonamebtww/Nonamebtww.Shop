using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
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
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<ManagedProductItem> Products { get; } = [];
  public ObservableCollection<CategoryRow> Categories { get; } = [];

  [ObservableProperty]
  public partial bool IsProductsTab { get; set; } = true;

  [ObservableProperty]
  public partial string SearchText { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => LoadAsync();

  [RelayCommand]
  private Task ShowProductsAsync() {
    IsProductsTab = true;
    return LoadAsync();
  }

  [RelayCommand]
  private Task ShowCategoriesAsync() {
    IsProductsTab = false;
    return LoadAsync();
  }

  [RelayCommand]
  private Task SearchAsync() => LoadAsync();

  [RelayCommand]
  private Task AddAsync() => Navigation.GoToAsync(IsProductsTab ? Routes.AdminProductEdit : Routes.AdminCategoryEdit);

  [RelayCommand]
  private Task EditProductAsync(ManagedProductItem item) =>
    Navigation.GoToAsync(Routes.AdminProductEdit, new Dictionary<string, object> { [Routes.Params.Id] = item.Id });

  [RelayCommand]
  private Task EditCategoryAsync(CategoryRow row) =>
    Navigation.GoToAsync(Routes.AdminCategoryEdit, new Dictionary<string, object> { [Routes.Params.Id] = row.Id });

  [RelayCommand]
  private async Task ToggleActiveAsync(ManagedProductItem item) {
    if (await ExecuteValueAsync(() => management.SetProductActiveAsync(item.Id, !item.IsActive)) is not null) {
      await LoadAsync();
    }
  }

  [RelayCommand]
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
      await LoadAsync();
    }
  }

  private async Task LoadAsync() {
    if (IsProductsTab) {
      var query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
      var page = await ExecuteAsync(() => products.SearchAsync(new ProductSearch(Query: query, PageSize: 200, IncludeInactive: true)));
      if (page is null) {
        return;
      }

      Products.Clear();
      foreach (var product in page.Items) {
        Products.Add(ManagedProductItem.From(product));
      }

      return;
    }

    var tree = await ExecuteAsync(() => categories.GetTreeAsync());
    if (tree is null) {
      return;
    }

    Categories.Clear();
    foreach (var row in CategoryRow.Flatten(tree)) {
      Categories.Add(row);
    }
  }
}
