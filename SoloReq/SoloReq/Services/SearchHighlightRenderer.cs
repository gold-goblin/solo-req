using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace SoloReq.Services;

public class SearchHighlightRenderer : IBackgroundRenderer
{
    private Brush _matchBrush = null!;
    private Brush _currentMatchBrush = null!;

    public List<(int Offset, int Length)> Matches { get; set; } = new();
    public int CurrentMatchIndex { get; set; } = -1;

    public KnownLayer Layer => KnownLayer.Selection;

    public SearchHighlightRenderer()
    {
        UpdateBrushes(ThemeService.CurrentIsDark);
        ThemeService.ThemeChanged += isDark => UpdateBrushes(isDark);
    }

    private void UpdateBrushes(bool isDark)
    {
        if (isDark)
        {
            _matchBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x92, 0x48));
            _currentMatchBrush = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0x47, 0x9C));
        }
        else
        {
            _matchBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0x92, 0x48));
            _currentMatchBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0x47, 0x9C));
        }
        _matchBrush.Freeze();
        _currentMatchBrush.Freeze();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (Matches.Count == 0) return;

        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0) return;

        var viewStart = visualLines[0].FirstDocumentLine.Offset;
        var lastLine = visualLines[^1].LastDocumentLine;
        var viewEnd = lastLine.Offset + lastLine.Length;

        for (int i = 0; i < Matches.Count; i++)
        {
            var (offset, length) = Matches[i];
            if (offset + length < viewStart || offset > viewEnd)
                continue;

            var brush = i == CurrentMatchIndex ? _currentMatchBrush : _matchBrush;
            var builder = new BackgroundGeometryBuilder
            {
                CornerRadius = 2,
                AlignToWholePixels = true
            };
            builder.AddSegment(textView, new TextSegment { Offset = offset, Length = length });
            var geometry = builder.CreateGeometry();
            if (geometry != null)
                drawingContext.DrawGeometry(brush, null, geometry);
        }
    }

    private class TextSegment : ICSharpCode.AvalonEdit.Document.ISegment
    {
        public int Offset { get; init; }
        public int Length { get; init; }
        public int EndOffset => Offset + Length;
    }
}
