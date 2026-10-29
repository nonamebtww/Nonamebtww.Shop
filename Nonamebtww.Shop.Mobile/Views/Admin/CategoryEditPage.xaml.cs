using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class CategoryEditPage : ContentPageBase<CategoryEditViewModel> {
  public CategoryEditPage(CategoryEditViewModel viewModel) {
    InitializeComponent();
    ViewModel = viewModel;
  }
}
