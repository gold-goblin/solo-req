using Newtonsoft.Json;

namespace SoloReq.Models;

public class GitHubRelease
{
    [JsonProperty("tag_name")]
    public string TagName { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("body")]
    public string Body { get; set; } = "";

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; } = "";

    [JsonProperty("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("assets")]
    public GitHubAsset[] Assets { get; set; } = [];
}

public class GitHubAsset
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonProperty("size")]
    public long Size { get; set; }
}
