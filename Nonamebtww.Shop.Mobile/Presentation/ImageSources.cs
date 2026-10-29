namespace Nonamebtww.Shop.Mobile.Presentation;

public static class ImageSources {
  public const string Placeholder = "product_placeholder.png";

  /// <summary>Photos are local file names or URLs; missing ones fall back to the placeholder.</summary>
  public static string Photo(string? value) => string.IsNullOrWhiteSpace(value) ? Placeholder : value;
}
