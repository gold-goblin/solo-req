using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;
using SoloReq.Services;

namespace SoloReq.ViewModels;

public partial class CookieManagerViewModel : ObservableObject
{
    private readonly CookieService _cookieService;

    public CookieManagerViewModel(CookieService cookieService)
    {
        _cookieService = cookieService;
        Refresh();
    }

    [ObservableProperty] private bool _autoSendEnabled;
    [ObservableProperty] private string _newName = string.Empty;
    [ObservableProperty] private string _newValue = string.Empty;
    [ObservableProperty] private string _newDomain = string.Empty;
    [ObservableProperty] private string _newPath = "/";

    public ObservableCollection<CookieItem> Cookies { get; } = new();

    partial void OnAutoSendEnabledChanged(bool value)
    {
        _cookieService.AutoSendEnabled = value;
    }

    public void Refresh()
    {
        AutoSendEnabled = _cookieService.AutoSendEnabled;
        Cookies.Clear();
        foreach (var c in _cookieService.Cookies)
            Cookies.Add(c);
    }

    [RelayCommand]
    private void AddCookie()
    {
        if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewDomain)) return;

        var cookie = new CookieItem
        {
            Name = NewName,
            Value = NewValue,
            Domain = NewDomain,
            Path = NewPath
        };
        _cookieService.AddCookie(cookie);
        NewName = string.Empty;
        NewValue = string.Empty;
        NewDomain = string.Empty;
        NewPath = "/";
        Refresh();
    }

    [RelayCommand]
    private void RemoveCookie(CookieItem? cookie)
    {
        if (cookie != null)
        {
            _cookieService.RemoveCookie(cookie);
            Refresh();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        _cookieService.Clear();
        Refresh();
    }
}
