using Avalonia;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Shared rendering context containing settings, viewport state, and coordinate transformation utilities.
/// Used by all visual managers to ensure consistent rendering behavior.
/// </summary>
public class RenderContext
{
    private readonly FlowCanvasSettings _settings;
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
    /// Gets the current viewport state.
    /// </summary>
    public ViewportState? Viewport => _viewport;

    /// <summary>
    /// Gets the current zoom/scale factor.
    /// </summary>
    public double Scale => _viewport?.Zoom ?? 1.0;

    /// <summary>
    /// Sets the viewport state for coordinate transformations.
    /// </summary>
    /// <param name="viewport">The viewport state to use.</param>
    public void SetViewport(ViewportState? viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Transforms a canvas coordinate to screen coordinate.
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
    /// Scales a value by the current zoom factor.
    /// </summary>
    /// <param name="value">The value to scale.</param>
    /// <returns>The scaled value.</returns>
    public double ScaleValue(double value) => value * Scale;
}
