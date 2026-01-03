using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Renders edges (connections) between nodes on the canvas.
/// </summary>
public class EdgeRenderer
{
    private readonly FlowCanvasSettings _settings;
    private readonly Dictionary<string, AvaloniaPath> _edgeVisuals = new();
    private readonly Dictionary<string, AvaloniaPath> _edgeVisiblePaths = new();
    private Func<double>? _getScale;
    private Func<AvaloniaPoint, AvaloniaPoint>? _transformToScreen;
    private Func<Node, (double width, double height)>? _getNodeDimensions;
    private Func<double, int, int, double?, double>? _getPortY;

    public EdgeRenderer(FlowCanvasSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Sets up coordinate transformation functions.
    /// </summary>
    public void SetTransformFunctions(
        Func<double> getScale,
        Func<AvaloniaPoint, AvaloniaPoint> transformToScreen,
        Func<Node, (double width, double height)> getNodeDimensions,
        Func<double, int, int, double?, double> getPortY)
    {
        _getScale = getScale;
        _transformToScreen = transformToScreen;
        _getNodeDimensions = getNodeDimensions;
        _getPortY = getPortY;
    }

    /// <summary>
    /// Gets the visual element for an edge.
    /// </summary>
    public AvaloniaPath? GetEdgeVisual(string edgeId)
    {
        return _edgeVisuals.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Clears all edge visuals.
    /// </summary>
    public void Clear()
    {
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
    }

    /// <summary>
    /// Renders all edges in the graph.
    /// </summary>
    public void RenderEdges(Canvas canvas, Graph graph, ThemeResources theme, AvaloniaPath? excludePath = null)
    {
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();

        // Remove existing edges, markers, labels, and hit areas
        var elementsToRemove = canvas.Children
            .Where(c =>
                (c is AvaloniaPath p && p != excludePath && p.Tag is string tag && (tag == "edge" || tag == "marker")) ||
                (c is AvaloniaPath p2 && p2 != excludePath && p2.Tag is Edge) ||
                (c is AvaloniaPath p3 && p3 != excludePath && !p3.IsHitTestVisible && p3.Tag == null) ||
                (c is TextBlock tb && tb.Tag is string tbTag && tbTag == "edgeLabel"))
            .ToList();

        foreach (var element in elementsToRemove)
        {
            canvas.Children.Remove(element);
        }

        // Also remove old edges without tags (backward compatibility)
        var oldEdges = canvas.Children.OfType<AvaloniaPath>()
            .Where(p => p != excludePath && p.Tag == null && p.IsHitTestVisible)
            .ToList();
        foreach (var edge in oldEdges)
        {
            canvas.Children.Remove(edge);
        }

        foreach (var edge in graph.Edges)
        {
            RenderEdge(canvas, edge, graph, theme);
        }
    }

    /// <summary>
    /// Renders a single edge with its markers and optional label.
    /// </summary>
    public AvaloniaPath? RenderEdge(Canvas canvas, Edge edge, Graph graph, ThemeResources theme)
    {
        if (_getScale == null || _transformToScreen == null || _getNodeDimensions == null || _getPortY == null)
            return null;

        var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return null;

        var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
        var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);

        if (sourcePortIndex < 0) sourcePortIndex = 0;
        if (targetPortIndex < 0) targetPortIndex = 0;

        var (sourceWidth, sourceHeight) = _getNodeDimensions(sourceNode);
        var (_, targetHeight) = _getNodeDimensions(targetNode);

        var sourceY = _getPortY(sourceNode.Position.Y, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count), sourceHeight);
        var targetY = _getPortY(targetNode.Position.Y, targetPortIndex, Math.Max(1, targetNode.Inputs.Count), targetHeight);
        var sourceX = sourceNode.Position.X + sourceWidth;
        var targetX = targetNode.Position.X;

        var startPoint = _transformToScreen(new AvaloniaPoint(sourceX, sourceY));
        var endPoint = _transformToScreen(new AvaloniaPoint(targetX, targetY));

        var scale = _getScale();
        var pathGeometry = EdgePathHelper.CreatePath(startPoint, endPoint, edge.Type);
        var strokeBrush = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;

        var hitAreaPath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = Brushes.Transparent,
            StrokeThickness = _settings.EdgeHitAreaWidth * scale,
            Tag = edge,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var visiblePath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = strokeBrush,
            StrokeThickness = (edge.IsSelected ? 3 : 2) * scale,
            IsHitTestVisible = false
        };

        canvas.Children.Insert(0, visiblePath);
        canvas.Children.Insert(1, hitAreaPath);

        _edgeVisuals[edge.Id] = hitAreaPath;
        _edgeVisiblePaths[edge.Id] = visiblePath;

        if (edge.MarkerEnd != EdgeMarker.None)
        {
            RenderEdgeMarker(canvas, endPoint, startPoint, edge.MarkerEnd, strokeBrush, scale);
        }

        if (edge.MarkerStart != EdgeMarker.None)
        {
            RenderEdgeMarker(canvas, startPoint, endPoint, edge.MarkerStart, strokeBrush, scale);
        }

        if (!string.IsNullOrEmpty(edge.Label))
        {
            RenderEdgeLabel(canvas, startPoint, endPoint, edge.Label, theme, scale);
        }

        return hitAreaPath;
    }

    /// <summary>
    /// Updates the selection visual of an edge.
    /// </summary>
    public void UpdateEdgeSelection(Edge edge, ThemeResources theme)
    {
        if (_edgeVisiblePaths.TryGetValue(edge.Id, out var visiblePath) && _getScale != null)
        {
            var scale = _getScale();
            visiblePath.Stroke = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;
            visiblePath.StrokeThickness = (edge.IsSelected ? 3 : 2) * scale;
        }
    }

    private void RenderEdgeMarker(Canvas canvas, AvaloniaPoint point, AvaloniaPoint fromPoint, EdgeMarker marker, IBrush stroke, double scale)
    {
        var angle = EdgePathHelper.CalculateAngle(fromPoint, point);
        var markerSize = 10 * scale;
        var isClosed = marker == EdgeMarker.ArrowClosed;

        var markerGeometry = EdgePathHelper.CreateArrowMarker(point, angle, markerSize, isClosed);

        var markerPath = new AvaloniaPath
        {
            Data = markerGeometry,
            Stroke = stroke,
            StrokeThickness = 2 * scale,
            Fill = isClosed ? stroke : null,
            Tag = "marker"
        };

        canvas.Children.Insert(0, markerPath);
    }

    private void RenderEdgeLabel(Canvas canvas, AvaloniaPoint start, AvaloniaPoint end, string label, ThemeResources theme, double scale)
    {
        var midPoint = new AvaloniaPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);

        var textBlock = new TextBlock
        {
            Text = label,
            FontSize = 12 * scale,
            Foreground = theme.NodeText,
            Background = theme.NodeBackground,
            Padding = new Thickness(4 * scale, 2 * scale, 4 * scale, 2 * scale),
            Tag = "edgeLabel"
        };

        Canvas.SetLeft(textBlock, midPoint.X);
        Canvas.SetTop(textBlock, midPoint.Y - 10 * scale);

        canvas.Children.Add(textBlock);
    }
}
