namespace Nonamebtww.Shop.Application.Common.Results;

public sealed record PagedList<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount) {
  public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
  public bool HasNextPage => Page < TotalPages;

  public static PagedList<T> Empty(int page = 1, int pageSize = 20) => new([], page, pageSize, 0);
}
