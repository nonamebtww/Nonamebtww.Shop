namespace Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

/// <summary>Данные для подробной карточки.</summary>
public interface IHasDetails : IHasPreview {
  public string? Description { get; }
}
