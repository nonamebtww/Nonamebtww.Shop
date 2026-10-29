using Microsoft.Maui.Controls.Shapes;

namespace Nonamebtww.Shop.Mobile.Controls;

/// <summary>Grey placeholder shape of a skeleton screen; the shimmer highlight slides over it.</summary>
public class SkeletonBlock : Border {
  public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
    nameof(CornerRadius), typeof(double), typeof(SkeletonBlock), 8d,
    propertyChanged: (b, _, v) => ((SkeletonBlock)b).StrokeShape = new RoundRectangle { CornerRadius = (double)v });

  public SkeletonBlock() {
    StrokeThickness = 0;
    StrokeShape = new RoundRectangle { CornerRadius = CornerRadius };
    HorizontalOptions = LayoutOptions.Fill;
    this.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E7E8EC"), Color.FromArgb("#2C2D33"));

    Highlight = new BoxView {
      HorizontalOptions = LayoutOptions.Start,
      VerticalOptions = LayoutOptions.Fill,
      InputTransparent = true,
      Background = new LinearGradientBrush {
        StartPoint = new Point(0, 0.5),
        EndPoint = new Point(1, 0.5),
        GradientStops = [
          new GradientStop(Colors.Transparent, 0f),
          new GradientStop(Color.FromRgba(255, 255, 255, Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? 0.10 : 0.65), 0.5f),
          new GradientStop(Colors.Transparent, 1f)
        ]
      }
    };

    Content = Highlight;
  }

  public double CornerRadius {
    get => (double)GetValue(CornerRadiusProperty);
    set => SetValue(CornerRadiusProperty, value);
  }

  internal BoxView Highlight { get; }

  /// <summary>Moves the highlight to <paramref name="phase"/> (0…1) of its sweep.</summary>
  internal void SetPhase(double phase) {
    var width = Width;
    if (width <= 0) {
      return;
    }

    var band = Math.Max(48, width * 0.6);
    Highlight.WidthRequest = band;
    Highlight.TranslationX = -band + phase * (width + band);
  }
}

/// <summary>
/// Container of a skeleton screen: runs one shimmer animation for all <see cref="SkeletonBlock"/> inside
/// while it is visible. With «Reduce Motion» on (iOS) the blocks stay still.
/// </summary>
public class SkeletonView : ContentView {
  private const string AnimationName = "Shimmer";
  private const uint PeriodMs = 1300;

  private readonly List<SkeletonBlock> _blocks = [];
  private bool _running;

  public SkeletonView() {
    SemanticProperties.SetDescription(this, "Загрузка…");
    Loaded += (_, _) => UpdateAnimation();
    Unloaded += (_, _) => Stop();
    PropertyChanged += (_, e) => {
      if (e.PropertyName == nameof(IsVisible)) {
        UpdateAnimation();
      }
    };
  }

  private static bool ReduceMotion =>
#if IOS || MACCATALYST
    UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
    false;
#endif

  private void UpdateAnimation() {
    if (IsVisible && IsLoaded && !ReduceMotion) {
      Start();
    } else {
      Stop();
    }
  }

  private void Start() {
    if (_running) {
      return;
    }

    _blocks.Clear();
    _blocks.AddRange(this.GetVisualTreeDescendants().OfType<SkeletonBlock>());
    if (_blocks.Count == 0) {
      return;
    }

    _running = true;
    // Phase comes from the clock, so every skeleton on screen shimmers in sync.
    new Animation(_ => {
      var phase = Environment.TickCount64 % PeriodMs / (double)PeriodMs;
      foreach (var block in _blocks) {
        block.SetPhase(phase);
      }
    }).Commit(this, AnimationName, rate: 16, length: PeriodMs, easing: Easing.Linear, repeat: () => _running);
  }

  private void Stop() {
    _running = false;
    this.AbortAnimation(AnimationName);
  }
}

/// <summary>Skeleton of the catalog: category chips and a two-column product grid.</summary>
public sealed class ProductGridSkeleton : SkeletonView {
  public ProductGridSkeleton() {
    var chips = new HorizontalStackLayout { Spacing = 8 };
    foreach (var width in new[] { 56d, 110, 70, 96 }) {
      chips.Add(new SkeletonBlock { WidthRequest = width, HeightRequest = 32, CornerRadius = 16 });
    }

    var grid = new Grid {
      ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
      ColumnSpacing = 10,
      RowSpacing = 10
    };

    for (var i = 0; i < 6; i++) {
      grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
      var card = new VerticalStackLayout {
        Spacing = 8,
        Children = {
          new SkeletonBlock { HeightRequest = 130, CornerRadius = 12 },
          new SkeletonBlock { HeightRequest = 14 },
          new SkeletonBlock { HeightRequest = 14, WidthRequest = 90, HorizontalOptions = LayoutOptions.Start },
          new SkeletonBlock { HeightRequest = 18, WidthRequest = 70, HorizontalOptions = LayoutOptions.Start },
          new SkeletonBlock { HeightRequest = 36, CornerRadius = 18 }
        }
      };
      grid.Add(card, i % 2, i / 2);
    }

    Content = new VerticalStackLayout { Spacing = 14, Children = { chips, grid } };
  }
}

/// <summary>Skeleton of a vertical list of cards with an optional thumbnail (orders, users, catalog management).</summary>
public sealed class ListSkeleton : SkeletonView {
  public static readonly BindableProperty RowsProperty = BindableProperty.Create(
    nameof(Rows), typeof(int), typeof(ListSkeleton), 6, propertyChanged: (b, _, _) => ((ListSkeleton)b).Build());

  public static readonly BindableProperty ThumbnailSizeProperty = BindableProperty.Create(
    nameof(ThumbnailSize), typeof(double), typeof(ListSkeleton), 0d, propertyChanged: (b, _, _) => ((ListSkeleton)b).Build());

  public ListSkeleton() => Build();

  public int Rows {
    get => (int)GetValue(RowsProperty);
    set => SetValue(RowsProperty, value);
  }

  /// <summary>0 — rows without a thumbnail.</summary>
  public double ThumbnailSize {
    get => (double)GetValue(ThumbnailSizeProperty);
    set => SetValue(ThumbnailSizeProperty, value);
  }

  private void Build() {
    var list = new VerticalStackLayout { Spacing = 10 };
    for (var i = 0; i < Rows; i++) {
      var row = new Grid {
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
        ColumnSpacing = 12,
        Padding = 12
      };

      if (ThumbnailSize > 0) {
        row.Add(new SkeletonBlock { WidthRequest = ThumbnailSize, HeightRequest = ThumbnailSize, CornerRadius = 10 }, 0);
      }

      row.Add(new VerticalStackLayout {
        Spacing = 8,
        VerticalOptions = LayoutOptions.Center,
        Children = {
          new SkeletonBlock { HeightRequest = 15 },
          new SkeletonBlock { HeightRequest = 12, WidthRequest = 160, HorizontalOptions = LayoutOptions.Start },
          new SkeletonBlock { HeightRequest = 12, WidthRequest = 100, HorizontalOptions = LayoutOptions.Start }
        }
      }, 1);

      list.Add(new Border {
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 14 },
        Content = row
      }.WithCardBackground());
    }

    Content = list;
  }
}

/// <summary>Skeleton of the product card: photo, title, rating, price, text.</summary>
public sealed class ProductDetailsSkeleton : SkeletonView {
  public ProductDetailsSkeleton() {
    Content = new VerticalStackLayout {
      Spacing = 10,
      Children = {
        new SkeletonBlock { HeightRequest = 300, CornerRadius = 0 },
        new VerticalStackLayout {
          Padding = new Thickness(16, 6),
          Spacing = 10,
          Children = {
            new SkeletonBlock { HeightRequest = 12, WidthRequest = 90, HorizontalOptions = LayoutOptions.Start },
            new SkeletonBlock { HeightRequest = 24 },
            new SkeletonBlock { HeightRequest = 14, WidthRequest = 140, HorizontalOptions = LayoutOptions.Start },
            new SkeletonBlock { HeightRequest = 26, WidthRequest = 120, HorizontalOptions = LayoutOptions.Start },
            new SkeletonBlock { HeightRequest = 18, WidthRequest = 110, HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(0, 12, 0, 0) },
            new SkeletonBlock { HeightRequest = 12 },
            new SkeletonBlock { HeightRequest = 12 },
            new SkeletonBlock { HeightRequest = 12, WidthRequest = 220, HorizontalOptions = LayoutOptions.Start }
          }
        }
      }
    };
  }
}

/// <summary>Skeleton of the profile header and menu.</summary>
public sealed class ProfileSkeleton : SkeletonView {
  public ProfileSkeleton() {
    var menu = new VerticalStackLayout { Spacing = 1 };
    for (var i = 0; i < 4; i++) {
      menu.Add(new SkeletonBlock { HeightRequest = 48, CornerRadius = 10 });
    }

    Content = new VerticalStackLayout {
      Spacing = 12,
      Children = {
        new Grid {
          ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
          ColumnSpacing = 14,
          Children = {
            new SkeletonBlock { WidthRequest = 64, HeightRequest = 64, CornerRadius = 32 },
            new VerticalStackLayout {
              Spacing = 8,
              VerticalOptions = LayoutOptions.Center,
              Children = {
                new SkeletonBlock { HeightRequest = 20 },
                new SkeletonBlock { HeightRequest = 12, WidthRequest = 120, HorizontalOptions = LayoutOptions.Start }
              }
            }.Column(1)
          }
        },
        menu
      }
    };
  }
}

/// <summary>Skeleton of a form (personal data).</summary>
public sealed class FormSkeleton : SkeletonView {
  public FormSkeleton() {
    var form = new VerticalStackLayout { Spacing = 12 };
    for (var i = 0; i < 4; i++) {
      form.Add(new SkeletonBlock { HeightRequest = 12, WidthRequest = 80, HorizontalOptions = LayoutOptions.Start });
      form.Add(new SkeletonBlock { HeightRequest = 44, CornerRadius = 10 });
    }

    form.Add(new SkeletonBlock { HeightRequest = 48, CornerRadius = 24, Margin = new Thickness(0, 8, 0, 0) });
    Content = form;
  }
}

internal static class SkeletonExtensions {
  public static Border WithCardBackground(this Border border) {
    border.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Color.FromArgb("#1E1F23"));
    return border;
  }

  public static T Column<T>(this T view, int column) where T : BindableObject {
    Grid.SetColumn(view, column);
    return view;
  }
}
