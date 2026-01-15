using Avalonia;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Viewport operations (zoom, pan, fit-to-view).
/// </summary>
public partial class FlowCanvas
{
    #region Public API - Viewport

    /// <summary>
    /// Forces a re-render of all edges. Useful after modifying edge properties.
    /// </summary>
    public void RefreshEdges()
    {
        // In direct rendering mode, just trigger a full redraw
        if (_useDirectRendering && _directRenderer != null)
        {
            _directRenderer.InvalidateVisual();
            return;
        }
        RenderEdges();
    }

    /// <summary>
    /// Forces a complete re-render of the graph.
    /// </summary>
    public void Refresh()
    {
        RenderAll();
    }

    /// <summary>
    /// Zooms in by one step, keeping the graph centered.
    /// </summary>
    public void ZoomIn()
    {
        _viewport.ZoomIn(GetGraphCenterInScreenCoords());
    }

    /// <summary>
    /// Zooms out by one step, keeping the graph centered.
    /// </summary>
    public void ZoomOut()
    {
        _viewport.ZoomOut(GetGraphCenterInScreenCoords());
    }

    /// <summary>
    /// Resets zoom to 100%, keeping the graph centered.
    /// </summary>
    public void ResetZoom()
    {
        _viewport.SetZoom(1.0, GetGraphCenterInScreenCoords());
    }

    /// <summary>
    /// Sets the zoom level.
    /// </summary>
    public void SetZoom(double zoom) => _viewport.SetZoom(zoom);

    /// <summary>
    /// Fits all nodes into the viewport.
    /// </summary>
    public void FitToView()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        if (Graph == null || !Graph.Elements.Nodes.Any()) return;

        var boundsTime = sw.ElapsedMilliseconds;
        var graphBounds = CalculateGraphBounds();
        var calcTime = sw.ElapsedMilliseconds - boundsTime;
        
        var viewSize = Bounds.Size;
        System.Diagnostics.Debug.WriteLine($"[FitToView] GraphBounds=({graphBounds.X:F0},{graphBounds.Y:F0},{graphBounds.Width:F0}x{graphBounds.Height:F0}) ViewSize=({viewSize.Width:F0}x{viewSize.Height:F0})");
        
        _viewport.FitToBounds(graphBounds, viewSize);
        var fitTime = sw.ElapsedMilliseconds - calcTime - boundsTime;
        
        System.Diagnostics.Debug.WriteLine($"[FitToView] After fit: Zoom={_viewport.Zoom:F3} Offset=({_viewport.OffsetX:F1},{_viewport.OffsetY:F1})");
        
        RenderGrid();
        
        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[FitToView] Total={sw.ElapsedMilliseconds}ms | Bounds={calcTime}ms, Fit={fitTime}ms, Grid={sw.ElapsedMilliseconds - fitTime - calcTime - boundsTime}ms");
    }

    /// <summary>
    /// Centers the viewport on the center of all nodes without changing zoom.
    /// </summary>
    public void CenterOnGraph()
    {
        if (Graph == null || !Graph.Elements.Nodes.Any()) return;

        var bounds = CalculateGraphBounds();
        var center = new AvaloniaPoint(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);

        _viewport.CenterOn(center);
        RenderGrid();
    }

    /// <summary>
    /// Centers the viewport on a specific point in canvas coordinates.
    /// </summary>
    public void CenterOn(double x, double y)
    {
        _viewport.CenterOn(new AvaloniaPoint(x, y));
        RenderGrid();
    }

    #endregion

    #region Viewport Helpers

    private AvaloniaPoint? GetGraphCenterInScreenCoords()
    {
        if (Graph == null || !Graph.Elements.Nodes.Any())
            return null;

        var bounds = CalculateGraphBounds();
        var center = new AvaloniaPoint(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);

        return _viewport.CanvasToScreen(center);
    }

    private Rect CalculateGraphBounds()
    {
        if (Graph == null || !Graph.Elements.Nodes.Any())
            return default;

        // Single-pass iteration instead of 4 separate LINQ queries (4x faster)
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        
        foreach (var node in Graph.Elements.Nodes)
        {
            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            var width = node.Width ?? Settings.NodeWidth;
            var height = node.Height ?? Settings.NodeHeight;
            maxX = Math.Max(maxX, node.Position.X + width);
            maxY = Math.Max(maxY, node.Position.Y + height);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion
}
