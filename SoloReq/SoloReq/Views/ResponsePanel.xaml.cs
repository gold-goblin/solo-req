using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SoloReq.Services;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class ResponsePanel : UserControl
{
    private ResponseViewModel? _vm;
    private readonly SearchHighlightRenderer _searchRenderer;
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

        _searchRenderer = new SearchHighlightRenderer();
        ResponseBodyEditor.TextArea.TextView.BackgroundRenderers.Add(_searchRenderer);

        ThemeService.ThemeChanged += _ =>
        {
            UpdateHighlighting();
            ResponseBodyEditor.TextArea.TextView.InvalidateLayer(_searchRenderer.Layer);
        };

        SearchTextBox.PreviewKeyDown += SearchTextBox_PreviewKeyDown;

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
        switch (e.PropertyName)
        {
            case nameof(ResponseViewModel.Body):
                UpdateBody();
                break;
            case nameof(ResponseViewModel.ContentType):
                UpdateHighlighting();
                break;
            case nameof(ResponseViewModel.SearchText):
            case nameof(ResponseViewModel.SearchCaseSensitive):
                PerformSearch();
                break;
            case nameof(ResponseViewModel.CurrentMatchIndex):
                UpdateRendererAndScroll();
                break;
            case nameof(ResponseViewModel.IsSearchVisible):
                OnSearchVisibilityChanged();
                break;
        }
    }

    private void UpdateBody()
    {
        if (_vm != null)
        {
            ResponseBodyEditor.Text = _vm.Body;
            UpdateHighlighting();
            if (_vm.IsSearchVisible && !string.IsNullOrEmpty(_vm.SearchText))
                PerformSearch();
        }
    }

    private void PerformSearch()
    {
        if (_vm == null) return;

        _searchRenderer.Matches.Clear();
        _searchRenderer.CurrentMatchIndex = -1;

        var text = ResponseBodyEditor.Text;
        var search = _vm.SearchText;

        if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(text))
        {
            _vm.TotalMatches = 0;
            _vm.CurrentMatchIndex = 0;
            ResponseBodyEditor.TextArea.TextView.InvalidateLayer(_searchRenderer.Layer);
            return;
        }

        var comparison = _vm.SearchCaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var matches = new List<(int Offset, int Length)>();
        int pos = 0;
        while (pos < text.Length)
        {
            int idx = text.IndexOf(search, pos, comparison);
            if (idx < 0) break;
            matches.Add((idx, search.Length));
            pos = idx + 1;
        }

        _searchRenderer.Matches = matches;
        _vm.TotalMatches = matches.Count;
        _vm.CurrentMatchIndex = matches.Count > 0 ? 0 : 0;

        if (matches.Count > 0)
        {
            _searchRenderer.CurrentMatchIndex = 0;
            ScrollToMatch(0);
        }

        ResponseBodyEditor.TextArea.TextView.InvalidateLayer(_searchRenderer.Layer);
    }

    private void UpdateRendererAndScroll()
    {
        if (_vm == null || _searchRenderer.Matches.Count == 0) return;

        _searchRenderer.CurrentMatchIndex = _vm.CurrentMatchIndex;
        ScrollToMatch(_vm.CurrentMatchIndex);
        ResponseBodyEditor.TextArea.TextView.InvalidateLayer(_searchRenderer.Layer);
    }

    private void ScrollToMatch(int index)
    {
        if (index < 0 || index >= _searchRenderer.Matches.Count) return;

        var (offset, _) = _searchRenderer.Matches[index];
        var location = ResponseBodyEditor.Document.GetLocation(offset);
        ResponseBodyEditor.ScrollTo(location.Line, location.Column);
    }

    private void OnSearchVisibilityChanged()
    {
        if (_vm == null) return;

        if (_vm.IsSearchVisible)
        {
            Dispatcher.BeginInvoke(() =>
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
        else
        {
            _searchRenderer.Matches.Clear();
            _searchRenderer.CurrentMatchIndex = -1;
            ResponseBodyEditor.TextArea.TextView.InvalidateLayer(_searchRenderer.Layer);
        }
    }

    private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                _vm.PreviousMatchCommand.Execute(null);
            else
                _vm.NextMatchCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _vm.CloseSearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    public void OpenSearch()
    {
        if (_vm != null)
        {
            _vm.IsSearchVisible = true;
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
