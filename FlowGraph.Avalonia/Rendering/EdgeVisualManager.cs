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
public partial class EdgeVisualManager
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
    /// Removes an edge visual and all its components from the canvas and tracking.
    /// </summary>
    /// <param name="canvas">The canvas containing the visual.</param>
    /// <param name="edge">The edge to remove.</param>
    /// <returns>True if the visual was found and removed.</returns>
    public bool RemoveEdgeVisual(Canvas canvas, Edge edge)
    {
        bool removed = false;

        // Remove hit area path
        if (_edgeVisuals.TryGetValue(edge.Id, out var hitPath))
        {
            canvas.Children.Remove(hitPath);
            _edgeVisuals.Remove(edge.Id);
            removed = true;
        }

        // Remove visible path
        if (_edgeVisiblePaths.TryGetValue(edge.Id, out var visiblePath))
        {
            canvas.Children.Remove(visiblePath);
            _edgeVisiblePaths.Remove(edge.Id);
            removed = true;
        }

        // Remove markers (arrows)
        if (_edgeMarkers.TryGetValue(edge.Id, out var markers))
        {
            foreach (var marker in markers)
            {
                canvas.Children.Remove(marker);
            }
            _edgeMarkers.Remove(edge.Id);
            removed = true;
        }

        // Remove label
        if (_edgeLabels.TryGetValue(edge.Id, out var label))
        {
            canvas.Children.Remove(label);
            _edgeLabels.Remove(edge.Id);
            removed = true;
        }

        // Remove endpoint handles
        if (_edgeEndpointHandles.TryGetValue(edge.Id, out var handles))
        {
            canvas.Children.Remove(handles.source);
            canvas.Children.Remove(handles.target);
            _edgeEndpointHandles.Remove(edge.Id);
            removed = true;
        }

        // Remove custom render results
        _customRenderResults.Remove(edge.Id);

        return removed;
    }

    /// <summary>
    /// Checks if an edge visual exists in tracking.
    /// </summary>
    /// <param name="edgeId">The edge ID to check.</param>
    /// <returns>True if the edge visual exists.</returns>
    public bool HasEdgeVisual(string edgeId) => _edgeVisuals.ContainsKey(edgeId);

    /// <summary>
    /// Renders all edges in the graph to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="graph">The graph containing edges.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="excludePath">Optional path to exclude from cleanup.</param>
    public void RenderEdges(Canvas canvas, Graph graph, ThemeResources theme, AvaloniaPath? excludePath = null)
    {
        // Remove previously tracked edge visuals from canvas
        // This is safer than pattern matching - we only remove what we created
        foreach (var (_, hitPath) in _edgeVisuals)
        {
            if (hitPath != excludePath)
                canvas.Children.Remove(hitPath);
        }
        foreach (var (_, visiblePath) in _edgeVisiblePaths)
        {
            if (visiblePath != excludePath)
                canvas.Children.Remove(visiblePath);
        }
        foreach (var (_, markers) in _edgeMarkers)
        {
            foreach (var marker in markers)
            {
                canvas.Children.Remove(marker);
            }
        }
        foreach (var (_, label) in _edgeLabels)
        {
            canvas.Children.Remove(label);
        }
        foreach (var (_, handles) in _edgeEndpointHandles)
        {
            canvas.Children.Remove(handles.source);
            canvas.Children.Remove(handles.target);
        }

        // Clear edge visuals dictionaries after removing from canvas
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
        _edgeMarkers.Clear();
        _edgeLabels.Clear();
        _edgeEndpointHandles.Clear();

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

        // Find the actual port objects to get their positions
        var sourcePort = sourceNode.Outputs.FirstOrDefault(p => p.Id == edge.SourcePort);
        var targetPort = targetNode.Inputs.FirstOrDefault(p => p.Id == edge.TargetPort);

        // Get node dimensions for proper port positioning
        var (sourceWidth, sourceHeight) = _nodeVisualManager.GetNodeDimensions(sourceNode);
        var (targetWidth, targetHeight) = _nodeVisualManager.GetNodeDimensions(targetNode);

        // Calculate port positions based on Port.Position property
        var (sourceX, sourceY) = GetPortCanvasPosition(
            sourceNode, sourcePort, sourceWidth, sourceHeight, isOutput: true);
        var (targetX, targetY) = GetPortCanvasPosition(
            targetNode, targetPort, targetWidth, targetHeight, isOutput: false);

        // Use canvas coordinates directly (MainCanvas transform handles viewport mapping)
        var startPoint = new AvaloniaPoint(sourceX, sourceY);
        var endPoint = new AvaloniaPoint(targetX, targetY);

        // In transform-based rendering, Scale=1.0 always. MatrixTransform handles zoom.
        // We still get the viewport zoom for calculations that need it (like inverse scale for constant-size elements)
        var viewportZoom = _renderContext.ViewportZoom;

        // Check for custom edge renderer
        var customRenderer = _edgeRendererRegistry?.GetRenderer(edge);
        if (customRenderer != null)
        {
            return RenderCustomEdge(canvas, edge, graph, theme, customRenderer, sourceNode, targetNode, startPoint, endPoint, viewportZoom);
        }

        // Create path based on edge type - use waypoints if available
        PathGeometry pathGeometry;
        var waypoints = edge.Waypoints;  // Get once to avoid multiple ToList() calls
        IReadOnlyList<Core.Point>? transformedWaypoints = null;

        if (waypoints != null && waypoints.Count > 0)
        {
            // Waypoints are already in canvas coordinates
            transformedWaypoints = waypoints;

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

        // Create visible edge path - use logical (unscaled) dimensions
        // MatrixTransform handles all zoom scaling
        var visiblePath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = strokeBrush,
            StrokeThickness = edge.IsSelected ? 3 : 2,
            StrokeDashArray = null,
            IsHitTestVisible = false
        };

        // Create invisible hit area path (wider, transparent stroke for easier clicking)
        // Use logical dimensions - MatrixTransform scales the hit area appropriately
        var hitAreaPath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = Brushes.Transparent,
            StrokeThickness = _renderContext.Settings.EdgeHitAreaWidth,
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

        // Render end marker (arrow) - use logical dimensions
        if (edge.MarkerEnd != EdgeMarker.None)
        {
            var lastFromPoint = GetLastFromPoint(startPoint, endPoint, edge.Waypoints, edge.Type);
            // Use port position if available, otherwise default to Left for input ports
            var targetPortPosition = targetPort?.Position ?? PortPosition.Left;
            var markerPath = RenderEdgeMarker(canvas, endPoint, lastFromPoint, edge.MarkerEnd, strokeBrush, targetPortPosition);
            if (markerPath != null)
            {
                markers.Add(markerPath);
            }
        }

        // Render start marker - use logical dimensions
        if (edge.MarkerStart != EdgeMarker.None)
        {
            var firstToPoint = GetFirstToPoint(startPoint, endPoint, edge.Waypoints, edge.Type);
            // Use port position if available, otherwise default to Right for output ports
            var sourcePortPosition = sourcePort?.Position ?? PortPosition.Right;
            var markerPath = RenderEdgeMarker(canvas, startPoint, firstToPoint, edge.MarkerStart, strokeBrush, sourcePortPosition);
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
            // Pass transformed waypoints for accurate label positioning along the routed path
            var labelVisual = RenderEdgeLabel(canvas, startPoint, endPoint, transformedWaypoints, edge, theme);
            if (labelVisual != null)
            {
                _edgeLabels[edge.Id] = labelVisual;
            }
        }

        return hitAreaPath;
    }

    /// <summary>
    /// Updates the selection visual state of an edge.
    /// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
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
                    Scale = _renderContext.Scale, // Scale is 1.0 in transform-based rendering
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
            // Use logical (unscaled) dimensions - MatrixTransform handles zoom
            visiblePath.Stroke = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;
            visiblePath.StrokeThickness = edge.IsSelected ? 3 : 2;
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
        double viewportZoom)
    {
        IReadOnlyList<AvaloniaPoint>? transformedWaypoints = null;
        var waypoints = edge.Waypoints; // cloned list via Edge.State
        if (waypoints != null && waypoints.Count > 0)
        {
            // Convert Core.Point waypoints to AvaloniaPoint (already in canvas coords)
            transformedWaypoints = waypoints
                .Select(wp => new AvaloniaPoint(wp.X, wp.Y))
                .ToList();
        }

        // Scale is 1.0 in transform-based rendering - MatrixTransform handles zoom
        var context = new EdgeRenderers.EdgeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = 1.0, // Transform-based rendering
            SourceNode = sourceNode,
            TargetNode = targetNode,
            StartPoint = startPoint,
            EndPoint = endPoint,
            Waypoints = transformedWaypoints,
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

    // Marker rendering methods are in EdgeVisualManager.Markers.cs
    // Label rendering methods are in EdgeVisualManager.Labels.cs

    /// <summary>
    /// Calculates the canvas position for a port based on its Position property.
    /// </summary>
    /// <param name="node">The node containing the port.</param>
    /// <param name="port">The port (can be null for default behavior).</param>
    /// <param name="nodeWidth">Width of the node.</param>
    /// <param name="nodeHeight">Height of the node.</param>
    /// <param name="isOutput">True if this is an output port, false for input.</param>
    /// <returns>Canvas coordinates (X, Y) for the port connection point.</returns>
    private (double X, double Y) GetPortCanvasPosition(
        Node node, Port? port, double nodeWidth, double nodeHeight, bool isOutput)
    {
        var nodeX = node.Position.X;
        var nodeY = node.Position.Y;

        var ports = isOutput ? node.Outputs : node.Inputs;
        var position = port?.Position ?? (isOutput ? PortPosition.Right : PortPosition.Left);

        // Distribute ports along the edge like GraphRenderModel does.
        var totalPorts = Math.Max(1, ports.Count);
        var portIndex = 0;
        if (port != null)
        {
            var idx = ports.IndexOf(port);
            if (idx >= 0) portIndex = idx;
        }

        static double Along(double start, double length, int index, int total)
        {
            if (total <= 1) return start + length / 2;
            var spacing = length / (total + 1);
            return start + spacing * (index + 1);
        }

        return position switch
        {
            PortPosition.Right => (nodeX + nodeWidth, Along(nodeY, nodeHeight, portIndex, totalPorts)),
            PortPosition.Left => (nodeX, Along(nodeY, nodeHeight, portIndex, totalPorts)),
            PortPosition.Top => (Along(nodeX, nodeWidth, portIndex, totalPorts), nodeY),
            PortPosition.Bottom => (Along(nodeX, nodeWidth, portIndex, totalPorts), nodeY + nodeHeight),
            _ => isOutput
                ? (nodeX + nodeWidth, Along(nodeY, nodeHeight, portIndex, totalPorts))
                : (nodeX, Along(nodeY, nodeHeight, portIndex, totalPorts))
        };
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
