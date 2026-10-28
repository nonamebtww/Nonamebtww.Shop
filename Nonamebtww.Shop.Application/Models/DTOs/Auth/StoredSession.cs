namespace Nonamebtww.Shop.Application.Models.DTOs.Auth;

/// <summary>Сессия, сохранённая на устройстве между запусками.</summary>
public sealed record StoredSession(Guid UserId, Guid DeviceId, AuthTokens Tokens);
