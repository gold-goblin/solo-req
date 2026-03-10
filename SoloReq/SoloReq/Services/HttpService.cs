using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SoloReq.Models;

namespace SoloReq.Services;

public class HttpService
{
    private HttpClient _httpClient;
    private bool _sslVerificationDisabled;
    private readonly CookieService _cookieService;

    public HttpService(CookieService cookieService)
    {
        _cookieService = cookieService;
        _httpClient = CreateHttpClient(false);
    }

    public bool SslVerificationDisabled
    {
        get => _sslVerificationDisabled;
        set
        {
            if (_sslVerificationDisabled != value)
            {
                _sslVerificationDisabled = value;
                _httpClient.Dispose();
                _httpClient = CreateHttpClient(value);
            }
        }
    }

    private static HttpClient CreateHttpClient(bool ignoreSsl)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            UseCookies = false // We manage cookies manually
        };

        if (ignoreSsl)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(100)
        };
    }

    public async Task<HttpResponseModel> SendAsync(HttpRequestModel request, CancellationToken cancellationToken = default)
    {
        var response = new HttpResponseModel();
        var sw = Stopwatch.StartNew();

        try
        {
            // Validate URL
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return new HttpResponseModel { IsError = true, ErrorMessage = "$ Ошибка: некорректный URL-адрес" };
            }

            var url = request.Url.Trim();

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return new HttpResponseModel { IsError = true, ErrorMessage = "$ Ошибка: некорректный URL-адрес" };
            }

            // Схема должна быть http или https
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return new HttpResponseModel { IsError = true, ErrorMessage = "$ Ошибка: поддерживаются только http:// и https://" };
            }

            // Host не должен быть пустым (ловит "https:/solo-log.ru" → host="")
            if (string.IsNullOrWhiteSpace(uri.Host))
            {
                return new HttpResponseModel { IsError = true, ErrorMessage = "$ Ошибка: адрес сервера не указан. Проверьте формат: https://example.com" };
            }

            // Add query params
            if (request.QueryParams.Count > 0)
            {
                var queryString = string.Join("&", request.QueryParams
                    .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                var separator = uri.Query.Length > 0 ? "&" : "?";
                url = url + separator + queryString;
                uri = new Uri(url);
            }

            // API Key in query
            if (request.AuthType == "ApiKey" && request.ApiKeyLocation == "Query" &&
                !string.IsNullOrEmpty(request.ApiKeyName))
            {
                var separator = uri.Query.Length > 0 ? "&" : "?";
                url = url + separator + $"{Uri.EscapeDataString(request.ApiKeyName)}={Uri.EscapeDataString(request.ApiKeyValue)}";
                uri = new Uri(url);
            }

            var httpMethod = new HttpMethod(request.Method.ToUpperInvariant());
            using var httpRequest = new HttpRequestMessage(httpMethod, uri);

            // Body
            if (request.Method is not ("GET" or "HEAD" or "OPTIONS") && !string.IsNullOrEmpty(request.Body))
            {
                httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
            }

            // Headers
            foreach (var header in request.Headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key)) continue;
                try
                {
                    if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                catch { }
            }

            // Auth
            switch (request.AuthType)
            {
                case "Basic":
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.AuthUsername}:{request.AuthPassword}"));
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    break;
                case "Bearer":
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AuthToken);
                    break;
                case "ApiKey" when request.ApiKeyLocation == "Header":
                    httpRequest.Headers.TryAddWithoutValidation(request.ApiKeyName, request.ApiKeyValue);
                    break;
                case "Custom":
                    httpRequest.Headers.TryAddWithoutValidation(request.CustomHeaderName, request.CustomHeaderValue);
                    break;
            }

            // Cookies
            if (_cookieService.AutoSendEnabled)
            {
                var cookies = _cookieService.GetCookiesForDomain(uri.Host);
                if (cookies.Count > 0)
                {
                    var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                    httpRequest.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                }
            }

            // Send
            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            sw.Stop();

            response.StatusCode = (int)httpResponse.StatusCode;
            response.ReasonPhrase = httpResponse.ReasonPhrase ?? httpResponse.StatusCode.ToString();
            response.ElapsedMs = sw.ElapsedMilliseconds;

            // Response headers
            foreach (var header in httpResponse.Headers)
                response.Headers[header.Key] = string.Join(", ", header.Value);
            if (httpResponse.Content != null)
            {
                foreach (var header in httpResponse.Content.Headers)
                    response.Headers[header.Key] = string.Join(", ", header.Value);
            }

            // Content type
            response.ContentType = httpResponse.Content?.Headers.ContentType?.MediaType ?? "text/plain";

            // Body
            var bodyBytes = await httpResponse.Content!.ReadAsByteArrayAsync(cancellationToken);
            response.BodySizeBytes = bodyBytes.Length;
            response.Body = Encoding.UTF8.GetString(bodyBytes);

            // Parse Set-Cookie
            if (httpResponse.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
            {
                _cookieService.ParseSetCookieHeaders(setCookieValues, uri.Host);
                foreach (var cookie in _cookieService.GetCookiesForDomain(uri.Host))
                    response.Cookies.Add(cookie);
            }
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            response.IsError = true;
            response.ErrorMessage = "$ Ошибка: превышено время ожидания ответа";
            response.ElapsedMs = sw.ElapsedMilliseconds;
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Security.Authentication.AuthenticationException)
        {
            sw.Stop();
            response.IsError = true;
            response.ErrorMessage = "$ Ошибка SSL-сертификата. Включите «Отключить проверку SSL» в настройках";
            response.ElapsedMs = sw.ElapsedMilliseconds;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            response.IsError = true;
            response.ElapsedMs = sw.ElapsedMilliseconds;

            if (ex.Message.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("name or service not known", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("NameResolutionFailure", StringComparison.OrdinalIgnoreCase) ||
                ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound })
            {
                response.ErrorMessage = "$ Ошибка: не удалось найти сервер (DNS)";
            }
            else if (ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                     ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused })
            {
                response.ErrorMessage = "$ Ошибка: сервер отклонил соединение";
            }
            else
            {
                response.ErrorMessage = $"$ Ошибка сети: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            response.IsError = true;
            response.ErrorMessage = $"$ Ошибка: {ex.Message}";
            response.ElapsedMs = sw.ElapsedMilliseconds;
        }

        return response;
    }
}
