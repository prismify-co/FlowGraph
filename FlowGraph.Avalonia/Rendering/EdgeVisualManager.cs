using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Manages rendering and tracking of edge visuals including paths, markers, and labels.
/// Responsible for creating, updating, and removing edge UI elements.
/// </summary>
public class EdgeVisualManager
{
    private readonly RenderContext _renderContext;
    private readonly NodeVisualManager _nodeVisualManager;
    private readonly EdgeRenderers.EdgeRendererRegistry? _edgeRendererRegistry;

    // Custom render results tracked separately
    private readonly Dictionary<string, EdgeRenderers.EdgeRenderResult> _customRenderResults = new();

    // Visual tracking
    private readonly Dictionary<string, AvaloniaPath> _edgeVisuals = new();  // Hit area paths
    private readonly Dictionary<string, AvaloniaPath> _edgeVisiblePaths = new();  // Visible paths
    private readonly Dictionary<string, List<AvaloniaPath>> _edgeMarkers = new();  // Edge markers (arrows)
    private readonly Dictionary<string, TextBlock> _edgeLabels = new();  // Edge labels
    private readonly Dictionary<string, (Ellipse source, Ellipse target)> _edgeEndpointHandles = new();  // Edge endpoint handles

    /// <summary>
    /// Creates a new edge visual manager.
    /// </summary>
    /// <param name="renderContext">Shared render context.</param>
    /// <param name="nodeVisualManager">Node visual manager for port position calculations.</param>
    /// <param name="edgeRendererRegistry">Optional registry for custom edge renderers.</param>
    public EdgeVisualManager(RenderContext renderContext, NodeVisualManager nodeVisualManager, EdgeRenderers.EdgeRendererRegistry? edgeRendererRegistry = null)
    {
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
        _nodeVisualManager = nodeVisualManager ?? throw new ArgumentNullException(nameof(nodeVisualManager));
        _edgeRendererRegistry = edgeRendererRegistry;
    }

    /// <summary>
    /// Gets the hit area path for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's hit area path, or null if not found.</returns>
    public AvaloniaPath? GetEdgeVisual(string edgeId)
    {
        return _edgeVisuals.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Gets the visible path for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's visible path, or null if not found.</returns>
    public AvaloniaPath? GetEdgeVisiblePath(string edgeId)
    {
        return _edgeVisiblePaths.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Gets the markers (arrows) for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's markers, or null if not found.</returns>
    public IReadOnlyList<AvaloniaPath>? GetEdgeMarkers(string edgeId)
    {
        return _edgeMarkers.TryGetValue(edgeId, out var markers) ? markers : null;
    }

    /// <summary>
    /// Gets the label for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's label, or null if not found.</returns>
    public TextBlock? GetEdgeLabel(string edgeId)
    {
        return _edgeLabels.TryGetValue(edgeId, out var label) ? label : null;
    }

    /// <summary>
    /// Gets the endpoint handles for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>Tuple of source and target endpoint handles.</returns>
    public (Ellipse? source, Ellipse? target) GetEdgeEndpointHandles(string edgeId)
    {
        return _edgeEndpointHandles.TryGetValue(edgeId, out var handles) ? handles : (null, null);
    }

    /// <summary>
    /// Clears all tracked edge visuals.
    /// Note: This does not remove them from the canvas.
    /// </summary>
    public void Clear()
    {
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
        _edgeMarkers.Clear();
        _edgeLabels.Clear();
        _edgeEndpointHandles.Clear();
        _customRenderResults.Clear();
    }

    /// <summary>
    /// Renders all edges in the graph to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="graph">The graph containing edges.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="excludePath">Optional path to exclude from cleanup.</param>
    public void RenderEdges(Canvas canvas, Graph graph, ThemeResources theme, AvaloniaPath? excludePath = null)
    {
        // Clear edge visuals dictionaries
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
        _edgeMarkers.Clear();
        _edgeLabels.Clear();

        // Remove existing edge endpoint handles
        foreach (var (_, handles) in _edgeEndpointHandles)
        {
            canvas.Children.Remove(handles.source);
            canvas.Children.Remove(handles.target);
        }
        _edgeEndpointHandles.Clear();

        // Remove existing edges, markers, labels, and hit areas
        var elementsToRemove = canvas.Children
            .Where(c =>
                (c is AvaloniaPath p && p != excludePath && p.Tag is string tag && (tag == "edge" || tag == "marker")) ||
                (c is AvaloniaPath p2 && p2 != excludePath && p2.Tag is Edge) ||
                (c is AvaloniaPath p3 && p3 != excludePath && !p3.IsHitTestVisible && p3.Tag == null) ||
                (c is TextBlock tb && tb.Tag is Edge) ||  // Edge labels now have Edge as tag
                (c is TextBlock tb2 && tb2.Tag is string tbTag && tbTag == "edgeLabel") ||  // Backward compat
                (c is Ellipse el && el.Tag is (Edge, bool)))
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

        // Render new edges - only if both endpoints are visible (by group collapse)
        // and at least one endpoint is in the visible viewport (virtualization)
        foreach (var edge in graph.Elements.Edges)
        {
            var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

            // Skip edges where either endpoint is hidden by a collapsed group
            if (sourceNode == null || targetNode == null ||
                !NodeVisualManager.IsNodeVisible(graph, sourceNode) ||
                !NodeVisualManager.IsNodeVisible(graph, targetNode))
            {
                continue;
            }

            // Virtualization: Skip edges where both endpoints are outside visible bounds
            if (!IsEdgeInVisibleBounds(sourceNode, targetNode))
            {
                continue;
            }

            RenderEdge(canvas, edge, graph, theme);
        }
    }

    /// <summary>
    /// Renders a single edge with its markers and optional label.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="edge">The edge to render.</param>
    /// <param name="graph">The graph containing the edge.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <returns>The created hit area path, or null if rendering failed.</returns>
    public AvaloniaPath? RenderEdge(Canvas canvas, Edge edge, Graph graph, ThemeResources theme)
    {
        var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return null;

        var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
        var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);

        if (sourcePortIndex < 0) sourcePortIndex = 0;
        if (targetPortIndex < 0) targetPortIndex = 0;

        // Get node dimensions for proper port positioning
        var (sourceWidth, sourceHeight) = _nodeVisualManager.GetNodeDimensions(sourceNode);
        var (_, targetHeight) = _nodeVisualManager.GetNodeDimensions(targetNode);

        // Get canvas coordinates
        var sourceY = _nodeVisualManager.GetPortYCanvas(sourceNode.Position.Y, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count), sourceHeight);
        var targetY = _nodeVisualManager.GetPortYCanvas(targetNode.Position.Y, targetPortIndex, Math.Max(1, targetNode.Inputs.Count), targetHeight);
        var sourceX = sourceNode.Position.X + sourceWidth;
        var targetX = targetNode.Position.X;

        // Transform to screen coordinates
        var startPoint = _renderContext.CanvasToScreen(sourceX, sourceY);
        var endPoint = _renderContext.CanvasToScreen(targetX, targetY);

        var scale = _renderContext.Scale;

        // Check for custom edge renderer
        var customRenderer = _edgeRendererRegistry?.GetRenderer(edge);
        if (customRenderer != null)
        {
            return RenderCustomEdge(canvas, edge, graph, theme, customRenderer, sourceNode, targetNode, startPoint, endPoint, scale);
        }

        // Create path based on edge type - use waypoints if available
        PathGeometry pathGeometry;
        if (edge.Waypoints != null && edge.Waypoints.Count > 0)
        {
            // Transform waypoints to screen coordinates
            var transformedWaypoints = edge.Waypoints
                .Select(wp => new Core.Point(
                    _renderContext.CanvasToScreen(wp.X, wp.Y).X,
                    _renderContext.CanvasToScreen(wp.X, wp.Y).Y))
                .ToList();

            pathGeometry = EdgePathHelper.CreatePathWithWaypoints(
                startPoint,
                endPoint,
                transformedWaypoints,
                edge.Type);
        }
        else
        {
            pathGeometry = EdgePathHelper.CreatePath(startPoint, endPoint, edge.Type);
        }

        var strokeBrush = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;

        // Create visible edge path (rendered first, appears behind)
        var visiblePath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = strokeBrush,
            StrokeThickness = (edge.IsSelected ? 3 : 2) * scale,
            StrokeDashArray = null,
            IsHitTestVisible = false
        };

        // Create invisible hit area path (wider, transparent stroke for easier clicking)
        var hitAreaPath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = Brushes.Transparent,
            StrokeThickness = _renderContext.Settings.EdgeHitAreaWidth * scale,
            Tag = edge,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // Add paths to canvas
        canvas.Children.Add(visiblePath);
        canvas.Children.Add(hitAreaPath);

        // Track both paths
        _edgeVisuals[edge.Id] = hitAreaPath;
        _edgeVisiblePaths[edge.Id] = visiblePath;

        // Track markers for this edge
        var markers = new List<AvaloniaPath>();

        // Render end marker (arrow)
        if (edge.MarkerEnd != EdgeMarker.None)
        {
            var lastFromPoint = GetLastFromPoint(startPoint, endPoint, edge.Waypoints);
            var markerPath = RenderEdgeMarker(canvas, endPoint, lastFromPoint, edge.MarkerEnd, strokeBrush, scale);
            if (markerPath != null)
            {
                markers.Add(markerPath);
            }
        }

        // Render start marker
        if (edge.MarkerStart != EdgeMarker.None)
        {
            var firstToPoint = GetFirstToPoint(startPoint, endPoint, edge.Waypoints);
            var markerPath = RenderEdgeMarker(canvas, startPoint, firstToPoint, edge.MarkerStart, strokeBrush, scale);
            if (markerPath != null)
            {
                markers.Add(markerPath);
            }
        }

        // Store markers for animation
        if (markers.Count > 0)
        {
            _edgeMarkers[edge.Id] = markers;
        }

        // Render label if present (use LabelInfo if available, else fall back to Label)
        var effectiveLabel = edge.Definition.EffectiveLabel;
        if (!string.IsNullOrEmpty(effectiveLabel))
        {
            var labelVisual = RenderEdgeLabel(canvas, startPoint, endPoint, edge, theme, scale);
            if (labelVisual != null)
            {
                _edgeLabels[edge.Id] = labelVisual;
            }
        }

        return hitAreaPath;
    }

    /// <summary>
    /// Updates the selection visual state of an edge.
    /// </summary>
    /// <param name="edge">The edge to update.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void UpdateEdgeSelection(Edge edge, ThemeResources theme)
    {
        // Check if this edge has a custom render result
        if (_customRenderResults.TryGetValue(edge.Id, out var customResult))
        {
            var customRenderer = _edgeRendererRegistry?.GetRenderer(edge);
            if (customRenderer != null)
            {
                var context = new EdgeRenderers.EdgeRenderContext
                {
                    Theme = theme,
                    Settings = _renderContext.Settings,
                    Scale = _renderContext.Scale,
                    SourceNode = null!, // Not needed for selection update
                    TargetNode = null!,
                    StartPoint = default,
                    EndPoint = default,
                    Graph = null!
                };
                customRenderer.UpdateSelection(customResult, edge, context);
                return;
            }
        }

        if (_edgeVisiblePaths.TryGetValue(edge.Id, out var visiblePath))
        {
            var scale = _renderContext.Scale;
            visiblePath.Stroke = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;
            visiblePath.StrokeThickness = (edge.IsSelected ? 3 : 2) * scale;
        }
    }

    /// <summary>
    /// Renders an edge using a custom renderer.
    /// </summary>
    private AvaloniaPath? RenderCustomEdge(
        Canvas canvas,
        Edge edge,
        Graph graph,
        ThemeResources theme,
        EdgeRenderers.IEdgeRenderer renderer,
        Node sourceNode,
        Node targetNode,
        AvaloniaPoint startPoint,
        AvaloniaPoint endPoint,
        double scale)
    {
        var context = new EdgeRenderers.EdgeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = scale,
            SourceNode = sourceNode,
            TargetNode = targetNode,
            StartPoint = startPoint,
            EndPoint = endPoint,
            Graph = graph
        };

        var result = renderer.Render(edge, context);

        // Add visuals to canvas
        canvas.Children.Add(result.VisiblePath);
        canvas.Children.Add(result.HitAreaPath);

        // Track the paths
        _edgeVisuals[edge.Id] = result.HitAreaPath;
        _edgeVisiblePaths[edge.Id] = result.VisiblePath;
        _customRenderResults[edge.Id] = result;

        // Add markers if present
        if (result.Markers is { Count: > 0 })
        {
            var markerList = new List<AvaloniaPath>();
            foreach (var marker in result.Markers)
            {
                canvas.Children.Add(marker);
                markerList.Add(marker);
            }
            _edgeMarkers[edge.Id] = markerList;
        }

        // Add label if present
        if (result.Label != null)
        {
            canvas.Children.Add(result.Label);
            if (result.Label is TextBlock tb)
            {
                _edgeLabels[edge.Id] = tb;
            }
        }

        // Add additional visuals
        if (result.AdditionalVisuals != null)
        {
            foreach (var visual in result.AdditionalVisuals)
            {
                canvas.Children.Add(visual);
            }
        }

        return result.HitAreaPath;
    }

    /// <summary>
    /// Gets the "from" point for calculating the end marker angle.
    /// </summary>
    private AvaloniaPoint GetLastFromPoint(AvaloniaPoint start, AvaloniaPoint end, List<Core.Point>? waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
            return start;

        var lastWaypoint = waypoints[^1];
        return _renderContext.CanvasToScreen(lastWaypoint.X, lastWaypoint.Y);
    }

    /// <summary>
    /// Gets the "to" point for calculating the start marker angle.
    /// </summary>
    private AvaloniaPoint GetFirstToPoint(AvaloniaPoint start, AvaloniaPoint end, List<Core.Point>? waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
            return end;

        var firstWaypoint = waypoints[0];
        return _renderContext.CanvasToScreen(firstWaypoint.X, firstWaypoint.Y);
    }

    /// <summary>
    /// Renders a marker (arrow) at an edge endpoint.
    /// </summary>
    private AvaloniaPath RenderEdgeMarker(Canvas canvas, AvaloniaPoint point, AvaloniaPoint fromPoint, EdgeMarker marker, IBrush stroke, double scale)
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

        canvas.Children.Add(markerPath);
        return markerPath;
    }

    /// <summary>
    /// Renders a label on an edge with support for anchor positioning and offsets.
    /// </summary>
    private TextBlock? RenderEdgeLabel(Canvas canvas, AvaloniaPoint start, AvaloniaPoint end, Edge edge, ThemeResources theme, double scale)
    {
        var labelInfo = edge.Definition.LabelInfo;
        var labelText = labelInfo?.Text ?? edge.Label;

        if (string.IsNullOrEmpty(labelText))
            return null;

        // Calculate position based on anchor
        double t = 0.5; // Default to center
        if (labelInfo != null)
        {
            t = labelInfo.Anchor switch
            {
                LabelAnchor.Start => 0.25,
                LabelAnchor.End => 0.75,
                _ => 0.5
            };
        }

        // Interpolate position along the edge
        var posX = start.X + (end.X - start.X) * t;
        var posY = start.Y + (end.Y - start.Y) * t;

        // Apply offsets (default -10 Y offset if no LabelInfo specified)
        var offsetX = labelInfo?.OffsetX ?? 0;
        var offsetY = labelInfo?.OffsetY ?? -10;

        var textBlock = new TextBlock
        {
            Text = labelText,
            FontSize = 12 * scale,
            Foreground = theme.NodeText,
            Background = theme.NodeBackground,
            Padding = new Thickness(4 * scale, 2 * scale, 4 * scale, 2 * scale),
            Tag = edge,  // Store edge reference for event handling
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        Canvas.SetLeft(textBlock, posX + offsetX * scale);
        Canvas.SetTop(textBlock, posY + offsetY * scale);

        canvas.Children.Add(textBlock);
        return textBlock;
    }

    /// <summary>
    /// Checks if an edge should be rendered based on whether at least one endpoint is in visible bounds.
    /// </summary>
    private bool IsEdgeInVisibleBounds(Node sourceNode, Node targetNode)
    {
        var (sourceWidth, sourceHeight) = _nodeVisualManager.GetNodeDimensions(sourceNode);
        var (targetWidth, targetHeight) = _nodeVisualManager.GetNodeDimensions(targetNode);

        var sourceInBounds = _renderContext.IsInVisibleBounds(
            sourceNode.Position.X, sourceNode.Position.Y, sourceWidth, sourceHeight);
        var targetInBounds = _renderContext.IsInVisibleBounds(
            targetNode.Position.X, targetNode.Position.Y, targetWidth, targetHeight);

        // Render if either endpoint is visible
        return sourceInBounds || targetInBounds;
    }
}
