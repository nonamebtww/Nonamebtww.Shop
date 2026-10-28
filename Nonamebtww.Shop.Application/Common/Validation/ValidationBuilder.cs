using Nonamebtww.Shop.Application.Common.Results;

namespace Nonamebtww.Shop.Application.Common.Validation;

/// <summary>Накопитель ошибок по полям. Без внешних библиотек, чтобы прототип не тянул зависимости.</summary>
public sealed class ValidationBuilder {
  private readonly Dictionary<string, List<string>> _errors = new();

  public ValidationBuilder Rule(bool condition, string field, string message) {
    if (!condition) {
      Add(field, message);
    }

    return this;
  }

  public ValidationBuilder Add(string field, string message) {
    if (!_errors.TryGetValue(field, out var list)) {
      _errors[field] = list = [];
    }

    list.Add(message);
    return this;
  }

  public bool IsValid => _errors.Count == 0;

  public Error? Build() =>
    IsValid ? null : Error.Validation(_errors.ToDictionary(x => x.Key, x => x.Value.ToArray()));
}
