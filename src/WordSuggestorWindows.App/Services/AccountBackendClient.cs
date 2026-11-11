using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface IAccountBackend
{
    string Origin { get; }
    Task<T> SendAsync<T>(HttpMethod method, string path, object? body, string? accessToken,
        CancellationToken cancellation, string? idempotencyKey = null);
}

public sealed class AccountBackendClient : IAccountBackend, IDisposable
{
    private readonly HttpClient _http;
    public string Origin { get; }
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    public static readonly JsonSerializerOptions Snake = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    public AccountBackendClient(string origin = "https://api.wordsuggestor.com", HttpMessageHandler? handler = null)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0 ||
            uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0) throw new AccountFailure("invalid_origin");
        Origin = uri.GetLeftPart(UriPartial.Authority);
        _http = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = TimeSpan.FromSeconds(12) };
    }
    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, string? accessToken,
        CancellationToken cancellation, string? idempotencyKey = null)
    {
        if (!path.StartsWith("/v1/", StringComparison.Ordinal) || path.Contains("..") || path.Contains('?') || path.Contains('#') || path.Contains('\\')) throw new AccountFailure("invalid_path");
        // Backend JacksonConfig applies snake_case to every endpoint.
        var options = Snake;
        using var request = new HttpRequestMessage(method, Origin + path);
        request.Headers.Accept.Add(new("application/json"));
        if (accessToken is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body, options), Encoding.UTF8, "application/json");
        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                // Never include backend bodies, URI, profile values or credentials in errors.
                var code = response.StatusCode switch { HttpStatusCode.Unauthorized => "unauthorized", HttpStatusCode.Forbidden => "forbidden", HttpStatusCode.Conflict => "conflict", (HttpStatusCode)429 => "rate_limited", _ => "server_error" };
                throw new AccountFailure(code, (int)response.StatusCode);
            }
            if (typeof(T) == typeof(bool)) return (T)(object)true;
            if (response.Content.Headers.ContentLength > 65536) throw new AccountFailure("invalid_response");
            using var stream = await response.Content.ReadAsStreamAsync(cancellation);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096]; int count;
            while ((count = await stream.ReadAsync(chunk, cancellation)) > 0)
            { if (buffer.Length + count > 65536) throw new AccountFailure("invalid_response"); await buffer.WriteAsync(chunk.AsMemory(0, count), cancellation); }
            return JsonSerializer.Deserialize<T>(buffer.ToArray(), options) ?? throw new AccountFailure("invalid_response");
        }
        catch (JsonException) { throw new AccountFailure("invalid_response"); }
        catch (HttpRequestException) { throw new AccountFailure("offline"); }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { throw new AccountFailure("offline"); }
    }
    public void Dispose() => _http.Dispose();
}
