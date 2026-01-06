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
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        _viewport.FitToBounds(bounds, Bounds.Size);
        RenderGrid();
    }

    /// <summary>
    /// Centers the viewport on the center of all nodes without changing zoom.
    /// </summary>
    public void CenterOnGraph()
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

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
        if (Graph == null || Graph.Nodes.Count == 0)
            return null;

        var bounds = CalculateGraphBounds();
        var center = new AvaloniaPoint(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);
        
        return _viewport.CanvasToScreen(center);
    }

    private Rect CalculateGraphBounds()
    {
        if (Graph == null || Graph.Nodes.Count == 0)
            return default;

        var minX = Graph.Nodes.Min(n => n.Position.X);
        var minY = Graph.Nodes.Min(n => n.Position.Y);
        var maxX = Graph.Nodes.Max(n => n.Position.X + (n.Width ?? Settings.NodeWidth));
        var maxY = Graph.Nodes.Max(n => n.Position.Y + (n.Height ?? Settings.NodeHeight));

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion
}
