using Nonamebtww.Shop.Application.Common.Results;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;

/// <summary>Общие запросы чтения каталога.</summary>
public interface ICatalogQueries<TSimple, TDetailed, TSearch> where TSimple : struct where TDetailed : struct where TSearch : struct {
  Task<Result<PagedList<TSimple>>> SearchAsync(TSearch search, CancellationToken ct = default);
  Task<Result<TDetailed>> GetAsync(Guid id, CancellationToken ct = default);
}
