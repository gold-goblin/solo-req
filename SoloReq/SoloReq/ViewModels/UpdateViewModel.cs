using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    private readonly UpdateService _updateService = new();
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _downloadCts;

    public UpdateViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private bool _isDetailsExpanded;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _downloadStatusText = "";
    [ObservableProperty] private string _newVersion = "";
    [ObservableProperty] private string _releaseNotes = "";
    [ObservableProperty] private string _currentVersion = "";
    [ObservableProperty] private bool _isUpdateError;
    [ObservableProperty] private string _updateErrorMessage = "";

    private string? _downloadUrl;

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        CurrentVersion = UpdateService.GetCurrentVersion();
        var settings = _settingsService.Load();

        var result = await _updateService.CheckForUpdateAsync(CurrentVersion);

        if (result.ErrorMessage != null)
            return; // Тихо игнорируем ошибки проверки

        if (!result.IsUpdateAvailable || result.Release == null)
            return;

        var tagVersion = result.Release.TagName.TrimStart('v');

        // Проверяем пропущенную версию
        if (settings?.SkippedVersion == tagVersion)
            return;

        NewVersion = tagVersion;
        ReleaseNotes = result.Release.Body;
        _downloadUrl = result.DownloadUrl;
        IsUpdateAvailable = true;
    }

    [RelayCommand]
    private async Task DownloadAndApplyAsync()
    {
        if (_downloadUrl == null) return;

        IsDownloading = true;
        IsUpdateError = false;
        _downloadCts = new CancellationTokenSource();

        // Определяем расширение файла из URL
        var extension = Path.GetExtension(new Uri(_downloadUrl).AbsolutePath).ToLowerInvariant();
        if (extension != ".rar" && extension != ".zip")
            extension = ".rar"; // fallback
        var tempPath = Path.Combine(Path.GetTempPath(), $"SoloReq_update_{NewVersion}{extension}");

        try
        {
            var progress = new Progress<double>(p =>
            {
                DownloadProgress = p;
                DownloadStatusText = $"$ Загрузка: {p:F0}%";
            });

            DownloadStatusText = "$ Загрузка: 0%";
            await _updateService.DownloadUpdateAsync(_downloadUrl, tempPath, progress, _downloadCts.Token);

            DownloadStatusText = "$ Установка обновления...";
            UpdateService.ApplyUpdate(tempPath);
        }
        catch (OperationCanceledException)
        {
            DownloadStatusText = "";
            CleanupTempFile(tempPath);
        }
        catch (Exception ex)
        {
            IsUpdateError = true;
            UpdateErrorMessage = $"$ Ошибка обновления: {ex.Message}";
            CleanupTempFile(tempPath);
        }
        finally
        {
            IsDownloading = false;
            _downloadCts = null;
        }
    }

    [RelayCommand]
    private void ShowDetails() => IsDetailsExpanded = true;

    [RelayCommand]
    private void HideDetails() => IsDetailsExpanded = false;

    [RelayCommand]
    private void SkipVersion()
    {
        var settings = _settingsService.Load() ?? new AppSettings();
        settings.SkippedVersion = NewVersion;
        _settingsService.Save(settings);
        IsUpdateAvailable = false;
        IsDetailsExpanded = false;
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCts?.Cancel();
    }

    private static void CleanupTempFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
