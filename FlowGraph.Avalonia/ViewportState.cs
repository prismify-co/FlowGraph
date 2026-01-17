using Avalonia;
using Avalonia.Media;
using FlowGraph.Core.Rendering;
using CorePoint = FlowGraph.Core.Point;
using CoreRect = FlowGraph.Core.Elements.Rect;
using CoreSize = FlowGraph.Core.Elements.Size;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages the viewport state including pan offset and zoom level.
/// Implements <see cref="IViewportState"/> for backend-agnostic viewport control.
/// </summary>
public class ViewportState : IViewportState
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

    /// <inheritdoc />
    CoreSize IReadOnlyViewportState.ViewSize => new(ViewSize.Width, ViewSize.Height);

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

        var topLeft = ViewportToCanvas(new Point(0, 0));
        var bottomRight = ViewportToCanvas(new Point(ViewSize.Width, ViewSize.Height));

        return new Rect(topLeft, bottomRight);
    }

    /// <inheritdoc />
    CoreRect IReadOnlyViewportState.GetVisibleCanvasRect()
    {
        var rect = GetVisibleRect();
        return new CoreRect(rect.X, rect.Y, rect.Width, rect.Height);
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
    /// Transforms a viewport point to canvas coordinates.
    /// </summary>
    public Point ViewportToCanvas(Point viewportPoint)
    {
        return new Point(
            (viewportPoint.X - OffsetX) / Zoom,
            (viewportPoint.Y - OffsetY) / Zoom
        );
    }

    /// <inheritdoc />
    CorePoint ICoordinateTransformer.ViewportToCanvas(double viewportX, double viewportY)
    {
        return new CorePoint(
            (viewportX - OffsetX) / Zoom,
            (viewportY - OffsetY) / Zoom
        );
    }

    /// <summary>
    /// Transforms a canvas point to viewport coordinates.
    /// </summary>
    public Point CanvasToViewport(Point canvasPoint)
    {
        return new Point(
            canvasPoint.X * Zoom + OffsetX,
            canvasPoint.Y * Zoom + OffsetY
        );
    }

    /// <inheritdoc />
    CorePoint ICoordinateTransformer.CanvasToViewport(double canvasX, double canvasY)
    {
        return new CorePoint(
            canvasX * Zoom + OffsetX,
            canvasY * Zoom + OffsetY
        );
    }

    /// <inheritdoc />
    public CorePoint ViewportToCanvasDelta(double viewportDeltaX, double viewportDeltaY)
    {
        return new CorePoint(viewportDeltaX / Zoom, viewportDeltaY / Zoom);
    }

    /// <inheritdoc />
    public CorePoint CanvasToViewportDelta(double canvasDeltaX, double canvasDeltaY)
    {
        return new CorePoint(canvasDeltaX * Zoom, canvasDeltaY * Zoom);
    }

    #region Obsolete methods for backward compatibility

    /// <summary>
    /// Transforms a screen point to canvas coordinates.
    /// </summary>
    [Obsolete("Use ViewportToCanvas instead.")]
    public Point ScreenToCanvas(Point screenPoint) => ViewportToCanvas(screenPoint);

    /// <summary>
    /// Transforms a canvas point to screen coordinates.
    /// </summary>
    [Obsolete("Use CanvasToViewport instead.")]
    public Point CanvasToScreen(Point canvasPoint) => CanvasToViewport(canvasPoint);

    /// <inheritdoc />
    [Obsolete("Use ViewportToCanvasDelta instead.")]
    public CorePoint ScreenToCanvasDelta(double screenDeltaX, double screenDeltaY)
        => ViewportToCanvasDelta(screenDeltaX, screenDeltaY);

    /// <inheritdoc />
    [Obsolete("Use CanvasToViewportDelta instead.")]
    public CorePoint CanvasToScreenDelta(double canvasDeltaX, double canvasDeltaY)
        => CanvasToViewportDelta(canvasDeltaX, canvasDeltaY);

    #endregion

    /// <summary>
    /// Fits the viewport to show a bounding box with padding.
    /// The padding is the minimum margin on each side in screen pixels.
    /// For better visual results, the actual margin will be at least 15% of the viewport dimension.
    /// </summary>
    public void FitToBounds(Rect bounds, Size viewSize, double padding = 50)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        ViewSize = viewSize;

        // Use the larger of fixed padding or 15% of viewport size for better visual margins
        var effectivePaddingX = Math.Max(padding, viewSize.Width * 0.15);
        var effectivePaddingY = Math.Max(padding, viewSize.Height * 0.15);

        var zoomX = (viewSize.Width - effectivePaddingX * 2) / bounds.Width;
        var zoomY = (viewSize.Height - effectivePaddingY * 2) / bounds.Height;
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

    #region IViewportState explicit implementations

    /// <inheritdoc />
    void IViewportState.SetZoom(double zoom, CorePoint? zoomCenter)
    {
        Point? avaloniaCenter = zoomCenter.HasValue
            ? new Point(zoomCenter.Value.X, zoomCenter.Value.Y)
            : null;
        SetZoom(zoom, avaloniaCenter);
    }

    /// <inheritdoc />
    void IViewportState.CenterOn(CorePoint canvasPoint)
    {
        CenterOn(new Point(canvasPoint.X, canvasPoint.Y));
    }

    /// <inheritdoc />
    void IViewportState.FitToBounds(CoreRect canvasBounds, double padding)
    {
        var bounds = new Rect(canvasBounds.X, canvasBounds.Y, canvasBounds.Width, canvasBounds.Height);
        FitToBounds(bounds, ViewSize, padding);
    }

    /// <inheritdoc />
    public void Reset()
    {
        Zoom = 1.0;
        OffsetX = 0;
        OffsetY = 0;
        OnViewportChanged();
    }

    #endregion

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
