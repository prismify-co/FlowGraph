using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
    /// Fits all elements (nodes and shapes) into the viewport.
    /// </summary>
    public void FitToView()
    {
        if (Graph == null) return;
        
        // Need at least one node or shape to fit
        if (!Graph.Elements.Nodes.Any() && !Graph.Elements.Shapes.Any()) return;

        var graphBounds = CalculateGraphBounds();
        if (graphBounds.Width <= 0 || graphBounds.Height <= 0)
            return;
            
        var viewSize = Bounds.Size;
        
        if (viewSize.Width <= 0 || viewSize.Height <= 0)
            return;

        _viewport.FitToBounds(graphBounds, viewSize, Settings.FitToViewPadding);
        
        // Explicitly apply transforms - the ViewportChanged event may not trigger 
        // ApplyViewportTransforms if we're in certain states (e.g., during graph loading)
        ApplyViewportTransforms();
        RenderGrid();
    }

    /// <summary>
    /// Centers the viewport on the center of all elements (nodes and shapes) without changing zoom.
    /// </summary>
    public void CenterOnGraph()
    {
        if (Graph == null) return;
        if (!Graph.Elements.Nodes.Any() && !Graph.Elements.Shapes.Any()) return;

        var bounds = CalculateGraphBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;
            
        var center = new AvaloniaPoint(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);

        _viewport.CenterOn(center);
        ApplyViewportTransforms();
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

        return _viewport.CanvasToViewport(center);
    }

    private Rect CalculateGraphBounds()
    {
        if (Graph == null)
            return default;

        // Include all elements: nodes and shapes
        var hasNodes = Graph.Elements.Nodes.Any();
        var hasShapes = Graph.Elements.Shapes.Any();
        
        if (!hasNodes && !hasShapes)
            return default;

        // Single-pass iteration instead of 4 separate LINQ queries (4x faster)
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        // Process nodes
        foreach (var node in Graph.Elements.Nodes)
        {
            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            var width = node.Width ?? Settings.NodeWidth;
            var height = node.Height ?? Settings.NodeHeight;
            maxX = Math.Max(maxX, node.Position.X + width);
            maxY = Math.Max(maxY, node.Position.Y + height);
        }

        // Process shapes (rectangles, text, lines, etc.)
        foreach (var shape in Graph.Elements.Shapes)
        {
            minX = Math.Min(minX, shape.Position.X);
            minY = Math.Min(minY, shape.Position.Y);
            var width = shape.Width ?? 0;
            var height = shape.Height ?? 0;
            maxX = Math.Max(maxX, shape.Position.X + width);
            maxY = Math.Max(maxY, shape.Position.Y + height);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion
}
