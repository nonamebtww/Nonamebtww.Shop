using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Common.Validation;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Management;
using Nonamebtww.Shop.Application.Models.DTOs.Catalog.Review;

namespace Nonamebtww.Shop.Application.Validation;

public sealed class SaveCategoryRequestValidator : IValidator<SaveCategoryRequest> {
  public static readonly SaveCategoryRequestValidator Instance = new();

  public Error? Validate(SaveCategoryRequest model) => new ValidationBuilder()
    .Rule(!string.IsNullOrWhiteSpace(model.Name), nameof(model.Name), "Введите название категории.")
    .Rule(model.Name.Length <= Rules.MaxNameLength, nameof(model.Name), $"Название не длиннее {Rules.MaxNameLength} символов.")
    .Rule(model.Id is null || model.ParentId != model.Id, nameof(model.ParentId), "Категория не может быть родителем самой себя.")
    .Build();
}

public sealed class SaveProductRequestValidator : IValidator<SaveProductRequest> {
  public static readonly SaveProductRequestValidator Instance = new();

  public Error? Validate(SaveProductRequest model) => new ValidationBuilder()
    .Rule(model.CategoryId != Guid.Empty, nameof(model.CategoryId), "Выберите категорию.")
    .Rule(!string.IsNullOrWhiteSpace(model.Sku), nameof(model.Sku), "Введите артикул.")
    .Rule(!string.IsNullOrWhiteSpace(model.Name), nameof(model.Name), "Введите название товара.")
    .Rule(model.Name.Length <= Rules.MaxNameLength * 2, nameof(model.Name), "Название слишком длинное.")
    .Rule(model.Price > 0, nameof(model.Price), "Цена должна быть больше нуля.")
    .Rule(model.Stock >= 0, nameof(model.Stock), "Остаток не может быть отрицательным.")
    .Build();
}

public sealed class AddReviewRequestValidator : IValidator<AddReviewRequest> {
  public static readonly AddReviewRequestValidator Instance = new();

  public Error? Validate(AddReviewRequest model) => new ValidationBuilder()
    .Rule(model.ProductId != Guid.Empty, nameof(model.ProductId), "Не указан товар.")
    .Rule(model.Rating is >= 1 and <= 5, nameof(model.Rating), "Поставьте оценку от 1 до 5.")
    .Rule(model.Text is null || model.Text.Length <= Rules.MaxTextLength, nameof(model.Text), $"Отзыв не длиннее {Rules.MaxTextLength} символов.")
    .Build();
}
