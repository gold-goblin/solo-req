using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;

namespace SoloReq.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    [ObservableProperty] private HistoryItem? _selectedItem;

    public ObservableCollection<HistoryItem> Items { get; } = new();

    public Action<HistoryItem>? OnItemSelected { get; set; }
    public Action<HistoryItem>? OnRepeatRequested { get; set; }

    public void AddItem(HttpRequestModel request, HttpResponseModel response)
    {
        var item = new HistoryItem
        {
            Request = request,
            Response = response
        };
        Items.Insert(0, item);
    }

    partial void OnSelectedItemChanged(HistoryItem? value)
    {
        if (value != null)
            OnItemSelected?.Invoke(value);
    }

    [RelayCommand]
    private void Repeat(HistoryItem? item)
    {
        if (item != null)
            OnRepeatRequested?.Invoke(item);
    }

    [RelayCommand]
    private void ClearHistory() => Items.Clear();
}
