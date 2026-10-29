namespace Nonamebtww.Shop.Application.Common;

internal static class Paging {
  public static (int Page, int Size) Normalize(int page, int size, int defaultSize = 20, int maxSize = 200) =>
    (Math.Max(1, page), size <= 0 ? defaultSize : Math.Min(size, maxSize));

  public static List<T> Slice<T>(IEnumerable<T> source, int page, int size) =>
    source.Skip((page - 1) * size).Take(size).ToList();
}
