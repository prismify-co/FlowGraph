using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FlowGraph.Avalonia;

/// <summary>
/// Renders the background grid for the canvas using efficient drawing.
/// </summary>
public class GridRenderer
{
    private FlowCanvasSettings _settings;
    private GridDrawingControl? _gridControl;

    public GridRenderer(FlowCanvasSettings? settings = null)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    /// <summary>
    /// Updates the settings used by this renderer.
    /// </summary>
    /// <param name="settings">The new settings to use.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
        _gridControl?.UpdateSettings(_settings);
    }

    /// <summary>
    /// Renders the grid onto the specified canvas.
    /// </summary>
    public void Render(Canvas canvas, Size viewSize, ViewportState viewport, IBrush gridBrush)
    {
        if (viewSize.Width <= 0 || viewSize.Height <= 0)
            return;

        // Create or update the grid control
        if (_gridControl == null)
        {
            _gridControl = new GridDrawingControl(_settings);
            canvas.Children.Add(_gridControl);
        }
        else if (!canvas.Children.Contains(_gridControl))
        {
            canvas.Children.Clear();
            canvas.Children.Add(_gridControl);
        }

        // Update the grid control with current state
        _gridControl.Width = viewSize.Width;
        _gridControl.Height = viewSize.Height;
        _gridControl.UpdateGrid(viewport, gridBrush);
    }
}

/// <summary>
/// A control that efficiently renders the grid using DrawingContext.
/// </summary>
internal class GridDrawingControl : Control
{
    private FlowCanvasSettings _settings;
    private ViewportState? _viewport;
    private IBrush? _gridBrush;
    private Pen? _gridPen;

    // Cached geometry for performance
    private StreamGeometry? _cachedGeometry;
    private double _cachedZoom;
    private double _cachedOffsetX;
    private double _cachedOffsetY;
    private Size _cachedSize;

    public GridDrawingControl(FlowCanvasSettings settings)
    {
        _settings = settings;
        IsHitTestVisible = false;
    }

    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings;
        InvalidateGeometryCache();
        InvalidateVisual();
    }

    public void UpdateGrid(ViewportState viewport, IBrush gridBrush)
    {
        _viewport = viewport;

        if (_gridBrush != gridBrush)
        {
            _gridBrush = gridBrush;
            _gridPen = new Pen(gridBrush, 1);
            InvalidateGeometryCache();
        }

        // Check if we need to regenerate geometry
        if (NeedsGeometryUpdate(viewport))
        {
            InvalidateGeometryCache();
        }

        InvalidateVisual();
    }

    private bool NeedsGeometryUpdate(ViewportState viewport)
    {
        // Regenerate if zoom or size changed significantly
        // Small pan movements don't need full regeneration due to tiling
        return _cachedGeometry == null ||
               Math.Abs(_cachedZoom - viewport.Zoom) > 0.001 ||
               Math.Abs(_cachedSize.Width - Bounds.Width) > 1 ||
               Math.Abs(_cachedSize.Height - Bounds.Height) > 1;
    }

    private void InvalidateGeometryCache()
    {
        _cachedGeometry = null;
    }

    public override void Render(DrawingContext context)
    {
        if (_viewport == null || _gridBrush == null)
            return;

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var spacing = _settings.GridSpacing;
        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        // Calculate effective spacing in screen coordinates
        var screenSpacing = spacing * zoom;

        // Skip rendering if dots would be too dense (< 4 pixels apart)
        if (screenSpacing < 4)
        {
            // Draw a subtle background instead when too zoomed out
            var fadedBrush = new SolidColorBrush(((ISolidColorBrush)_gridBrush).Color, 0.3);
            context.FillRectangle(fadedBrush, bounds);
            return;
        }

        // Calculate dot size in screen coordinates
        var dotRadius = Math.Max(_settings.GridDotSize * zoom / 2, 0.5);

        // Calculate visible grid range in canvas coordinates
        var startX = Math.Floor(-offsetX / zoom / spacing) * spacing;
        var startY = Math.Floor(-offsetY / zoom / spacing) * spacing;
        var endX = Math.Ceiling((bounds.Width - offsetX) / zoom / spacing) * spacing;
        var endY = Math.Ceiling((bounds.Height - offsetY) / zoom / spacing) * spacing;

        // Limit the number of dots to prevent performance issues
        var maxDotsPerAxis = 200;
        var xCount = (int)((endX - startX) / spacing);
        var yCount = (int)((endY - startY) / spacing);

        if (xCount > maxDotsPerAxis || yCount > maxDotsPerAxis)
        {
            // Increase spacing to reduce dot count
            var factor = Math.Max(xCount, yCount) / (double)maxDotsPerAxis;
            spacing *= Math.Ceiling(factor);
            screenSpacing = spacing * zoom;
            startX = Math.Floor(-offsetX / zoom / spacing) * spacing;
            startY = Math.Floor(-offsetY / zoom / spacing) * spacing;
        }

        // Draw dots using DrawingContext (much faster than UI elements)
        for (var x = startX; x <= endX; x += spacing)
        {
            for (var y = startY; y <= endY; y += spacing)
            {
                // Transform to screen coordinates
                var screenX = x * zoom + offsetX;
                var screenY = y * zoom + offsetY;

                // Skip dots outside visible bounds
                if (screenX < -dotRadius || screenX > bounds.Width + dotRadius ||
                    screenY < -dotRadius || screenY > bounds.Height + dotRadius)
                    continue;

                // Draw the dot
                context.DrawEllipse(_gridBrush, null, new Point(screenX, screenY), dotRadius, dotRadius);
            }
        }

        // Cache state for comparison
        _cachedZoom = zoom;
        _cachedOffsetX = offsetX;
        _cachedOffsetY = offsetY;
        _cachedSize = bounds.Size;
    }
}
