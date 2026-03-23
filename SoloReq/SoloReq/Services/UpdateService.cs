using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SoloReq.Models;

namespace SoloReq.Services;

public class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; set; }
    public GitHubRelease? Release { get; set; }
    public string? DownloadUrl { get; set; }
    public long DownloadSize { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UpdateService
{
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "SoloReq-UpdateChecker/1.0" },
            { "Accept", "application/vnd.github.v3+json" }
        },
        Timeout = TimeSpan.FromSeconds(15)
    };

    private const string ReleasesUrl = "https://api.github.com/repos/gold-goblin/solo-req/releases/latest";

#if SELF_CONTAINED
    public static bool IsSelfContained => true;
#else
    public static bool IsSelfContained => false;
#endif

    public static string GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0] ?? "0.0.0";
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(string currentVersion)
    {
        try
        {
            var response = await _httpClient.GetAsync(ReleasesUrl);
            if (!response.IsSuccessStatusCode)
                return new UpdateCheckResult();

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonConvert.DeserializeObject<GitHubRelease>(json);
            if (release == null)
                return new UpdateCheckResult();

            var tagVersion = release.TagName.TrimStart('v');
            if (!Version.TryParse(tagVersion, out var newVersion) ||
                !Version.TryParse(currentVersion.Split('+')[0], out var current))
                return new UpdateCheckResult();

            if (newVersion <= current)
                return new UpdateCheckResult();

            // Ищем RAR архив в первую очередь, затем ZIP (для обратной совместимости)
            var rarSuffix = IsSelfContained ? "win-x64.rar" : "win-x64-framework.rar";
            var zipSuffix = IsSelfContained ? "win-x64.zip" : "win-x64-framework.zip";
            
            var asset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(rarSuffix, StringComparison.OrdinalIgnoreCase));
            
            // Если RAR не найден, ищем ZIP
            if (asset == null)
            {
                asset = release.Assets.FirstOrDefault(a =>
                    a.Name.EndsWith(zipSuffix, StringComparison.OrdinalIgnoreCase));
            }

            if (asset == null)
                return new UpdateCheckResult();

            return new UpdateCheckResult
            {
                IsUpdateAvailable = true,
                Release = release,
                DownloadUrl = asset.BrowserDownloadUrl,
                DownloadSize = asset.Size
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult { ErrorMessage = ex.Message };
        }
    }

    public async Task DownloadUpdateAsync(string url, string targetPath, IProgress<double> progress, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var buffer = new byte[81920];
        long bytesRead = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        int read;
        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            bytesRead += read;
            if (totalBytes > 0)
                progress.Report((double)bytesRead / totalBytes * 100);
        }
    }

    public static void ApplyUpdate(string downloadedArchivePath)
    {
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Не удалось определить путь текущего exe");
        var appDir = Path.GetDirectoryName(currentExe)!;
        var tempExtractDir = Path.Combine(Path.GetTempPath(), $"SoloReq_update_{Guid.NewGuid()}");
        var isZip = downloadedArchivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

        try
        {
            Directory.CreateDirectory(tempExtractDir);

            // Распаковываем архив
            if (isZip)
                ExtractZip(downloadedArchivePath, tempExtractDir);
            else
                ExtractRar(downloadedArchivePath, tempExtractDir);

            // Находим новый exe в распакованном архиве
            var newExePath = Directory.GetFiles(tempExtractDir, "SoloReq.exe", SearchOption.TopDirectoryOnly).FirstOrDefault()
                ?? Directory.GetFiles(tempExtractDir, "SoloReq.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("SoloReq.exe не найден в архиве обновления");

            // Создаём батник для обновления (т.к. нельзя перезаписать запущенный exe напрямую)
            var updaterScript = CreateUpdaterScript(currentExe, newExePath, tempExtractDir, appDir, downloadedArchivePath);

            // Запускаем батник и завершаем текущее приложение
            Process.Start(new ProcessStartInfo(updaterScript)
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            System.Windows.Application.Current.Shutdown();
        }
        catch
        {
            // Чистим временные файлы при ошибке
            CleanupDirectory(tempExtractDir);
            throw;
        }
    }

    private static void ExtractRar(string rarPath, string extractDir)
    {
        try
        {
            using var archive = RarArchive.Open(rarPath);
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory && !string.IsNullOrEmpty(e.Key)))
            {
                var entryKey = entry.Key!;
                var destPath = Path.Combine(extractDir, entryKey);
                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                entry.WriteToFile(destPath, new SharpCompress.Common.ExtractionOptions
                {
                    Overwrite = true,
                    PreserveFileTime = true
                });
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка распаковки архива: {ex.Message}", ex);
        }
    }

    private static void ExtractZip(string zipPath, string extractDir)
    {
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка распаковки ZIP архива: {ex.Message}", ex);
        }
    }

    private static string CreateUpdaterScript(string currentExe, string newExePath, string tempExtractDir, string appDir, string archivePath)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"SoloReq_updater_{Guid.NewGuid()}.bat");
        var newExeDir = Path.GetDirectoryName(newExePath)!;

        // Скрипт:
        // 1. Ждёт завершения процесса SoloReq
        // 2. Копирует новые файлы из распакованного архива
        // 3. Запускает новую версию
        // 4. Чистит временные файлы
        var script = $@"
@echo off
chcp 65001 >nul
title SoloReq Updater

:: Ждём завершения процесса SoloReq
:waitloop
tasklist | findstr /i ""SoloReq.exe"" >nul
if %errorlevel% == 0 (
    timeout /t 1 /nobreak >nul
    goto waitloop
)

:: Небольшая пауза на всякий случай
timeout /t 1 /nobreak >nul

:: Копируем все файлы из распакованного архива
xcopy /s /y /q ""{newExeDir}\*.*"" ""{appDir}\""

:: Если копирование успешно, чистим
if %errorlevel% == 0 (
    rmdir /s /q ""{tempExtractDir}""
    del /f /q ""{archivePath}""
    del /f /q ""{scriptPath}""
    
    :: Запускаем новую версию
    start "" ""{currentExe}""
) else (
    echo Ошибка обновления
    pause
)
";

        File.WriteAllText(scriptPath, script);
        return scriptPath;
    }

    public static void CleanupOldVersion()
    {
        // Старая логика больше не нужна, т.к. updater батник сам чистит
        // Но оставим для обратной совместимости (если остался SoloReq_old.exe)
        try
        {
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (currentExe == null) return;
            var oldExe = Path.Combine(Path.GetDirectoryName(currentExe)!, "SoloReq_old.exe");
            if (File.Exists(oldExe))
                File.Delete(oldExe);
        }
        catch { /* ignore */ }
    }

    private static void CleanupDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch { /* ignore */ }
    }
}
