using Newtonsoft.Json;

namespace SoloReq.Models;

public class UserAgentRawEntry
{
    [JsonProperty("ua")] public string Ua { get; set; } = string.Empty;
    [JsonProperty("browser")] public string Browser { get; set; } = string.Empty;
    [JsonProperty("browser_version")] public string BrowserVersion { get; set; } = string.Empty;
    [JsonProperty("os")] public string Os { get; set; } = string.Empty;
}

public class UserAgentsApiResponse
{
    [JsonProperty("desktop")] public List<UserAgentRawEntry> Desktop { get; set; } = new();
    [JsonProperty("mobile")] public List<UserAgentRawEntry> Mobile { get; set; } = new();
}

public class UserAgentEntry
{
    public string Ua { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = false;
}
