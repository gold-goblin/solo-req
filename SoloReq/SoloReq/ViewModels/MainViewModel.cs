using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HttpService _httpService;
    private readonly ThemeService _themeService;
    private readonly CookieService _cookieService;
    private readonly JsonFormatterService _jsonFormatter;
    private CancellationTokenSource? _cts;

    public bool IsFirstRun => _themeService.IsFirstRun;

    public MainViewModel()
    {
        _jsonFormatter = new JsonFormatterService();
        _cookieService = new CookieService();
        _httpService = new HttpService(_cookieService);
        var settingsService = new SettingsService();
        _themeService = new ThemeService(settingsService);
        _themeService.Initialize();
        _isDarkTheme = _themeService.IsDarkTheme;

        Request = new RequestViewModel(_jsonFormatter);
        Response = new ResponseViewModel(_jsonFormatter);
        History = new HistoryViewModel();
        CookieManager = new CookieManagerViewModel(_cookieService);

        Request.OnHttpSchemeDetected = httpRequested =>
        {
            if (httpRequested && !SslVerificationDisabled)
            {
                var result = MessageBox.Show(
                    "URL содержит http://. Отключить SSL?",
                    ">_ solo-req",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    SslVerificationDisabled = true;
            }
        };

        History.OnItemSelected = OnHistoryItemSelected;
        History.OnRepeatRequested = item =>
        {
            Request.LoadFromRequest(item.Request);
            SendRequestCommand.Execute(null);
        };
    }

    public RequestViewModel Request { get; }
    public ResponseViewModel Response { get; }
    public HistoryViewModel History { get; }
    public CookieManagerViewModel CookieManager { get; }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _sslVerificationDisabled;
    [ObservableProperty] private bool _isDarkTheme = true;

    // Commands set from code-behind (need View access)
    public ICommand? FocusUrlCommand { get; set; }
    public ICommand? FocusHistoryCommand { get; set; }
    public ICommand? OpenCookieManagerCommand { get; set; }
    public ICommand? OpenAboutCommand { get; set; }

    partial void OnSslVerificationDisabledChanged(bool value)
    {
        _httpService.SslVerificationDisabled = value;
        if (value)
        {
            MessageBox.Show(
                "Внимание: отключение проверки SSL снижает безопасность.\nИспользуйте только для тестирования.",
                ">_ solo-req",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task SendRequestAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        IsLoading = true;
        Response.Clear();

        try
        {
            var requestModel = Request.BuildRequest(!SslVerificationDisabled);
            var responseModel = await _httpService.SendAsync(requestModel, _cts.Token);
            Response.LoadFromResponse(responseModel);
            History.AddItem(requestModel, responseModel);
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyTheme(bool isDark)
    {
        _themeService.ApplyTheme(isDark);
        IsDarkTheme = _themeService.IsDarkTheme;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        IsDarkTheme = _themeService.IsDarkTheme;
    }

    [RelayCommand]
    private void ClearRequest()
    {
        Request.Clear();
        Response.Clear();
    }

    private void OnHistoryItemSelected(HistoryItem item)
    {
        Request.LoadFromRequest(item.Request);
        Response.LoadFromResponse(item.Response);
    }
}
