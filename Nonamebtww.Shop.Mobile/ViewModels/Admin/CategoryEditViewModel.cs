using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>«Изменение информации о категории»: name, description, photo, parent.</summary>
public sealed partial class CategoryEditViewModel(
  ICategoryQueries categories,
  ICatalogManagementService management,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs), IQueryAttributable {
  private static readonly Option<Guid?> NoParent = new(null, "Без родителя (корневая)");

  private Guid? _categoryId;
  private bool _loaded;

  public ObservableCollection<Option<Guid?>> ParentOptions { get; } = [];

  [ObservableProperty]
  public partial Option<Guid?>? SelectedParent { get; set; }

  [ObservableProperty]
  public partial string Name { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Description { get; set; } = string.Empty;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PhotoPreview))]
  public partial string? Photo { get; set; }

  [ObservableProperty]
  public partial bool IsExisting { get; set; }

  public string PhotoPreview => ImageSources.Photo(Photo);

  public void ApplyQueryAttributes(IDictionary<string, object> query) {
    _categoryId = query.TryGetValue(Routes.Params.Id, out var id) && id is Guid categoryId ? categoryId : null;
    _loaded = false;
  }

  public override async Task OnAppearingAsync() {
    if (_loaded) {
      return;
    }

    _loaded = true;
    IsExisting = _categoryId is not null;
    Title = IsExisting ? "Редактирование категории" : "Новая категория";

    var tree = await ExecuteAsync(() => categories.GetTreeAsync());
    ParentOptions.Clear();
    ParentOptions.Add(NoParent);
    foreach (var row in CategoryRow.Flatten(tree ?? []).Where(x => x.Id != _categoryId)) {
      ParentOptions.Add(new Option<Guid?>(row.Id, new string(' ', row.Level * 2) + row.Name));
    }

    SelectedParent = NoParent;
    if (_categoryId is not { } id) {
      return;
    }

    var category = await ExecuteValueAsync(() => categories.GetAsync(id));
    if (category is { } c) {
      Name = c.Name;
      Description = c.Description ?? string.Empty;
      Photo = c.Photo;
      SelectedParent = ParentOptions.FirstOrDefault(x => x.Value == c.ParentId) ?? NoParent;
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
    var request = new SaveCategoryRequest(
      _categoryId,
      SelectedParent?.Value,
      Name.Trim(),
      string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
      Photo);

    if (await ExecuteValueAsync(() => management.SaveCategoryAsync(request)) is not null) {
      await Navigation.GoBackAsync();
    }
  }

  [RelayCommand]
  private async Task DeleteAsync() {
    if (_categoryId is not { } id || !await Dialogs.ConfirmAsync("Категория", $"Удалить категорию «{Name}»? Удалить можно только пустую категорию.")) {
      return;
    }

    if (await ExecuteAsync(() => management.DeleteCategoryAsync(id))) {
      await Navigation.GoBackAsync();
    }
  }
}
