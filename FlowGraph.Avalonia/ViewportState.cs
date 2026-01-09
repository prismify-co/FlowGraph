using Avalonia;
using Avalonia.Media;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages the viewport state including pan offset and zoom level.
/// </summary>
public class ViewportState
{
    private FlowCanvasSettings _settings;

    public ViewportState(FlowCanvasSettings? settings = null)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    /// <summary>
    /// Updates the settings used by this viewport.
    /// </summary>
    /// <param name="settings">The new settings to use.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    /// <summary>
    /// Current zoom level (1.0 = 100%).
    /// </summary>
    public double Zoom { get; private set; } = 1.0;

    /// <summary>
    /// Current X offset (pan).
    /// </summary>
    public double OffsetX { get; private set; }

    /// <summary>
    /// Current Y offset (pan).
    /// </summary>
    public double OffsetY { get; private set; }

    /// <summary>
    /// The current view size (set by the canvas).
    /// </summary>
    public Size ViewSize { get; private set; }

    /// <summary>
    /// Event raised when the viewport changes.
    /// </summary>
    public event EventHandler? ViewportChanged;

    /// <summary>
    /// Sets the view size (called when canvas size changes).
    /// </summary>
    public void SetViewSize(Size size)
    {
        ViewSize = size;
        OnViewportChanged();
    }

    /// <summary>
    /// Gets the visible area in canvas coordinates.
    /// Returns a Rect with Width=0 if view size is not set.
    /// </summary>
    public Rect GetVisibleRect()
    {
        if (ViewSize.Width <= 0 || ViewSize.Height <= 0)
            return new Rect(0, 0, 0, 0);

        var topLeft = ScreenToCanvas(new Point(0, 0));
        var bottomRight = ScreenToCanvas(new Point(ViewSize.Width, ViewSize.Height));

        return new Rect(topLeft, bottomRight);
    }

    /// <summary>
    /// Sets the zoom level, optionally zooming towards a specific point.
    /// If no zoom center is provided, zooms towards the center of the view.
    /// </summary>
    public void SetZoom(double newZoom, Point? zoomCenter = null)
    {
        newZoom = Math.Clamp(newZoom, _settings.MinZoom, _settings.MaxZoom);

        if (Math.Abs(newZoom - Zoom) < 0.001)
            return;

        // If no zoom center provided, use the center of the view
        var center = zoomCenter ?? new Point(ViewSize.Width / 2, ViewSize.Height / 2);

        // Adjust offset to keep the zoom center point stationary on screen
        var zoomFactor = newZoom / Zoom;
        OffsetX = center.X - (center.X - OffsetX) * zoomFactor;
        OffsetY = center.Y - (center.Y - OffsetY) * zoomFactor;

        Zoom = newZoom;
        ClampToBounds();
        OnViewportChanged();
    }

    /// <summary>
    /// Sets the zoom level directly without triggering change events.
    /// Used by animations to avoid recursive updates.
    /// </summary>
    internal void SetZoomDirect(double zoom)
    {
        Zoom = Math.Clamp(zoom, _settings.MinZoom, _settings.MaxZoom);
    }

    /// <summary>
    /// Sets the offset directly without triggering change events.
    /// Used by animations to avoid recursive updates.
    /// </summary>
    internal void SetOffsetDirect(double x, double y)
    {
        OffsetX = x;
        OffsetY = y;
    }

    /// <summary>
    /// Notifies that the viewport changed (used after animation updates).
    /// </summary>
    internal void NotifyViewportChanged()
    {
        OnViewportChanged();
    }

    /// <summary>
    /// Zooms in by one step, centered on the view.
    /// </summary>
    public void ZoomIn(Point? zoomCenter = null)
    {
        SetZoom(Zoom + _settings.ZoomStep, zoomCenter);
    }

    /// <summary>
    /// Zooms out by one step, centered on the view.
    /// </summary>
    public void ZoomOut(Point? zoomCenter = null)
    {
        SetZoom(Zoom - _settings.ZoomStep, zoomCenter);
    }

    /// <summary>
    /// Resets zoom to 100%, keeping the current view center.
    /// </summary>
    public void ResetZoom()
    {
        // Zoom towards center of view
        SetZoom(1.0, new Point(ViewSize.Width / 2, ViewSize.Height / 2));
    }

    /// <summary>
    /// Sets the pan offset.
    /// </summary>
    public void SetOffset(double x, double y)
    {
        OffsetX = x;
        OffsetY = y;
        OnViewportChanged();
    }

    /// <summary>
    /// Pans by a delta amount.
    /// </summary>
    public void Pan(double deltaX, double deltaY)
    {
        OffsetX += deltaX;
        OffsetY += deltaY;
        ClampToBounds();
        OnViewportChanged();
    }

    /// <summary>
    /// Centers the viewport on a specific canvas point.
    /// Does nothing if ViewSize is not set.
    /// </summary>
    public void CenterOn(Point canvasPoint)
    {
        // Don't center if view size is not set yet
        if (ViewSize.Width <= 0 || ViewSize.Height <= 0)
            return;

        OffsetX = ViewSize.Width / 2 - canvasPoint.X * Zoom;
        OffsetY = ViewSize.Height / 2 - canvasPoint.Y * Zoom;
        ClampToBounds();
        OnViewportChanged();
    }

    /// <summary>
    /// Transforms a screen point to canvas coordinates.
    /// </summary>
    public Point ScreenToCanvas(Point screenPoint)
    {
        return new Point(
            (screenPoint.X - OffsetX) / Zoom,
            (screenPoint.Y - OffsetY) / Zoom
        );
    }

    /// <summary>
    /// Transforms a canvas point to screen coordinates.
    /// </summary>
    public Point CanvasToScreen(Point canvasPoint)
    {
        return new Point(
            canvasPoint.X * Zoom + OffsetX,
            canvasPoint.Y * Zoom + OffsetY
        );
    }

    /// <summary>
    /// Fits the viewport to show a bounding box with padding.
    /// </summary>
    public void FitToBounds(Rect bounds, Size viewSize, double padding = 50)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        ViewSize = viewSize;

        var zoomX = (viewSize.Width - padding * 2) / bounds.Width;
        var zoomY = (viewSize.Height - padding * 2) / bounds.Height;
        var newZoom = Math.Clamp(Math.Min(zoomX, zoomY), _settings.MinZoom, _settings.MaxZoom);

        Zoom = newZoom;

        // Center the bounds
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;

        OffsetX = viewSize.Width / 2 - centerX * newZoom;
        OffsetY = viewSize.Height / 2 - centerY * newZoom;

        ClampToBounds();
        OnViewportChanged();
    }

    /// <summary>
    /// Applies the viewport transforms to a MatrixTransform.
    /// The matrix represents: screenPos = canvasPos * zoom + offset
    /// </summary>
    public void ApplyToTransforms(MatrixTransform transform)
    {
        // In Avalonia, Matrix constructor is: (m11, m12, m21, m22, m31, m32)
        // For scale + translate: m11=scaleX, m22=scaleY, m31=translateX, m32=translateY
        // m12=0, m21=0 (no skew)
        // 
        // This creates a matrix that transforms points as:
        // x' = x * m11 + y * m21 + m31 = x * zoom + offsetX
        // y' = x * m12 + y * m22 + m32 = y * zoom + offsetY
        var matrix = new Matrix(
            Zoom,    // m11 - scale X
            0,       // m12 
            0,       // m21
            Zoom,    // m22 - scale Y  
            OffsetX, // m31 - translate X (offsetX)
            OffsetY  // m32 - translate Y (offsetY)
        );
        transform.Matrix = matrix;
    }

    private void OnViewportChanged()
    {
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clamps the viewport offset to stay within configured bounds.
    /// </summary>
    private void ClampToBounds()
    {
        if (_settings.ViewportBounds is not Rect bounds)
            return;

        if (ViewSize.Width <= 0 || ViewSize.Height <= 0)
            return;

        var padding = _settings.ViewportBoundsPadding;

        // Calculate the visible area in canvas coordinates
        var visibleWidth = ViewSize.Width / Zoom;
        var visibleHeight = ViewSize.Height / Zoom;

        // Calculate min/max offsets to keep the bounds visible
        // The offset is: screenPos = canvasPos * zoom + offset
        // So: offset = screenPos - canvasPos * zoom

        // Maximum offset (left/top edge of bounds at left/top of screen with padding)
        var maxOffsetX = padding - bounds.Left * Zoom;
        var maxOffsetY = padding - bounds.Top * Zoom;

        // Minimum offset (right/bottom edge of bounds at right/bottom of screen with padding)
        var minOffsetX = ViewSize.Width - padding - bounds.Right * Zoom;
        var minOffsetY = ViewSize.Height - padding - bounds.Bottom * Zoom;

        // If bounds are smaller than view, center them instead
        if (bounds.Width * Zoom < ViewSize.Width - padding * 2)
        {
            var centerX = bounds.Left + bounds.Width / 2;
            OffsetX = ViewSize.Width / 2 - centerX * Zoom;
        }
        else
        {
            OffsetX = Math.Clamp(OffsetX, minOffsetX, maxOffsetX);
        }

        if (bounds.Height * Zoom < ViewSize.Height - padding * 2)
        {
            var centerY = bounds.Top + bounds.Height / 2;
            OffsetY = ViewSize.Height / 2 - centerY * Zoom;
        }
        else
        {
            OffsetY = Math.Clamp(OffsetY, minOffsetY, maxOffsetY);
        }
    }
}
