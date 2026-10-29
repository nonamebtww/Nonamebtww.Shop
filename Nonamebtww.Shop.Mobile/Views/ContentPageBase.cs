using Nonamebtww.Shop.Mobile.ViewModels.Base;
using ReactiveUI.Maui;

namespace Nonamebtww.Shop.Mobile.Views;

/// <summary>
/// Base page (ReactiveUI): typed <see cref="ReactiveContentPage{TViewModel}.ViewModel"/>
/// (kept in sync with BindingContext) and OnAppearing forwarded to the ViewModel.
/// </summary>
public abstract class ContentPageBase<TViewModel> : ReactiveContentPage<TViewModel>
  where TViewModel : ViewModelBase {
  protected override async void OnAppearing() {
    base.OnAppearing();

    if (ViewModel is { } viewModel) {
      await viewModel.OnAppearingAsync();
    }
  }
}
