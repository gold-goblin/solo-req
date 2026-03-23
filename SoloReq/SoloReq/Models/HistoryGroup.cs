using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SoloReq.Models;

public partial class HistoryGroup : ObservableObject
{
    public string GroupKey { get; set; } = "";
    public string DisplayUrl { get; set; } = "";
    [ObservableProperty] private DateTime _lastTimestamp;
    public ObservableCollection<HistoryItem> Entries { get; } = new();
    public ObservableCollection<MethodGroup> Methods { get; } = new();
    public ObservableCollection<HistoryItem> SortedEntries { get; } = new();

    private static readonly Dictionary<string, int> MethodOrder = new()
    {
        ["GET"] = 0, ["POST"] = 1, ["PUT"] = 2, ["DELETE"] = 3, ["PATCH"] = 4,
        ["HEAD"] = 5, ["OPTIONS"] = 6
    };

    public void RefreshSortedEntries()
    {
        SortedEntries.Clear();
        var sorted = Entries
            .OrderBy(e => MethodOrder.GetValueOrDefault(e.Request.Method, 99));
        foreach (var item in sorted)
            SortedEntries.Add(item);

        LastTimestamp = Entries.Max(e => e.Timestamp);
    }

    public void RefreshMethods()
    {
        Methods.Clear();
        var grouped = Entries
            .GroupBy(e => e.Request.Method)
            .OrderByDescending(g => g.Max(e => e.Timestamp));

        foreach (var g in grouped)
        {
            var items = g.OrderByDescending(e => e.Timestamp).ToList();
            var latest = items[0];
            var mg = new MethodGroup
            {
                Method = g.Key,
                StatusCode = latest.Response.StatusCode,
                Count = items.Count,
                LastTimestamp = latest.Timestamp,
                LatestItem = latest
            };
            foreach (var item in items)
                mg.Items.Add(item);
            Methods.Add(mg);
        }

        LastTimestamp = Entries.Max(e => e.Timestamp);
    }

    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        var s = url.Trim();

        // Remove scheme
        if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            s = s[8..];
        else if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            s = s[7..];

        // Remove query string and fragment
        var qIdx = s.IndexOf('?');
        if (qIdx >= 0) s = s[..qIdx];
        var fIdx = s.IndexOf('#');
        if (fIdx >= 0) s = s[..fIdx];

        // Remove trailing slash
        s = s.TrimEnd('/');

        // Lowercase host part (before first /)
        var slashIdx = s.IndexOf('/');
        if (slashIdx >= 0)
            s = s[..slashIdx].ToLowerInvariant() + s[slashIdx..];
        else
            s = s.ToLowerInvariant();

        return s;
    }

    public static string MakeDisplayUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        var s = url.Trim();

        // Remove scheme
        if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            s = s[8..];
        else if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            s = s[7..];

        // Remove query string and fragment
        var qIdx = s.IndexOf('?');
        if (qIdx >= 0) s = s[..qIdx];
        var fIdx = s.IndexOf('#');
        if (fIdx >= 0) s = s[..fIdx];

        // Remove trailing slash
        s = s.TrimEnd('/');

        return s;
    }
}

public partial class MethodGroup : ObservableObject
{
    public string Method { get; set; } = "";
    [ObservableProperty] private int _statusCode;
    [ObservableProperty] private int _count;
    [ObservableProperty] private DateTime _lastTimestamp;
    public HistoryItem? LatestItem { get; set; }
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _editingName;
    public ObservableCollection<HistoryItem> Items { get; } = new();

    public bool HasMultipleItems => Count > 1;
    public string CountDisplay => $"(×{Count})";

    partial void OnCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasMultipleItems));
        OnPropertyChanged(nameof(CountDisplay));
    }
}
