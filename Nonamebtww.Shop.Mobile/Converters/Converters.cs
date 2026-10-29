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

/// <summary>Heart icon file for the favorite state (glyphs are missing in the bundled font).</summary>
public sealed class FavoriteIconConverter : IValueConverter {
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    value is true ? "heart_filled.png" : "heart_outline.png";
  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
