using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FlowGraph.Avalonia;

/// <summary>
/// Renders the background grid for the canvas using efficient drawing.
/// Uses transform-based panning for O(1) pan performance.
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
/// Uses transform-based panning: dots are rendered with padding, then translated.
/// Only re-renders when zoom changes or pan exceeds the padding bounds.
/// </summary>
internal class GridDrawingControl : Control
{
    private FlowCanvasSettings _settings;
    private ViewportState? _viewport;
    private IBrush? _gridBrush;

    // Transform for O(1) pan
    private readonly TranslateTransform _panTransform;

    // Base offset used when dots were last rendered
    private double _baseOffsetX;
    private double _baseOffsetY;
    private double _baseZoom;
    private Size _baseSize;

    // Padding (in screen pixels) - render extra dots beyond viewport for smooth panning
    private const double PanPadding = 200;

    public GridDrawingControl(FlowCanvasSettings settings)
    {
        _settings = settings;
        IsHitTestVisible = false;

        // Set up the pan transform
        _panTransform = new TranslateTransform();
        RenderTransform = _panTransform;
    }

    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings;
        ForceFullRender();
    }

    public void UpdateGrid(ViewportState viewport, IBrush gridBrush)
    {
        _viewport = viewport;

        if (_gridBrush != gridBrush)
        {
            _gridBrush = gridBrush;
            ForceFullRender();
            return;
        }

        // Check if we need a full re-render or can just update the transform
        if (NeedsFullRender(viewport))
        {
            // Full re-render needed (zoom changed or pan exceeded padding)
            ForceFullRender();
        }
        else
        {
            // O(1) pan: just update the transform offset
            var deltaX = viewport.OffsetX - _baseOffsetX;
            var deltaY = viewport.OffsetY - _baseOffsetY;
            _panTransform.X = deltaX;
            _panTransform.Y = deltaY;
        }
    }

    private bool NeedsFullRender(ViewportState viewport)
    {
        // Always need full render on first draw
        if (_baseZoom == 0)
            return true;

        // Zoom changed - dot positions and sizes change
        if (Math.Abs(_baseZoom - viewport.Zoom) > 0.001)
            return true;

        // Size changed significantly
        if (Math.Abs(_baseSize.Width - Bounds.Width) > 1 ||
            Math.Abs(_baseSize.Height - Bounds.Height) > 1)
            return true;

        // Pan exceeded padding bounds - need to render more dots
        var deltaX = Math.Abs(viewport.OffsetX - _baseOffsetX);
        var deltaY = Math.Abs(viewport.OffsetY - _baseOffsetY);
        if (deltaX > PanPadding || deltaY > PanPadding)
            return true;

        return false;
    }

    private void ForceFullRender()
    {
        // Reset transform
        _panTransform.X = 0;
        _panTransform.Y = 0;

        // Store current offset as base
        if (_viewport != null)
        {
            _baseOffsetX = _viewport.OffsetX;
            _baseOffsetY = _viewport.OffsetY;
            _baseZoom = _viewport.Zoom;
        }
        _baseSize = Bounds.Size;

        InvalidateVisual();
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

        // Use base offset for rendering (transform handles the delta)
        var offsetX = _baseOffsetX;
        var offsetY = _baseOffsetY;

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
        // Add padding for smooth pan without re-render
        var paddedWidth = bounds.Width + PanPadding * 2;
        var paddedHeight = bounds.Height + PanPadding * 2;

        var startX = Math.Floor((-offsetX - PanPadding) / zoom / spacing) * spacing;
        var startY = Math.Floor((-offsetY - PanPadding) / zoom / spacing) * spacing;
        var endX = Math.Ceiling((paddedWidth - offsetX - PanPadding) / zoom / spacing) * spacing;
        var endY = Math.Ceiling((paddedHeight - offsetY - PanPadding) / zoom / spacing) * spacing;

        // Limit the number of dots to prevent performance issues
        var maxDotsPerAxis = 250; // Slightly higher to account for padding
        var xCount = (int)((endX - startX) / spacing);
        var yCount = (int)((endY - startY) / spacing);

        if (xCount > maxDotsPerAxis || yCount > maxDotsPerAxis)
        {
            // Increase spacing to reduce dot count
            var factor = Math.Max(xCount, yCount) / (double)maxDotsPerAxis;
            spacing *= Math.Ceiling(factor);
            screenSpacing = spacing * zoom;
            startX = Math.Floor((-offsetX - PanPadding) / zoom / spacing) * spacing;
            startY = Math.Floor((-offsetY - PanPadding) / zoom / spacing) * spacing;
        }

        // Draw dots using DrawingContext (much faster than UI elements)
        for (var x = startX; x <= endX; x += spacing)
        {
            for (var y = startY; y <= endY; y += spacing)
            {
                // Transform to screen coordinates using base offset
                var screenX = x * zoom + offsetX;
                var screenY = y * zoom + offsetY;

                // Skip dots way outside visible bounds (with padding margin)
                if (screenX < -PanPadding - dotRadius || screenX > bounds.Width + PanPadding + dotRadius ||
                    screenY < -PanPadding - dotRadius || screenY > bounds.Height + PanPadding + dotRadius)
                    continue;

                // Draw the dot
                context.DrawEllipse(_gridBrush, null, new Point(screenX, screenY), dotRadius, dotRadius);
            }
        }
    }
}
