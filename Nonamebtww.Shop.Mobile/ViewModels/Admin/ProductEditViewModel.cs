using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>«Изменение информации о товаре»: description, characteristics, photos, price and stock.</summary>
public sealed partial class ProductEditViewModel(
  IProductQueries products,
  ICategoryQueries categories,
  ICatalogManagementService management,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private Guid? _productId;
  private bool _loaded;

  public ObservableCollection<Option<Guid>> CategoryOptions { get; } = [];

  [ObservableProperty]
  public partial Option<Guid>? SelectedCategory { get; set; }

  [ObservableProperty]
  public partial string Sku { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Name { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Description { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Characteristics { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Price { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Stock { get; set; } = "0";

  [ObservableProperty]
  public partial bool IsActive { get; set; } = true;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PhotoPreview))]
  public partial string? Photo { get; set; }

  public string PhotoPreview => ImageSources.Photo(Photo);

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    _productId = query.TryGetValue(Routes.Params.Id, out var id) && id is Guid productId ? productId : null;
    _loaded = false;
  }

  public override async Task OnAppearingAsync() {
    if (_loaded) {
      return;
    }

    _loaded = true;
    Title = _productId is null ? "Новый товар" : "Редактирование товара";

    var tree = await ExecuteAsync(() => categories.GetTreeAsync());
    CategoryOptions.Clear();
    foreach (var row in CategoryRow.Flatten(tree ?? [])) {
      CategoryOptions.Add(new Option<Guid>(row.Id, new string(' ', row.Level * 2) + row.Name));
    }

    if (_productId is not { } id) {
      SelectedCategory = CategoryOptions.FirstOrDefault();
      return;
    }

    var product = await ExecuteValueAsync(() => products.GetAsync(id));
    if (product is { } p) {
      Apply(p);
    }
  }

  [RelayCommand]
  private async Task PickPhotoAsync() {
    try {
      Photo = await PhotoPicker.PickAsync() ?? Photo;
    } catch (Exception ex) {
      await Dialogs.AlertAsync("Фото", ex.Message);
    }
  }

  [RelayCommand]
  private void RemovePhoto() => Photo = null;

  [RelayCommand]
  private async Task SaveAsync() {
    if (SelectedCategory is null) {
      await Dialogs.AlertAsync("Товар", "Выберите категорию.");
      return;
    }

    if (!decimal.TryParse(Price.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
        || !int.TryParse(Stock, out var stock)) {
      await Dialogs.AlertAsync("Товар", "Проверьте цену и остаток: нужны числа.");
      return;
    }

    var request = new SaveProductRequest(
      _productId,
      SelectedCategory.Value,
      Sku.Trim(),
      Name.Trim(),
      string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
      ParseCharacteristics(Characteristics),
      price,
      stock,
      Photo,
      IsActive);

    var saved = await ExecuteValueAsync(() => management.SaveProductAsync(request));
    if (saved is not null) {
      await Navigation.GoBackAsync();
    }
  }

  private void Apply(ProductDetailed product) {
    SelectedCategory = CategoryOptions.FirstOrDefault(x => x.Value == product.CategoryId);
    Sku = product.Sku;
    Name = product.Name;
    Description = product.Description ?? string.Empty;
    Characteristics = string.Join(Environment.NewLine, product.Characteristics.Select(x => $"{x.Key}: {x.Value}"));
    Price = product.Price.ToString(CultureInfo.InvariantCulture);
    Stock = product.Stock.ToString(CultureInfo.InvariantCulture);
    IsActive = product.IsActive;
    Photo = product.Photo;
  }

  private static Dictionary<string, string> ParseCharacteristics(string text) {
    var result = new Dictionary<string, string>();
    foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
      var separator = line.IndexOf(':');
      if (separator <= 0) {
        continue;
      }

      result[line[..separator].Trim()] = line[(separator + 1)..].Trim();
    }

    return result;
  }
}
