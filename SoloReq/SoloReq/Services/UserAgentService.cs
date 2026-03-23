using System.IO;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
using SoloReq.Models;

namespace SoloReq.Services;

public class UserAgentService
{
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", $"solo_req/{GetVersion()}" }
        },
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string ApiUrl = "https://api.solo-log.ru/solo-req/v1/user-agents";
    private readonly object _lock = new();
    private List<UserAgentEntry> _entries = new();
    private List<UserAgentEntry> _customEntries = new();

    public async Task LoadAsync()
    {
        LoadCustomEntries();

        try
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<UserAgentsApiResponse>(json);
                if (apiResponse != null)
                {
                    var entries = new List<UserAgentEntry>();
                    foreach (var raw in apiResponse.Desktop)
                        entries.Add(ToEntry(raw, "desktop"));
                    foreach (var raw in apiResponse.Mobile)
                        entries.Add(ToEntry(raw, "mobile"));

                    lock (_lock) { _entries = entries; }
                    SaveCache(entries);
                    return;
                }
            }
        }
        catch { /* fallback to cache */ }

        var cached = LoadCache();
        if (cached != null)
            lock (_lock) { _entries = cached; }
    }

    public IReadOnlyList<UserAgentEntry> GetEntries()
    {
        lock (_lock)
        {
            var merged = new List<UserAgentEntry>(_entries.Count + _customEntries.Count);
            merged.AddRange(_entries);
            merged.AddRange(_customEntries);
            return merged;
        }
    }

    public void AddCustomEntry(UserAgentEntry entry)
    {
        entry.IsCustom = true;
        entry.DeviceType = "custom";
        lock (_lock) { _customEntries.Add(entry); }
        SaveCustomEntries();
    }

    public void UpdateCustomEntry(UserAgentEntry old, UserAgentEntry updated)
    {
        updated.IsCustom = true;
        updated.DeviceType = "custom";
        lock (_lock)
        {
            var index = _customEntries.IndexOf(old);
            if (index >= 0)
                _customEntries[index] = updated;
        }
        SaveCustomEntries();
    }

    public void DeleteCustomEntry(UserAgentEntry entry)
    {
        lock (_lock) { _customEntries.Remove(entry); }
        SaveCustomEntries();
    }

    private static UserAgentEntry ToEntry(UserAgentRawEntry raw, string deviceType) => new()
    {
        Ua = raw.Ua,
        Browser = raw.Browser,
        BrowserVersion = raw.BrowserVersion,
        Os = raw.Os,
        DeviceType = deviceType
    };

    private static string GetCachePath() => PathResolver.Resolve("user-agents.json");
    private static string GetCustomPath() => PathResolver.Resolve("custom-user-agents.json");

    private static void SaveCache(List<UserAgentEntry> entries)
    {
        try
        {
            var path = GetCachePath();
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch { /* ignore */ }
    }

    private static List<UserAgentEntry>? LoadCache()
    {
        try
        {
            var path = GetCachePath();
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<UserAgentEntry>>(json);
        }
        catch { return null; }
    }

    private void SaveCustomEntries()
    {
        try
        {
            List<UserAgentEntry> snapshot;
            lock (_lock) { snapshot = new List<UserAgentEntry>(_customEntries); }
            var path = GetCustomPath();
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch { /* ignore */ }
    }

    private void LoadCustomEntries()
    {
        try
        {
            var path = GetCustomPath();
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            var entries = JsonConvert.DeserializeObject<List<UserAgentEntry>>(json);
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    e.IsCustom = true;
                    e.DeviceType = "custom";
                }
                lock (_lock) { _customEntries = entries; }
            }
        }
        catch { /* ignore */ }
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0] ?? "1.0.0";
    }
}
