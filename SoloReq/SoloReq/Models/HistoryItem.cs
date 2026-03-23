using CommunityToolkit.Mvvm.ComponentModel;

namespace SoloReq.Models;

public partial class HistoryItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public HttpRequestModel Request { get; set; } = new();
    public HttpResponseModel Response { get; set; } = new();
    [ObservableProperty] private string? _name;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _editingName;
}
