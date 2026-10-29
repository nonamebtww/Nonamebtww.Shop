using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Nonamebtww.Shop.Mobile.Controls;

/// <summary>
/// Search field for the navigation bar (Shell.TitleView): flat rounded system-gray fill, magnifier,
/// borderless entry. The native SearchBar draws a background and an outer shadow that the title view
/// box clips; this field has no shadow, like the iOS search field.
/// </summary>
public sealed class SearchField : Border {
  public static readonly BindableProperty TextProperty = BindableProperty.Create(
    nameof(Text), typeof(string), typeof(SearchField), string.Empty, BindingMode.TwoWay);

  public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
    nameof(Placeholder), typeof(string), typeof(SearchField), string.Empty,
    propertyChanged: (b, _, v) => ((SearchField)b).OnPlaceholderChanged((string)v));

  public static readonly BindableProperty SearchCommandProperty = BindableProperty.Create(
    nameof(SearchCommand), typeof(ICommand), typeof(SearchField));

  private readonly Entry _entry;

  public SearchField() {
    StrokeThickness = 0;
    StrokeShape = new RoundRectangle { CornerRadius = 10 };
    HeightRequest = 36;
    Padding = new Thickness(10, 0, 4, 0);
    VerticalOptions = LayoutOptions.Center;
    // iOS systemFill-like grey (light) and tertiary fill (dark).
    this.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#EEEFF1"), Color.FromArgb("#2C2C2E"));

    var icon = new Image {
      WidthRequest = 18,
      HeightRequest = 18,
      Opacity = 0.55,
      VerticalOptions = LayoutOptions.Center,
      InputTransparent = true
    };
    icon.SetAppTheme(Image.SourceProperty, "hero_magnifying_glass.png", "hero_magnifying_glass_dark.png");

    _entry = new Entry {
      FontSize = 16,
      BackgroundColor = Colors.Transparent,
      ReturnType = ReturnType.Search,
      ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
      IsSpellCheckEnabled = false,
      VerticalOptions = LayoutOptions.Center
    };
    _entry.SetBinding(Entry.TextProperty, static (SearchField f) => f.Text, BindingMode.TwoWay, source: this);
    _entry.SetBinding(Entry.ReturnCommandProperty, static (SearchField f) => f.SearchCommand, source: this);
    _entry.SetAppThemeColor(Entry.PlaceholderColorProperty, Color.FromArgb("#8A8A8E"), Color.FromArgb("#8D8D93"));
    _entry.SetAppThemeColor(Entry.TextColorProperty, Color.FromArgb("#141414"), Colors.White);

    var grid = new Grid {
      ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
      ColumnSpacing = 6
    };
    grid.Add(icon, 0);
    grid.Add(_entry, 1);
    Content = grid;
  }

  public string Text {
    get => (string)GetValue(TextProperty);
    set => SetValue(TextProperty, value);
  }

  public string Placeholder {
    get => (string)GetValue(PlaceholderProperty);
    set => SetValue(PlaceholderProperty, value);
  }

  public ICommand? SearchCommand {
    get => (ICommand?)GetValue(SearchCommandProperty);
    set => SetValue(SearchCommandProperty, value);
  }

  private void OnPlaceholderChanged(string placeholder) {
    _entry.Placeholder = placeholder;
    SemanticProperties.SetDescription(_entry, placeholder);
  }
}
