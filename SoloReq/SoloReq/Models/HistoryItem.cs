namespace SoloReq.Models;

public class HistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public HttpRequestModel Request { get; set; } = new();
    public HttpResponseModel Response { get; set; } = new();
}
