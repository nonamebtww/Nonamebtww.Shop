namespace Nonamebtww.Shop.Application.Common.Results;

/// <summary>Ошибка выполнения use case. Code стабилен и подходит для локализации в UI.</summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure) {
  public IReadOnlyDictionary<string, string[]> Fields { get; init; } = new Dictionary<string, string[]>();

  public static Error Failure(string code, string message) => new(code, message);
  public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
  public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
  public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
  public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

  public static Error Validation(IReadOnlyDictionary<string, string[]> fields) =>
    new("validation", "Проверьте введённые данные.", ErrorType.Validation) { Fields = fields };

  public static Error Validation(string field, string message) =>
    Validation(new Dictionary<string, string[]> { [field] = [message] });
}
