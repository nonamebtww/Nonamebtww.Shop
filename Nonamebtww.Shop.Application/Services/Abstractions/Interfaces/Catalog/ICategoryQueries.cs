using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Category;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;

/// <summary>Процесс DFD «Просмотр каталога»: категории.</summary>
public interface ICategoryQueries : ICatalogQueries<CategorySimple, CategoryDetailed, CategorySearch> {
  Task<Result<IReadOnlyList<CategoryNode>>> GetTreeAsync(CancellationToken ct = default);
}
