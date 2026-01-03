using Avalonia;
using Avalonia.Media;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages the viewport state including pan offset and zoom level.
/// </summary>
public class ViewportState
{
    private readonly FlowCanvasSettings _settings;

    public ViewportState(FlowCanvasSettings? settings = null)
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
    /// Event raised when the viewport changes.
    /// </summary>
    public event EventHandler? ViewportChanged;

    /// <summary>
    /// Sets the zoom level, optionally zooming towards a specific point.
    /// </summary>
    public void SetZoom(double newZoom, Point? zoomCenter = null)
    {
        newZoom = Math.Clamp(newZoom, _settings.MinZoom, _settings.MaxZoom);

        if (Math.Abs(newZoom - Zoom) < 0.001)
            return;

        if (zoomCenter.HasValue)
        {
            var zoomFactor = newZoom / Zoom;
            OffsetX = zoomCenter.Value.X - (zoomCenter.Value.X - OffsetX) * zoomFactor;
            OffsetY = zoomCenter.Value.Y - (zoomCenter.Value.Y - OffsetY) * zoomFactor;
        }

        Zoom = newZoom;
        OnViewportChanged();
    }

    /// <summary>
    /// Zooms in by one step.
    /// </summary>
    public void ZoomIn(Point? zoomCenter = null)
    {
        SetZoom(Zoom + _settings.ZoomStep, zoomCenter);
    }

    /// <summary>
    /// Zooms out by one step.
    /// </summary>
    public void ZoomOut(Point? zoomCenter = null)
    {
        SetZoom(Zoom - _settings.ZoomStep, zoomCenter);
    }

    /// <summary>
    /// Resets zoom to 100%.
    /// </summary>
    public void ResetZoom()
    {
        SetZoom(1.0);
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

        var zoomX = (viewSize.Width - padding * 2) / bounds.Width;
        var zoomY = (viewSize.Height - padding * 2) / bounds.Height;
        var newZoom = Math.Clamp(Math.Min(zoomX, zoomY), _settings.MinZoom, _settings.MaxZoom);

        Zoom = newZoom;

        // Center the bounds
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;

        OffsetX = viewSize.Width / 2 - centerX * newZoom;
        OffsetY = viewSize.Height / 2 - centerY * newZoom;

        OnViewportChanged();
    }

    /// <summary>
    /// Applies the viewport transforms to scale and translate transforms.
    /// </summary>
    public void ApplyToTransforms(ScaleTransform scale, TranslateTransform translate)
    {
        scale.ScaleX = Zoom;
        scale.ScaleY = Zoom;
        translate.X = OffsetX;
        translate.Y = OffsetY;
    }

    private void OnViewportChanged()
    {
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }
}
