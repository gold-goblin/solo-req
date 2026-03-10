using System.Reflection;
using System.Windows.Controls;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SoloReq.Services;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class ResponsePanel : UserControl
{
    private ResponseViewModel? _vm;
    private readonly IHighlightingDefinition? _jsonDark;
    private readonly IHighlightingDefinition? _jsonLight;
    private readonly IHighlightingDefinition? _xmlDark;
    private readonly IHighlightingDefinition? _xmlLight;
    private readonly IHighlightingDefinition? _htmlDark;
    private readonly IHighlightingDefinition? _htmlLight;

    public ResponsePanel()
    {
        InitializeComponent();
        _jsonDark = LoadXshd("SoloReq.Highlighting.json-dark.xshd");
        _jsonLight = LoadXshd("SoloReq.Highlighting.json-light.xshd");
        _xmlDark = LoadXshd("SoloReq.Highlighting.xml-dark.xshd");
        _xmlLight = LoadXshd("SoloReq.Highlighting.xml-light.xshd");
        _htmlDark = LoadXshd("SoloReq.Highlighting.html-dark.xshd");
        _htmlLight = LoadXshd("SoloReq.Highlighting.html-light.xshd");

        ThemeService.ThemeChanged += _ => UpdateHighlighting();

        DataContextChanged += (_, _) =>
        {
            if (_vm != null)
                _vm.PropertyChanged -= Vm_PropertyChanged;

            _vm = DataContext as ResponseViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += Vm_PropertyChanged;
                UpdateBody();
            }
        };
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResponseViewModel.Body))
            UpdateBody();
        else if (e.PropertyName == nameof(ResponseViewModel.ContentType))
            UpdateHighlighting();
    }

    private void UpdateBody()
    {
        if (_vm != null)
        {
            ResponseBodyEditor.Text = _vm.Body;
            UpdateHighlighting();
        }
    }

    private void UpdateHighlighting()
    {
        if (_vm == null) return;

        var ct = _vm.ContentType.ToLowerInvariant();
        var isDark = ThemeService.CurrentIsDark;

        if (ct.Contains("json"))
            ResponseBodyEditor.SyntaxHighlighting = isDark ? _jsonDark : _jsonLight;
        else if (ct.Contains("xml"))
            ResponseBodyEditor.SyntaxHighlighting = isDark ? _xmlDark : _xmlLight;
        else if (ct.Contains("html"))
            ResponseBodyEditor.SyntaxHighlighting = isDark ? _htmlDark : _htmlLight;
        else
            ResponseBodyEditor.SyntaxHighlighting = null;
    }

    private static IHighlightingDefinition? LoadXshd(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;
        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
