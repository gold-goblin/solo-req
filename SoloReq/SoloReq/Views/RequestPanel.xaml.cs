using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using SoloReq.Models;
using SoloReq.Services;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class RequestPanel : UserControl
{
    private RequestViewModel? _vm;
    private readonly IHighlightingDefinition? _jsonDark;
    private readonly IHighlightingDefinition? _jsonLight;

    public RequestPanel()
    {
        InitializeComponent();
        _jsonDark = LoadXshd("SoloReq.Highlighting.json-dark.xshd");
        _jsonLight = LoadXshd("SoloReq.Highlighting.json-light.xshd");
        ApplyHighlighting();
        ThemeService.ThemeChanged += _ => ApplyHighlighting();

        DataContextChanged += (_, _) =>
        {
            if (_vm != null)
                _vm.PropertyChanged -= Vm_PropertyChanged;

            _vm = DataContext as RequestViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += Vm_PropertyChanged;
                BodyEditor.Text = _vm.Body;
            }
        };

        BodyEditor.TextChanged += (_, _) =>
        {
            if (_vm != null && _vm.Body != BodyEditor.Text)
                _vm.Body = BodyEditor.Text;
        };
    }

    private void ApplyHighlighting()
    {
        BodyEditor.SyntaxHighlighting = ThemeService.CurrentIsDark ? _jsonDark : _jsonLight;
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RequestViewModel.Body) && _vm != null && BodyEditor.Text != _vm.Body)
        {
            BodyEditor.Text = _vm.Body;
        }
    }

    private static IHighlightingDefinition? LoadXshd(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;
        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    public void FocusUrl()
    {
        UrlTextBox.Focus();
        UrlTextBox.SelectAll();
    }

    private void OpenUserAgentPicker(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: HeaderItem headerItem })
            return;

        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is not MainViewModel mainVm)
            return;

        var vm = new UserAgentPickerViewModel(mainVm.UserAgentService);
        var picker = new UserAgentPickerWindow
        {
            DataContext = vm,
            Owner = mainWindow
        };

        if (picker.ShowDialog() == true && picker.SelectedUserAgent != null)
            headerItem.Value = picker.SelectedUserAgent.Ua;
    }
}
