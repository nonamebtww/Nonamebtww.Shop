namespace Nonamebtww.Shop.Mobile.Presentation;

public static class ImageSources {
  public const string Placeholder = "product_placeholder.png";

  /// <summary>Photos are local file names or URLs; missing ones fall back to the placeholder.</summary>
  public static string Photo(string? value) => string.IsNullOrWhiteSpace(value) ? Placeholder : value;

  /// <summary>
  /// Small variant of a photo for lists and grids. Full photos are 1000×1000 px; decoding them in every
  /// list cell made scrolling stutter. The demo CDN keeps a 300×300 «thumbnail.webp» next to each photo
  /// (a real backend would return a thumbnail URL); other sources fall back to the photo itself.
  /// </summary>
  public static string Thumbnail(string? value) {
    var photo = Photo(value);
    if (!photo.StartsWith("https://cdn.dummyjson.com/product-images/", StringComparison.Ordinal)) {
      return photo;
    }

    var slash = photo.LastIndexOf('/');
    return photo[..(slash + 1)] + "thumbnail.webp";
  }
}
