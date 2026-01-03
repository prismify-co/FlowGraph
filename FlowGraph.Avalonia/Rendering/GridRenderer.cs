using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;

namespace FlowGraph.Avalonia;

/// <summary>
/// Renders the background grid for the canvas.
/// </summary>
public class GridRenderer
{
    private readonly FlowCanvasSettings _settings;

    public GridRenderer(FlowCanvasSettings? settings = null)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    /// <summary>
    /// Renders the grid onto the specified canvas.
    /// </summary>
    public void Render(Canvas canvas, Size viewSize, ViewportState viewport, IBrush gridBrush)
    {
        canvas.Children.Clear();

        if (viewSize.Width <= 0 || viewSize.Height <= 0)
            return;

        var spacing = _settings.GridSpacing;
        var zoom = viewport.Zoom;
        var offsetX = viewport.OffsetX;
        var offsetY = viewport.OffsetY;

        // Calculate visible area in canvas coordinates
        var startX = -offsetX / zoom - spacing;
        var startY = -offsetY / zoom - spacing;
        var endX = (viewSize.Width - offsetX) / zoom + spacing;
        var endY = (viewSize.Height - offsetY) / zoom + spacing;

        // Snap to grid
        startX = Math.Floor(startX / spacing) * spacing;
        startY = Math.Floor(startY / spacing) * spacing;

        // Adjust dot size based on zoom (keep dots visible but not too large)
        var dotSize = Math.Max(_settings.GridDotSize / zoom, 1);

        // Draw grid dots
        for (double x = startX; x < endX; x += spacing)
        {
            for (double y = startY; y < endY; y += spacing)
            {
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = gridBrush
                };
                Canvas.SetLeft(dot, x - dotSize / 2);
                Canvas.SetTop(dot, y - dotSize / 2);
                canvas.Children.Add(dot);
            }
        }
    }
}
