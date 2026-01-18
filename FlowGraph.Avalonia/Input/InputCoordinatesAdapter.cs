using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core.Coordinates;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Implementation of <see cref="IInputCoordinates"/> that abstracts away
/// rendering mode differences for coordinate handling.
/// 
/// <para>
/// This adapter automatically detects the current rendering mode and uses
/// the appropriate coordinate conversion strategy:
/// <list type="bullet">
/// <item>Visual Tree mode: GetPosition(MainCanvas) already returns canvas coordinates</item>
/// <item>Direct Rendering mode: GetPosition(RootPanel) returns viewport coordinates, requires transform</item>
/// </list>
/// </para>
/// </summary>
public sealed class InputCoordinatesAdapter : IInputCoordinates
{
    private readonly ViewportState _viewport;
    private readonly Panel? _rootPanel;
    private readonly Canvas? _mainCanvas;
    private readonly DirectCanvasRenderer? _directRenderer;

    /// <summary>
    /// Creates a new InputCoordinatesAdapter.
    /// </summary>
    /// <param name="viewport">The viewport state for coordinate transforms.</param>
    /// <param name="rootPanel">The root panel (untransformed container).</param>
    /// <param name="mainCanvas">The main canvas (transformed container).</param>
    /// <param name="directRenderer">The direct renderer, if in direct rendering mode.</param>
    public InputCoordinatesAdapter(
        ViewportState viewport,
        Panel? rootPanel,
        Canvas? mainCanvas,
        DirectCanvasRenderer? directRenderer)
    {
        _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
        _rootPanel = rootPanel;
        _mainCanvas = mainCanvas;
        _directRenderer = directRenderer;
    }

    /// <summary>
    /// Whether we're currently in Direct Rendering mode.
    /// </summary>
    public bool IsDirectRenderingMode => _directRenderer != null;

    /// <inheritdoc />
    public double Zoom => _viewport.Zoom;

    /// <inheritdoc />
    public CanvasPoint GetPointerCanvasPosition(PointerEventArgs e)
    {
        if (IsDirectRenderingMode && _rootPanel != null)
        {
            // Direct Rendering mode: GetPosition(RootPanel) returns viewport coordinates
            // We need to transform to canvas coordinates
            var viewportPos = e.GetPosition(_rootPanel);
            var canvasPos = _viewport.ViewportToCanvas(viewportPos);
            System.Diagnostics.Debug.WriteLine($"[CoordAdapter.GetCanvasPos] DirectMode: ViewportPos=({viewportPos.X:F1},{viewportPos.Y:F1}) → CanvasPos=({canvasPos.X:F1},{canvasPos.Y:F1}) using Offset=({_viewport.OffsetX:F1},{_viewport.OffsetY:F1}) Zoom={_viewport.Zoom:F2}");
            return new CanvasPoint(canvasPos.X, canvasPos.Y);
        }
        else if (_mainCanvas != null)
        {
            // Visual Tree mode: GetPosition(MainCanvas) already applies inverse transform
            // This returns canvas coordinates directly
            var canvasPos = e.GetPosition(_mainCanvas);
            return new CanvasPoint(canvasPos.X, canvasPos.Y);
        }
        else
        {
            // Fallback - should not happen in normal operation
            return CanvasPoint.Zero;
        }
    }

    /// <inheritdoc />
    public ViewportPoint GetPointerViewportPosition(PointerEventArgs e)
    {
        if (_rootPanel != null)
        {
            // RootPanel is always untransformed, so this gives viewport coordinates
            var viewportPos = e.GetPosition(_rootPanel);
            return new ViewportPoint(viewportPos.X, viewportPos.Y);
        }
        else if (_mainCanvas != null)
        {
            // Fallback: get canvas position and transform to viewport
            var canvasPos = e.GetPosition(_mainCanvas);
            var viewportPos = _viewport.CanvasToViewport(canvasPos);
            return new ViewportPoint(viewportPos.X, viewportPos.Y);
        }
        else
        {
            return ViewportPoint.Zero;
        }
    }

    /// <inheritdoc />
    public CanvasRect GetVisibleCanvasRect()
    {
        var rect = _viewport.GetVisibleRect();
        return new CanvasRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    /// <inheritdoc />
    public ViewportRect GetViewportBounds()
    {
        var size = _viewport.ViewSize;
        return new ViewportRect(0, 0, size.Width, size.Height);
    }

    /// <inheritdoc />
    public ViewportPoint ToViewport(CanvasPoint canvas)
    {
        var result = _viewport.CanvasToViewport(new AvaloniaPoint(canvas.X, canvas.Y));
        return new ViewportPoint(result.X, result.Y);
    }

    /// <inheritdoc />
    public CanvasPoint ToCanvas(ViewportPoint viewport)
    {
        var result = _viewport.ViewportToCanvas(new AvaloniaPoint(viewport.X, viewport.Y));
        return new CanvasPoint(result.X, result.Y);
    }

    /// <inheritdoc />
    public ViewportVector ToViewport(CanvasVector canvasDelta)
    {
        // Vectors only need zoom scaling, not offset
        return new ViewportVector(canvasDelta.DX * _viewport.Zoom, canvasDelta.DY * _viewport.Zoom);
    }

    /// <inheritdoc />
    public CanvasVector ToCanvas(ViewportVector viewportDelta)
    {
        // Vectors only need zoom scaling, not offset
        return new CanvasVector(viewportDelta.DX / _viewport.Zoom, viewportDelta.DY / _viewport.Zoom);
    }
}
