using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class RequestViewModel : ObservableObject
{
    private readonly JsonFormatterService _jsonFormatter;

    public RequestViewModel(JsonFormatterService jsonFormatter)
    {
        _jsonFormatter = jsonFormatter;
        Headers.Add(new HeaderItem { Key = "Content-Type", Value = "application/json" });
        Headers.Add(new HeaderItem { Key = "Accept", Value = "application/json" });
    }

    [ObservableProperty] private string _url = string.Empty;

    // Событие: RequestViewModel просит MainViewModel переключить SSL
    // Action<bool> — true = запрошен http://, false = запрошен https://
    public Action<bool>? OnHttpSchemeDetected { get; set; }

    partial void OnUrlChanged(string value)
    {
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Url = value[8..];
        }
        else if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            Url = value[7..];
            OnHttpSchemeDetected?.Invoke(true);
        }
    }

    public static string StripScheme(string url)
    {
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url[8..];
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return url[7..];
        return url;
    }
    [ObservableProperty] private string _selectedMethod = "GET";
    [ObservableProperty] private string _body = string.Empty;
    [ObservableProperty] private string _authType = "None";
    [ObservableProperty] private string _authUsername = string.Empty;
    [ObservableProperty] private string _authPassword = string.Empty;
    [ObservableProperty] private string _authToken = string.Empty;
    [ObservableProperty] private string _apiKeyName = string.Empty;
    [ObservableProperty] private string _apiKeyValue = string.Empty;
    [ObservableProperty] private string _apiKeyLocation = "Header";
    [ObservableProperty] private string _customHeaderName = string.Empty;
    [ObservableProperty] private string _customHeaderValue = string.Empty;

    public ObservableCollection<HeaderItem> Headers { get; } = new();
    public ObservableCollection<QueryParamItem> QueryParams { get; } = new();

    public string[] Methods { get; } = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };
    public string[] AuthTypes { get; } = { "None", "Basic", "Bearer", "ApiKey", "Custom" };
    public string[] ApiKeyLocations { get; } = { "Header", "Query" };

    public bool IsBodyEnabled => SelectedMethod is not ("GET" or "HEAD" or "OPTIONS");

    partial void OnSelectedMethodChanged(string value) => OnPropertyChanged(nameof(IsBodyEnabled));

    [RelayCommand]
    private void FormatJson()
    {
        if (!string.IsNullOrWhiteSpace(Body))
            Body = _jsonFormatter.Format(Body);
    }

    [RelayCommand]
    private void MinifyJson()
    {
        if (!string.IsNullOrWhiteSpace(Body))
            Body = _jsonFormatter.Minify(Body);
    }

    [RelayCommand]
    private void AddHeader() => Headers.Add(new HeaderItem());

    [RelayCommand]
    private void RemoveHeader(HeaderItem? header)
    {
        if (header != null) Headers.Remove(header);
    }

    [RelayCommand]
    private void AddQueryParam() => QueryParams.Add(new QueryParamItem());

    [RelayCommand]
    private void RemoveQueryParam(QueryParamItem? param)
    {
        if (param != null) QueryParams.Remove(param);
    }

    public HttpRequestModel BuildRequest(bool useSsl)
    {
        var scheme = useSsl ? "https://" : "http://";
        var request = new HttpRequestModel
        {
            Url = scheme + StripScheme(Url.Trim()),
            Method = SelectedMethod,
            Body = Body,
            AuthType = AuthType,
            AuthUsername = AuthUsername,
            AuthPassword = AuthPassword,
            AuthToken = AuthToken,
            ApiKeyName = ApiKeyName,
            ApiKeyValue = ApiKeyValue,
            ApiKeyLocation = ApiKeyLocation,
            CustomHeaderName = CustomHeaderName,
            CustomHeaderValue = CustomHeaderValue
        };

        foreach (var h in Headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key)))
            request.Headers[h.Key] = h.Value;

        foreach (var p in QueryParams.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key)))
            request.QueryParams[p.Key] = p.Value;

        return request;
    }

    public void LoadFromRequest(HttpRequestModel request)
    {
        Url = StripScheme(request.Url);
        SelectedMethod = request.Method;
        Body = request.Body;
        AuthType = request.AuthType;
        AuthUsername = request.AuthUsername;
        AuthPassword = request.AuthPassword;
        AuthToken = request.AuthToken;
        ApiKeyName = request.ApiKeyName;
        ApiKeyValue = request.ApiKeyValue;
        ApiKeyLocation = request.ApiKeyLocation;
        CustomHeaderName = request.CustomHeaderName;
        CustomHeaderValue = request.CustomHeaderValue;

        Headers.Clear();
        foreach (var h in request.Headers)
            Headers.Add(new HeaderItem { Key = h.Key, Value = h.Value });

        QueryParams.Clear();
        foreach (var p in request.QueryParams)
            QueryParams.Add(new QueryParamItem { Key = p.Key, Value = p.Value });
    }

    public void Clear()
    {
        Url = string.Empty;
        SelectedMethod = "GET";
        Body = string.Empty;
        AuthType = "None";
        AuthUsername = string.Empty;
        AuthPassword = string.Empty;
        AuthToken = string.Empty;
        ApiKeyName = string.Empty;
        ApiKeyValue = string.Empty;
        ApiKeyLocation = "Header";
        CustomHeaderName = string.Empty;
        CustomHeaderValue = string.Empty;
        Headers.Clear();
        Headers.Add(new HeaderItem { Key = "Content-Type", Value = "application/json" });
        Headers.Add(new HeaderItem { Key = "Accept", Value = "application/json" });
        QueryParams.Clear();
    }
}
