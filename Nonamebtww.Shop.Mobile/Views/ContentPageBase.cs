using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.Views;

/// <summary>Base page: forwards OnAppearing to the ViewModel set as BindingContext.</summary>
public abstract class ContentPageBase : ContentPage {
  protected override async void OnAppearing() {
    base.OnAppearing();

    if (BindingContext is ViewModelBase viewModel) {
      await viewModel.OnAppearingAsync();
    }
  }
}
