// DirectGraphRenderer.CoordinateTransforms.cs
// Partial class containing coordinate transformation methods

using Avalonia;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

public partial class DirectGraphRenderer
{
    #region Coordinate Transforms

    private AvaloniaPoint ScreenToCanvas(double screenX, double screenY)
    {
        // PHASE 1: DirectRenderer does its own hit testing (bypasses visual tree).
        // Screen coords come from GetPosition(_rootPanel) - we need to convert to canvas space
        // using the viewport transform to match against node bounds.
        // The MatrixTransform only affects visual tree rendering, not DirectRenderer hit testing.
        if (_viewport == null) return new AvaloniaPoint(screenX, screenY);
        return new AvaloniaPoint(
            (screenX - _viewport.OffsetX) / _viewport.Zoom,
            (screenY - _viewport.OffsetY) / _viewport.Zoom);
    }

    private AvaloniaPoint CanvasToScreen(AvaloniaPoint canvasPoint, double zoom, double offsetX, double offsetY)
    {
        return new AvaloniaPoint(
            canvasPoint.X * zoom + offsetX,
            canvasPoint.Y * zoom + offsetY);
    }

    private Rect CanvasToScreen(Rect canvasRect, double zoom, double offsetX, double offsetY)
    {
        return new Rect(
            canvasRect.X * zoom + offsetX,
            canvasRect.Y * zoom + offsetY,
            canvasRect.Width * zoom,
            canvasRect.Height * zoom);
    }

    // Debug counter for tracking culling decisions
    private static int _debugFrameCount = 0;
    private static bool _debugVerbose = false; // Set to true for per-node logging

    private bool IsInVisibleBounds(Node node, double zoom, double offsetX, double offsetY, Rect viewBounds)
    {
        var canvasBounds = _model.GetNodeBounds(node);
        var screenBounds = CanvasToScreen(canvasBounds, zoom, offsetX, offsetY);
        var buffer = _settings.PortSize * zoom;

        var check1 = screenBounds.X + screenBounds.Width + buffer >= 0;  // right edge visible
        var check2 = screenBounds.X - buffer <= viewBounds.Width;         // left edge visible
        var check3 = screenBounds.Y + screenBounds.Height + buffer >= 0; // bottom edge visible
        var check4 = screenBounds.Y - buffer <= viewBounds.Height;        // top edge visible
        var isVisible = check1 && check2 && check3 && check4;

        // Log culling decisions for first few nodes in verbose mode
        if (_debugVerbose && !isVisible)
        {
            System.Diagnostics.Debug.WriteLine($"[CULL] {node.Id}: canvas=({canvasBounds.X:F0},{canvasBounds.Y:F0},{canvasBounds.Width:F0}x{canvasBounds.Height:F0}) " +
                $"screen=({screenBounds.X:F0},{screenBounds.Y:F0},{screenBounds.Width:F0}x{screenBounds.Height:F0}) " +
                $"viewBounds=(0,0,{viewBounds.Width:F0}x{viewBounds.Height:F0}) " +
                $"checks=({check1},{check2},{check3},{check4})");
        }

        return isVisible;
    }

    #endregion
}
