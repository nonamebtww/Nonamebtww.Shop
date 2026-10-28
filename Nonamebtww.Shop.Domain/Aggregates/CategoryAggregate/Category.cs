using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;

/// <summary>
/// Категория каталога (IDEF1X «Категория»), образует дерево через <see cref="ParentId"/>.
/// Проверку на циклы при переносе выполняет приложение по дереву из <see cref="ICategoriesRepository"/>.
/// </summary>
public class Category : AggregateRoot<Guid>, IHasDetails {
  private Category() { }

  private Category(Guid id, string name, Guid? parentId, string? description, string? photo) : base(id) {
    SetDetails(name, description, photo);
    MoveTo(parentId);
  }

  public Guid? ParentId { get; private set; }
  public string Name { get; private set; } = null!;
  public string? Description { get; private set; }
  public string? Photo { get; private set; }

  public bool IsRoot => ParentId is null;

  public static Category Create(string name, Guid? parentId = null, string? description = null, string? photo = null) =>
    new(Guid.NewGuid(), name, parentId, description, photo);

  public void Update(string name, string? description, string? photo) => SetDetails(name, description, photo);

  public void MoveTo(Guid? parentId) {
    if (parentId == Guid.Empty) parentId = null;
    if (parentId == Id) throw new DomainException("Категория не может быть родителем самой себя.");
    ParentId = parentId;
  }

  private void SetDetails(string name, string? description, string? photo) {
    Name = Guard.NotEmpty(name, "Название категории", 128);
    Description = Guard.Optional(description, "Описание категории", 2048);
    Photo = Guard.Optional(photo, "Фото категории", 1024);
  }
}
