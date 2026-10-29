namespace Nonamebtww.Shop.Application.Mappings;

/// <summary>
/// Характеристики товара хранятся в Domain одной строкой («Ключ: значение» на строку),
/// в DTO — словарём.
/// </summary>
internal static class Characteristics {
  public static IReadOnlyDictionary<string, string> Parse(string? text) {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (string.IsNullOrWhiteSpace(text)) {
      return result;
    }

    foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
      var separator = line.IndexOf(':');
      if (separator <= 0) {
        result[line] = string.Empty;
        continue;
      }

      result[line[..separator].Trim()] = line[(separator + 1)..].Trim();
    }

    return result;
  }

  public static string? Format(IReadOnlyDictionary<string, string>? values) =>
    values is null || values.Count == 0
      ? null
      : string.Join('\n', values.Where(x => !string.IsNullOrWhiteSpace(x.Key)).Select(x => $"{x.Key.Trim()}: {x.Value.Trim()}"));
}
