using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace SoloReq.Services;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string? SkippedVersion { get; set; }
    public double HistoryPanelWidth { get; set; } = 220;
    public double RequestPanelRatio { get; set; } = 0.5;
}

public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsPath = PathResolver.Resolve("settings.json");
    }

    public AppSettings? Load()
    {
        if (!File.Exists(_settingsPath))
            return null;

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonConvert.DeserializeObject<AppSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(_settingsPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }
}

/// <summary>
/// Резолвит путь к файлу данных: если файл существует рядом с exe — использует его (portable),
/// иначе — %LOCALAPPDATA%\SoloReq.
/// </summary>
public static class PathResolver
{
    private static readonly string _exeDir;
    private static readonly string _appDataDir;

    static PathResolver()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        _exeDir = exePath != null ? Path.GetDirectoryName(exePath)! : AppDomain.CurrentDomain.BaseDirectory;
        _appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoloReq");
    }

    /// <summary>
    /// Возвращает путь к файлу: portable (рядом с exe) если существует, иначе AppData.
    /// </summary>
    public static string Resolve(string fileName)
    {
        var portablePath = Path.Combine(_exeDir, fileName);
        if (File.Exists(portablePath))
            return portablePath;

        return Path.Combine(_appDataDir, fileName);
    }

    /// <summary>
    /// Возвращает актуальную папку с настройками (где лежит settings.json или AppData).
    /// </summary>
    public static string GetSettingsDir()
    {
        var portablePath = Path.Combine(_exeDir, "settings.json");
        return File.Exists(portablePath) ? _exeDir : _appDataDir;
    }
}
