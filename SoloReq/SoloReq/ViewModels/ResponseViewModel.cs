using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class ResponseViewModel : ObservableObject
{
    private readonly JsonFormatterService _jsonFormatter;

    public ResponseViewModel(JsonFormatterService jsonFormatter)
    {
        _jsonFormatter = jsonFormatter;
    }

    [ObservableProperty] private int _statusCode;
    [ObservableProperty] private string _statusLine = string.Empty;
    [ObservableProperty] private string _body = string.Empty;
    [ObservableProperty] private long _elapsedMs;
    [ObservableProperty] private long _bodySizeBytes;
    [ObservableProperty] private string _contentType = string.Empty;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasResponse;

    public ObservableCollection<KeyValuePair<string, string>> Headers { get; } = new();
    public ObservableCollection<CookieItem> Cookies { get; } = new();

    public void LoadFromResponse(HttpResponseModel response)
    {
        StatusCode = response.StatusCode;
        ElapsedMs = response.ElapsedMs;
        BodySizeBytes = response.BodySizeBytes;
        ContentType = response.ContentType;
        IsError = response.IsError;
        ErrorMessage = response.ErrorMessage;
        HasResponse = true;

        if (response.IsError)
        {
            StatusLine = response.ErrorMessage;
            Body = string.Empty;
        }
        else
        {
            StatusLine = $"$ Ответ сервера: {response.StatusCode}. Выполнился за: {response.ElapsedMs}мс. Размер ответа: {FormatSize(response.BodySizeBytes)}";

            // Auto-format JSON
            if (response.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                Body = _jsonFormatter.Format(response.Body);
            else
                Body = response.Body;
        }

        Headers.Clear();
        foreach (var h in response.Headers)
            Headers.Add(h);

        Cookies.Clear();
        foreach (var c in response.Cookies)
            Cookies.Add(c);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} Б",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} КБ",
        _ => $"{bytes / (1024.0 * 1024.0):F1} МБ"
    };

    [RelayCommand]
    private void CopyBody()
    {
        if (!string.IsNullOrEmpty(Body))
            Clipboard.SetText(Body);
    }

    [RelayCommand]
    private void CopyHeaders()
    {
        var text = string.Join("\n", Headers.Select(h => $"{h.Key}: {h.Value}"));
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    [RelayCommand]
    private void SaveBody()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Все файлы (*.*)|*.*|JSON (*.json)|*.json|XML (*.xml)|*.xml|HTML (*.html)|*.html|Текст (*.txt)|*.txt",
            DefaultExt = ContentType switch
            {
                var ct when ct.Contains("json") => ".json",
                var ct when ct.Contains("xml") => ".xml",
                var ct when ct.Contains("html") => ".html",
                _ => ".txt"
            }
        };

        if (dialog.ShowDialog() == true)
            File.WriteAllText(dialog.FileName, Body);
    }

    [RelayCommand]
    private void FormatResponse()
    {
        if (ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            Body = _jsonFormatter.Format(Body);
    }

    public void Clear()
    {
        StatusCode = 0;
        StatusLine = string.Empty;
        Body = string.Empty;
        ElapsedMs = 0;
        BodySizeBytes = 0;
        ContentType = string.Empty;
        IsError = false;
        ErrorMessage = string.Empty;
        HasResponse = false;
        Headers.Clear();
        Cookies.Clear();
    }
}
