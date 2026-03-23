using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SoloReq.Models;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class UserAgentPickerViewModel : ObservableObject
{
    private readonly UserAgentService _service;

    public UserAgentPickerViewModel(UserAgentService service)
    {
        _service = service;
        ApplyFilter();
    }

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedDeviceFilter = "Все";
    [ObservableProperty] private UserAgentEntry? _selectedEntry;
    [ObservableProperty] private bool _isCustomFilter;

    public ObservableCollection<UserAgentEntry> FilteredEntries { get; } = new();

    public string[] DeviceFilters { get; } = { "Все", "Desktop", "Mobile", "Пользовательские" };

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedDeviceFilterChanged(string value)
    {
        IsCustomFilter = value == "Пользовательские";
        ApplyFilter();
    }

    public void ApplyFilter()
    {
        FilteredEntries.Clear();
        var search = SearchText?.Trim() ?? string.Empty;

        foreach (var entry in _service.GetEntries())
        {
            if (SelectedDeviceFilter == "Desktop" && entry.DeviceType != "desktop") continue;
            if (SelectedDeviceFilter == "Mobile" && entry.DeviceType != "mobile") continue;
            if (SelectedDeviceFilter == "Пользовательские" && entry.DeviceType != "custom") continue;

            if (search.Length > 0)
            {
                var match = entry.Browser.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            entry.Os.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            entry.Ua.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }

            FilteredEntries.Add(entry);
        }
    }

    public void AddCustomEntry(UserAgentEntry entry)
    {
        _service.AddCustomEntry(entry);
        ApplyFilter();
    }

    public void EditCustomEntry(UserAgentEntry old, UserAgentEntry updated)
    {
        _service.UpdateCustomEntry(old, updated);
        ApplyFilter();
    }

    public void DeleteCustomEntry(UserAgentEntry entry)
    {
        _service.DeleteCustomEntry(entry);
        ApplyFilter();
    }
}
