using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Models;

namespace SoloReq.ViewModels;

public partial class UserAgentEditorViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _uaString = string.Empty;
    [ObservableProperty] private string _version = string.Empty;
    [ObservableProperty] private string _os = string.Empty;

    public bool IsEditMode { get; }

    public UserAgentEditorViewModel(UserAgentEntry? existing = null)
    {
        if (existing != null)
        {
            IsEditMode = true;
            Name = existing.Browser;
            UaString = existing.Ua;
            Version = existing.BrowserVersion;
            Os = existing.Os;
        }
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(UaString);

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(CanSave));
    partial void OnUaStringChanged(string value) => OnPropertyChanged(nameof(CanSave));

    public UserAgentEntry ToEntry() => new()
    {
        Browser = Name.Trim(),
        Ua = UaString.Trim(),
        BrowserVersion = Version.Trim(),
        Os = Os.Trim(),
        DeviceType = "custom",
        IsCustom = true
    };
}
