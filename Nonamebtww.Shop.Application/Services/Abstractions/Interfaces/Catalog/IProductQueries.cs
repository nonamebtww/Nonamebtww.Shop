using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Product;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Catalog;

/// <summary>Процессы DFD «Просмотр каталога» (поиск товаров) и «Просмотр товара» (GetAsync — карточка с отзывами).</summary>
public interface IProductQueries : ICatalogQueries<ProductSimple, ProductDetailed, ProductSearch>;
