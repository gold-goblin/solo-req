namespace SoloReq.Models;

public class HttpResponseModel
{
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public List<CookieItem> Cookies { get; set; } = new();
    public long ElapsedMs { get; set; }
    public long BodySizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
