using System.Globalization;

namespace Nonamebtww.Shop.Mobile.Converters;

public sealed class InvertedBoolConverter : IValueConverter {
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>True when the value is not null and, for strings and collections, not empty.</summary>
public sealed class IsNotNullOrEmptyConverter : IValueConverter {
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch {
    null => false,
    string s => !string.IsNullOrWhiteSpace(s),
    System.Collections.ICollection c => c.Count > 0,
    _ => true
  };

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}

/// <summary>
/// HeroIcons heart: outline when the product is not in favorites, solid when it is.
/// Black in light appearance, white in dark appearance.
/// </summary>
public sealed class FavoriteIconConverter : IValueConverter {
  /// <summary>ConverterParameter "black" keeps the black icon in both appearances (heart on a white disc).</summary>
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
    var name = value is true ? "hero_heart_solid" : "hero_heart";
    return parameter is "black" ? $"{name}.png" : Icons.Themed(name);
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}

/// <summary>Icon name from ConverterParameter (e.g. "hero_trash") resolved for the current appearance.</summary>
public sealed class ThemedIconConverter : IValueConverter {
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    Icons.Themed(parameter as string ?? value as string ?? string.Empty);

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}

public static class Icons {
  /// <summary>"hero_x" → "hero_x.png" (black) or "hero_x_dark.png" (white) for the dark appearance.</summary>
  public static string Themed(string name) =>
    Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? $"{name}_dark.png" : $"{name}.png";
}

/// <summary>true → 1, false → 0.35: a control that still reacts (to explain why) but looks unavailable.</summary>
public sealed class EnabledOpacityConverter : IValueConverter {
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? 1d : 0.35d;
  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
