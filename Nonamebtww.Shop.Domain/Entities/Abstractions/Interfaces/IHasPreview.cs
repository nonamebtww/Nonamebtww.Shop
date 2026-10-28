namespace Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

/// <summary>Данные для краткой карточки (списки каталога).</summary>
public interface IHasPreview {
  public string Name { get; }
  public string? Photo { get; }
}
