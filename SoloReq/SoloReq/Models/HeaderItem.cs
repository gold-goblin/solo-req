using CommunityToolkit.Mvvm.ComponentModel;

namespace SoloReq.Models;

public partial class HeaderItem : ObservableObject
{
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _key = string.Empty;
    [ObservableProperty] private string _value = string.Empty;
}
