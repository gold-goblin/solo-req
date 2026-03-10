using System.IO;
using Newtonsoft.Json;

namespace SoloReq.Services;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
}

public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
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
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }
}
