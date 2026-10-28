using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Storage;

namespace Nonamebtww.Shop.Infrastructure.Api.Http;

/// <summary>
/// Основа для будущих HTTP-реализаций I*Api. В прототипе не используется
/// (включается только при InfrastructureOptions.UseRemoteApi = true).
/// Подставляет access-токен из ISessionStore и переводит HTTP-ошибки в Error.
/// </summary>
public abstract class HttpApiClientBase {
  protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly HttpClient _http;
  private readonly ISessionStore _sessionStore;

  protected HttpApiClientBase(HttpClient http, ISessionStore sessionStore) {
    _http = http;
    _sessionStore = sessionStore;
  }

  protected Task<Result<T>> GetAsync<T>(string url, CancellationToken ct) =>
    SendAsync<T>(HttpMethod.Get, url, null, ct);

  protected Task<Result<T>> PostAsync<T>(string url, object? body, CancellationToken ct) =>
    SendAsync<T>(HttpMethod.Post, url, body, ct);

  protected Task<Result<T>> PutAsync<T>(string url, object? body, CancellationToken ct) =>
    SendAsync<T>(HttpMethod.Put, url, body, ct);

  protected Task<Result<T>> DeleteAsync<T>(string url, CancellationToken ct) =>
    SendAsync<T>(HttpMethod.Delete, url, null, ct);

  protected async Task<Result> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct) {
    using var response = await SendRawAsync(method, url, body, ct);
    return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ToErrorAsync(response, ct));
  }

  protected async Task<Result<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct) {
    using var response = await SendRawAsync(method, url, body, ct);

    if (!response.IsSuccessStatusCode) {
      return Result<T>.Failure(await ToErrorAsync(response, ct));
    }

    var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    return value is null
      ? Result<T>.Failure(Error.Failure("api.empty_response", "Сервер вернул пустой ответ."))
      : Result<T>.Success(value);
  }

  private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, CancellationToken ct) {
    using var request = new HttpRequestMessage(method, url);

    if (body is not null) {
      request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
    }

    var session = await _sessionStore.LoadAsync(ct);

    if (session is not null) {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Tokens.AccessToken);
    }

    return await _http.SendAsync(request, ct);
  }

  private static async Task<Error> ToErrorAsync(HttpResponseMessage response, CancellationToken ct) {
    var details = await response.Content.ReadAsStringAsync(ct);

    return response.StatusCode switch {
      HttpStatusCode.NotFound => Error.NotFound("api.not_found", "Не найдено."),
      HttpStatusCode.Unauthorized => Error.Unauthorized("api.unauthorized", "Требуется вход в систему."),
      HttpStatusCode.Forbidden => Error.Forbidden("api.forbidden", "Недостаточно прав."),
      HttpStatusCode.Conflict => Error.Conflict("api.conflict", details),
      HttpStatusCode.BadRequest => Error.Failure("api.bad_request", details),
      _ => Error.Failure("api.error", $"Ошибка сервера: {(int)response.StatusCode}.")
    };
  }
}
