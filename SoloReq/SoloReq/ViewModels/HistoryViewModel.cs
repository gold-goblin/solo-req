using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryService _historyService;
    private readonly List<HistoryItem> _allItems = new();

    public ObservableCollection<HistoryGroup> Groups { get; } = new();

    public Action<HistoryItem>? OnItemSelected { get; set; }
    public Action<HistoryItem>? OnRepeatRequested { get; set; }

    public HistoryViewModel(HistoryService historyService)
    {
        _historyService = historyService;
    }

    public void Initialize()
    {
        var loaded = _historyService.Load();
        _allItems.Clear();
        _allItems.AddRange(loaded);
        RebuildGroups();
    }

    public void AddItem(HttpRequestModel request, HttpResponseModel response)
    {
        var item = new HistoryItem
        {
            Request = request,
            Response = response,
            Timestamp = DateTime.Now
        };

        // Check for deduplication: same URL group + same method + same body + same headers + same auth
        var groupKey = HistoryGroup.NormalizeUrl(request.Url);
        var existing = _allItems.FirstOrDefault(e =>
            HistoryGroup.NormalizeUrl(e.Request.Url) == groupKey &&
            e.Request.Method == request.Method &&
            e.Request.Body == request.Body &&
            HeadersEqual(e.Request.Headers, request.Headers) &&
            AuthEqual(e.Request, request));

        if (existing != null)
        {
            // Overwrite existing entry
            existing.Response = response;
            existing.Timestamp = DateTime.Now;
        }
        else
        {
            _allItems.Insert(0, item);
        }

        RebuildGroups();
        ScheduleSave();
    }

    [RelayCommand]
    private void DeleteEntry(HistoryItem? item)
    {
        if (item == null) return;
        _allItems.Remove(item);
        RebuildGroups();
        ScheduleSave();
    }

    [RelayCommand]
    private void DeleteGroup(HistoryGroup? group)
    {
        if (group == null) return;
        foreach (var entry in group.Entries.ToList())
            _allItems.Remove(entry);
        RebuildGroups();
        ScheduleSave();
    }

    [RelayCommand]
    private void Repeat(HistoryItem? item)
    {
        if (item != null)
            OnRepeatRequested?.Invoke(item);
    }

    [RelayCommand]
    private void StartRenaming(HistoryItem? item)
    {
        if (item == null) return;
        item.EditingName = item.Name;
        item.IsEditing = true;
    }

    [RelayCommand]
    private void FinishRenaming(HistoryItem? item)
    {
        if (item == null) return;
        item.IsEditing = false;
        RenameEntry(item, item.EditingName);
    }

    [RelayCommand]
    private void CancelRenaming(HistoryItem? item)
    {
        if (item == null) return;
        item.IsEditing = false;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _allItems.Clear();
        Groups.Clear();
        ScheduleSave();
    }

    public void SelectItem(HistoryItem item)
    {
        OnItemSelected?.Invoke(item);
    }

    public void RenameEntry(HistoryItem item, string? name)
    {
        item.Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        ScheduleSave();
    }

    private void RebuildGroups()
    {
        Groups.Clear();

        var grouped = _allItems
            .GroupBy(i => HistoryGroup.NormalizeUrl(i.Request.Url))
            .OrderByDescending(g => g.Max(i => i.Timestamp));

        foreach (var g in grouped)
        {
            var first = g.First();
            var group = new HistoryGroup
            {
                GroupKey = g.Key,
                DisplayUrl = HistoryGroup.MakeDisplayUrl(first.Request.Url)
            };

            foreach (var item in g.OrderByDescending(i => i.Timestamp))
                group.Entries.Add(item);

            group.RefreshSortedEntries();
            Groups.Add(group);
        }
    }

    private void ScheduleSave()
    {
        _historyService.SaveDebounced(_allItems.ToList());
    }

    private static bool HeadersEqual(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var val) || val != kv.Value)
                return false;
        }
        return true;
    }

    private static bool AuthEqual(HttpRequestModel a, HttpRequestModel b)
    {
        return a.AuthType == b.AuthType &&
               a.AuthUsername == b.AuthUsername &&
               a.AuthPassword == b.AuthPassword &&
               a.AuthToken == b.AuthToken &&
               a.ApiKeyName == b.ApiKeyName &&
               a.ApiKeyValue == b.ApiKeyValue &&
               a.ApiKeyLocation == b.ApiKeyLocation &&
               a.CustomHeaderName == b.CustomHeaderName &&
               a.CustomHeaderValue == b.CustomHeaderValue;
    }
}
