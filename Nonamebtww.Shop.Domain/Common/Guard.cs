using Nonamebtww.Shop.Domain.Exceptions;

namespace Nonamebtww.Shop.Domain.Common;

internal static class Guard {
  public static string NotEmpty(string? value, string name, int maxLength = 256) {
    if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"Поле «{name}» не может быть пустым.");
    var trimmed = value.Trim();
    if (trimmed.Length > maxLength) throw new DomainException($"Поле «{name}» длиннее {maxLength} символов.");
    return trimmed;
  }

  public static string? Optional(string? value, string name, int maxLength = 2048) {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    if (trimmed.Length > maxLength) throw new DomainException($"Поле «{name}» длиннее {maxLength} символов.");
    return trimmed;
  }

  public static Guid NotEmpty(Guid value, string name) {
    if (value == Guid.Empty) throw new DomainException($"Не задан идентификатор «{name}».");
    return value;
  }

  public static int Positive(int value, string name) {
    if (value <= 0) throw new DomainException($"Значение «{name}» должно быть больше нуля.");
    return value;
  }

  public static int NotNegative(int value, string name) {
    if (value < 0) throw new DomainException($"Значение «{name}» не может быть отрицательным.");
    return value;
  }
}
