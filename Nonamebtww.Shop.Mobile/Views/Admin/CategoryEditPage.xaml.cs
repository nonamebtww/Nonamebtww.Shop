using Nonamebtww.Shop.Mobile.ViewModels.Admin;

namespace Nonamebtww.Shop.Mobile.Views.Admin;

public partial class CategoryEditPage : ContentPageBase {
  public CategoryEditPage(CategoryEditViewModel viewModel) {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
