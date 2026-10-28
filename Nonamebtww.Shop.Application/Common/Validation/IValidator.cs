using Nonamebtww.Shop.Application.Common.Results;

namespace Nonamebtww.Shop.Application.Common.Validation;

public interface IValidator<in T> {
  /// <summary>Возвращает null, если модель корректна, иначе ошибку типа Validation.</summary>
  Error? Validate(T model);
}
