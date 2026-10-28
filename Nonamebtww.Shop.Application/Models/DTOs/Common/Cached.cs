namespace Nonamebtww.Shop.Application.Models.DTOs.Common;

public sealed record Cached<T>(T Value, DateTimeOffset StoredAt, bool IsFresh);
