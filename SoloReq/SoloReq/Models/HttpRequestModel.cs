namespace SoloReq.Models;

public class HttpRequestModel
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public string AuthType { get; set; } = "None"; // None, Basic, Bearer, ApiKey, Custom
    public string AuthUsername { get; set; } = string.Empty;
    public string AuthPassword { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string ApiKeyName { get; set; } = string.Empty;
    public string ApiKeyValue { get; set; } = string.Empty;
    public string ApiKeyLocation { get; set; } = "Header"; // Header or Query
    public string CustomHeaderName { get; set; } = string.Empty;
    public string CustomHeaderValue { get; set; } = string.Empty;
}
