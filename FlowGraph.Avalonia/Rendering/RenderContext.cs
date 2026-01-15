using Avalonia;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Shared rendering context containing settings, viewport state, and coordinate transformation utilities.
/// Used by all visual managers to ensure consistent rendering behavior.
/// 
/// <para><b>TRANSFORM-BASED RENDERING (Phase 2):</b></para>
/// <para>
/// Renderers create visuals at "logical size" (unscaled). The MatrixTransform on MainCanvas
/// handles all zoom/pan transformations. This enables O(1) zoom operations.
/// </para>
/// <list type="bullet">
/// <item><see cref="Scale"/> always returns 1.0 - use for visual sizing</item>
/// <item><see cref="ViewportZoom"/> returns actual zoom - use for calculations</item>
/// <item><see cref="InverseScale"/> returns 1/zoom - use for constant-size elements</item>
/// </list>
/// </summary>
public class RenderContext
{
    private FlowCanvasSettings _settings;
    private ViewportState? _viewport;

    /// <summary>
    /// Creates a new render context with the specified settings.
    /// </summary>
    /// <param name="settings">Canvas settings. If null, default settings are used.</param>
    public RenderContext(FlowCanvasSettings? settings = null)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    /// <summary>
    /// Gets the canvas settings.
    /// </summary>
    public FlowCanvasSettings Settings => _settings;

    /// <summary>
    /// Updates the settings. Call this when FlowCanvasSettings property changes.
    /// </summary>
    /// <param name="settings">The new settings to use.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    /// <summary>
    /// Gets the current viewport state.
    /// </summary>
    public ViewportState? Viewport => _viewport;

    /// <summary>
    /// Gets the logical scale factor for creating visuals.
    /// 
    /// <para><b>ALWAYS RETURNS 1.0</b></para>
    /// <para>
    /// In transform-based rendering, visuals are created at logical size (unscaled).
    /// The MatrixTransform on MainCanvas handles zoom. This enables O(1) zoom operations
    /// since visuals don't need to be recreated when zoom changes.
    /// </para>
    /// 
    /// <para><b>Usage:</b> Use for visual sizing (Width, Height, FontSize, etc.)</para>
    /// <para><b>For actual zoom:</b> Use <see cref="ViewportZoom"/> instead.</para>
    /// </summary>
    /// <remarks>
    /// This is a breaking change from the previous behavior where Scale returned viewport.Zoom.
    /// Custom renderers that used context.Scale for calculations should switch to ViewportZoom.
    /// </remarks>
    public double Scale => 1.0;

    /// <summary>
    /// Gets the actual viewport zoom level.
    /// 
    /// <para><b>Usage:</b></para>
    /// <list type="bullet">
    /// <item>Calculations that need to know the current zoom (e.g., level-of-detail decisions)</item>
    /// <item>Hit testing calculations</item>
    /// <item>Computing inverse scale for constant-size elements</item>
    /// </list>
    /// 
    /// <para><b>DO NOT USE</b> for visual sizing - use <see cref="Scale"/> instead.</para>
    /// </summary>
    public double ViewportZoom => _viewport?.Zoom ?? 1.0;

    /// <summary>
    /// Gets the inverse scale factor for elements that should stay constant screen size.
    /// 
    /// <para><b>Usage:</b> Apply as a ScaleTransform to elements like:</para>
    /// <list type="bullet">
    /// <item>Resize handles (should stay same size regardless of zoom)</item>
    /// <item>Edge endpoint handles</item>
    /// <item>Selection indicators</item>
    /// </list>
    /// 
    /// <example>
    /// <code>
    /// handle.RenderTransform = new ScaleTransform(context.InverseScale, context.InverseScale);
    /// handle.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
    /// </code>
    /// </example>
    /// </summary>
    public double InverseScale => 1.0 / ViewportZoom;

    /// <summary>
    /// Sets the viewport state for coordinate transformations.
    /// </summary>
    /// <param name="viewport">The viewport state to use.</param>
    public void SetViewport(ViewportState? viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Gets the visible area in canvas coordinates, expanded by the virtualization buffer.
    /// Used for culling nodes/edges outside the viewport.
    /// </summary>
    /// <returns>The visible rect with buffer, or an infinite rect if no viewport is set or view size is 0.</returns>
    public AvaloniaRect GetVisibleBoundsWithBuffer()
    {
        if (_viewport == null)
        {
            // No viewport = render everything
            return new AvaloniaRect(double.MinValue / 2, double.MinValue / 2, double.MaxValue, double.MaxValue);
        }

        var visibleRect = _viewport.GetVisibleRect();

        // If view size is not set (Width=0), render everything to avoid incorrectly culling nodes
        if (visibleRect.Width <= 0 || visibleRect.Height <= 0)
        {
            return new AvaloniaRect(double.MinValue / 2, double.MinValue / 2, double.MaxValue, double.MaxValue);
        }

        var buffer = _settings.VirtualizationBuffer;

        var result = new AvaloniaRect(
            visibleRect.X - buffer,
            visibleRect.Y - buffer,
            visibleRect.Width + buffer * 2,
            visibleRect.Height + buffer * 2);

        return result;
    }

    /// <summary>
    /// Checks if a rectangular area intersects with the visible viewport (with buffer).
    /// </summary>
    /// <param name="x">X coordinate of the rect.</param>
    /// <param name="y">Y coordinate of the rect.</param>
    /// <param name="width">Width of the rect.</param>
    /// <param name="height">Height of the rect.</param>
    /// <returns>True if the rect intersects the visible area.</returns>
    public bool IsInVisibleBounds(double x, double y, double width, double height)
    {
        if (!_settings.EnableVirtualization)
            return true;

        // If no viewport or view size not set, render everything
        if (_viewport == null)
            return true;

        var visibleRect = _viewport.GetVisibleRect();
        if (visibleRect.Width <= 0 || visibleRect.Height <= 0)
            return true;

        var buffer = _settings.VirtualizationBuffer;
        var visibleBounds = new AvaloniaRect(
            visibleRect.X - buffer,
            visibleRect.Y - buffer,
            visibleRect.Width + buffer * 2,
            visibleRect.Height + buffer * 2);

        var nodeRect = new AvaloniaRect(x, y, width, height);

        return visibleBounds.Intersects(nodeRect);
    }

    /// <summary>
    /// Transforms a canvas coordinate to screen coordinate.
    /// 
    /// <para><b>USAGE GUIDANCE:</b></para>
    /// <list type="bullet">
    /// <item><b>DO NOT USE</b> when positioning visual elements with Canvas.SetLeft/SetTop - 
    /// elements on MainCanvas should be positioned in canvas coordinates, the MatrixTransform handles viewport conversion</item>
    /// <item><b>DO USE</b> when rendering with DrawingContext (backgrounds, direct rendering) - 
    /// DrawingContext needs screen coordinates</item>
    /// <item><b>DO USE</b> for calculations that need to know screen-space dimensions (hit areas, label placement)</item>
    /// </list>
    /// 
    /// <para><b>CORRECT:</b> <c>drawingContext.DrawRectangle(brush, pen, CanvasToScreen(rect))</c></para>
    /// <para><b>INCORRECT:</b> <c>Canvas.SetLeft(element, CanvasToScreen(x, y).X)</c> - causes double transform!</para>
    /// </summary>
    /// <param name="canvasX">X coordinate in canvas space.</param>
    /// <param name="canvasY">Y coordinate in canvas space.</param>
    /// <returns>The corresponding screen coordinate.</returns>
    public AvaloniaPoint CanvasToScreen(double canvasX, double canvasY)
    {
        if (_viewport == null)
        {
            if (_settings.DebugCoordinateTransforms)
            {
                System.Diagnostics.Debug.WriteLine($"RenderContext.CanvasToScreen: NO VIEWPORT! Returning ({canvasX}, {canvasY})");
            }
            return new AvaloniaPoint(canvasX, canvasY);
        }

        var result = _viewport.CanvasToScreen(new AvaloniaPoint(canvasX, canvasY));

        if (_settings.DebugCoordinateTransforms)
        {
            System.Diagnostics.Debug.WriteLine($"RenderContext.CanvasToScreen: ({canvasX}, {canvasY}) -> ({result.X}, {result.Y}) [zoom={_viewport.Zoom}, offset=({_viewport.OffsetX}, {_viewport.OffsetY})]");
        }

        return result;
    }

    /// <summary>
    /// Transforms a canvas point to screen coordinate.
    /// </summary>
    /// <param name="canvasPoint">Point in canvas space.</param>
    /// <returns>The corresponding screen coordinate.</returns>
    public AvaloniaPoint CanvasToScreen(AvaloniaPoint canvasPoint)
    {
        return CanvasToScreen(canvasPoint.X, canvasPoint.Y);
    }

    /// <summary>
    /// Transforms a screen coordinate to canvas coordinate.
    /// 
    /// <para><b>USAGE GUIDANCE:</b></para>
    /// <list type="bullet">
    /// <item><b>PREFER</b> using <c>e.GetPosition(_mainCanvas)</c> for hit testing - gives canvas coords directly</item>
    /// <item><b>DO USE</b> when you have screen coordinates (from GetPosition(_rootPanel)) and need canvas coords</item>
    /// <item><b>DO USE</b> for inverse calculations from CanvasToScreen operations</item>
    /// </list>
    /// 
    /// <para><b>CORRECT:</b> <c>var canvasPos = e.GetPosition(_mainCanvas)</c> (no conversion needed)</para>
    /// <para><b>ALSO CORRECT:</b> <c>var canvasPos = ScreenToCanvas(e.GetPosition(_rootPanel))</c></para>
    /// </summary>
    /// <param name="screenX">X coordinate in screen space.</param>
    /// <param name="screenY">Y coordinate in screen space.</param>
    /// <returns>The corresponding canvas coordinate.</returns>
    public AvaloniaPoint ScreenToCanvas(double screenX, double screenY)
    {
        if (_viewport == null)
        {
            return new AvaloniaPoint(screenX, screenY);
        }

        return _viewport.ScreenToCanvas(new AvaloniaPoint(screenX, screenY));
    }

    /// <summary>
    /// Transforms a screen point to canvas coordinate.
    /// </summary>
    /// <param name="screenPoint">Point in screen space.</param>
    /// <returns>The corresponding canvas coordinate.</returns>
    public AvaloniaPoint ScreenToCanvas(AvaloniaPoint screenPoint)
    {
        return ScreenToCanvas(screenPoint.X, screenPoint.Y);
    }

    /// <summary>
    /// Scales a value by the logical scale factor (always 1.0).
    /// 
    /// <para><b>DEPRECATED:</b> This method now returns the input value unchanged.</para>
    /// <para>In transform-based rendering, visual sizing should use unscaled values.</para>
    /// <para>For calculations that need actual zoom, use <c>value * ViewportZoom</c>.</para>
    /// </summary>
    /// <param name="value">The value to scale.</param>
    /// <returns>The input value unchanged (Scale is always 1.0).</returns>
    [Obsolete("Use raw values for visual sizing. For zoom-aware calculations, use value * ViewportZoom.")]
    public double ScaleValue(double value) => value * Scale;
}
