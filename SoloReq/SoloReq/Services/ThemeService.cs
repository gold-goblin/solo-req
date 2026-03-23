using System.Windows;

namespace SoloReq.Services;

public class ThemeService
{
    private readonly SettingsService _settingsService;

    public static event Action<bool>? ThemeChanged;
    public static bool CurrentIsDark { get; private set; } = true;

    public bool IsDarkTheme { get; private set; } = true;
    public bool IsFirstRun { get; private set; }

    public ThemeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        var settings = _settingsService.Load();
        if (settings == null)
        {
            IsFirstRun = true;
            return;
        }

        IsFirstRun = false;
        if (settings.Theme == "Light")
        {
            ApplyTheme(isDark: false);
        }
    }

    public void ApplyTheme(bool isDark)
    {
        CurrentIsDark = isDark;
        IsDarkTheme = isDark;
        var themePath = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

        var newTheme = new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        };

        var app = Application.Current;
        if (app.Resources.MergedDictionaries.Count > 0)
            app.Resources.MergedDictionaries[0] = newTheme;
        else
            app.Resources.MergedDictionaries.Add(newTheme);

        var existingSettings = _settingsService.Load() ?? new AppSettings();
        existingSettings.Theme = isDark ? "Dark" : "Light";
        _settingsService.Save(existingSettings);
        ThemeChanged?.Invoke(isDark);
    }

    public void ToggleTheme()
    {
        ApplyTheme(!IsDarkTheme);
    }
}
