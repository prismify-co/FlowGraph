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

    /// <summary>
    /// Gets the "from" point for calculating the end marker angle.
    /// For bezier curves, calculates the approach direction based on control points.
    /// </summary>
    private AvaloniaPoint GetLastFromPoint(AvaloniaPoint start, AvaloniaPoint end, List<Core.Point>? waypoints, EdgeType edgeType = EdgeType.Bezier)
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            var lastWaypoint = waypoints[^1];
            // Waypoints are already in canvas coordinates
            return new AvaloniaPoint(lastWaypoint.X, lastWaypoint.Y);
        }

        // For bezier curves, calculate the approach direction from the second control point
        if (edgeType == EdgeType.Bezier)
        {
            return CalculateBezierApproachPoint(start, end, isEndPoint: true);
        }

        return start;
    }

    /// <summary>
    /// Gets the "to" point for calculating the start marker angle.
    /// For bezier curves, calculates the departure direction based on control points.
    /// </summary>
    private AvaloniaPoint GetFirstToPoint(AvaloniaPoint start, AvaloniaPoint end, List<Core.Point>? waypoints, EdgeType edgeType = EdgeType.Bezier)
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            var firstWaypoint = waypoints[0];
            // Waypoints are already in canvas coordinates
            return new AvaloniaPoint(firstWaypoint.X, firstWaypoint.Y);
        }

        // For bezier curves, calculate the departure direction from the first control point
        if (edgeType == EdgeType.Bezier)
        {
            return CalculateBezierApproachPoint(start, end, isEndPoint: false);
        }

        return end;
    }

    /// <summary>
    /// Calculates a point representing the bezier curve's approach/departure direction.
    /// This mimics the control point calculation from EdgePathHelper.CreateBezierPath.
    /// </summary>
    private static AvaloniaPoint CalculateBezierApproachPoint(AvaloniaPoint start, AvaloniaPoint end, bool isEndPoint)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var controlPointOffset = Math.Abs(dx) / 2;
        controlPointOffset = Math.Max(controlPointOffset, 50);

        // Mirror the control point logic from EdgePathHelper.CalculateDirectionAwareControlPoints
        if (dx > Math.Abs(dy) * 0.5)
        {
            // Standard left-to-right bezier - horizontal approach
            return isEndPoint
                ? new AvaloniaPoint(end.X - controlPointOffset, end.Y)
                : new AvaloniaPoint(start.X + controlPointOffset, start.Y);
        }

        if (dx < -Math.Abs(dy) * 0.5)
        {
            // Right-to-left (backwards) - horizontal approach
            return isEndPoint
                ? new AvaloniaPoint(end.X - controlPointOffset, end.Y)
                : new AvaloniaPoint(start.X + controlPointOffset, start.Y);
        }

        // Mostly vertical edges
        if (Math.Abs(dx) < 10)
        {
            // Pure vertical
            var verticalDist = Math.Abs(dy) / 3;
            return isEndPoint
                ? new AvaloniaPoint(end.X, end.Y - (dy > 0 ? verticalDist : -verticalDist))
                : new AvaloniaPoint(start.X, start.Y + (dy > 0 ? verticalDist : -verticalDist));
        }

        if (dx >= 0)
        {
            // Going down-right - slight horizontal offset
            var hOffset = Math.Min(controlPointOffset * 0.3, Math.Abs(dx) * 0.5);
            return isEndPoint
                ? new AvaloniaPoint(end.X - hOffset, end.Y - controlPointOffset * 0.4)
                : new AvaloniaPoint(start.X + hOffset, start.Y + controlPointOffset * 0.4);
        }
        else
        {
            // Going down-left
            return isEndPoint
                ? new AvaloniaPoint(end.X - controlPointOffset * 0.3, end.Y - Math.Abs(dy) * 0.3)
                : new AvaloniaPoint(start.X + controlPointOffset * 0.3, start.Y + Math.Abs(dy) * 0.3);
        }
    }

    /// <summary>
    /// Renders a marker (arrow) at an edge endpoint.
    /// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
    /// </summary>
    /// <param name="canvas">The canvas to render on.</param>
    /// <param name="point">The marker tip position.</param>
    /// <param name="fromPoint">The point the edge comes from (for angle calculation).</param>
    /// <param name="marker">The marker type.</param>
    /// <param name="stroke">The stroke brush.</param>
    /// <param name="portPosition">Optional port position for accurate arrow direction.</param>
    /// <returns>The rendered marker path.</returns>
    private AvaloniaPath RenderEdgeMarker(
        Canvas canvas,
        AvaloniaPoint point,
        AvaloniaPoint fromPoint,
        EdgeMarker marker,
        IBrush stroke,
        PortPosition? portPosition = null)
    {
        // Use the hybrid angle calculation for consistent arrow direction
        var angle = EdgePathHelper.CalculateArrowAngle(point, fromPoint, portPosition);
        // Use logical (unscaled) marker size - MatrixTransform handles zoom
        var markerSize = 10;
        var isClosed = marker == EdgeMarker.ArrowClosed;

        var markerGeometry = EdgePathHelper.CreateArrowMarker(point, angle, markerSize, isClosed);

        var markerPath = new AvaloniaPath
        {
            Data = markerGeometry,
            Stroke = stroke,
            StrokeThickness = 2,
            Fill = isClosed ? stroke : null,
            Tag = "marker"
        };

        canvas.Children.Add(markerPath);
        return markerPath;
    }

    /// <summary>
    /// Renders a label on an edge with support for anchor positioning and offsets.
    /// Automatically positions labels based on edge direction to avoid overlap with the edge line.
    /// Supports perpendicular offsets that rotate with the edge direction (like GoJS segmentOffset).
    /// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
    /// </summary>
    private TextBlock? RenderEdgeLabel(Canvas canvas, AvaloniaPoint start, AvaloniaPoint end, IReadOnlyList<Core.Point>? waypoints, Edge edge, ThemeResources theme)
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

        // Calculate position along the actual path (including waypoints) and get edge angle
        // Use 1.0 for scale since we're in transform-based rendering
        var (posX, posY, edgeDirection, edgeAngle) = CalculateLabelPositionOnPathWithDirectionAndAngle(start, end, waypoints, t, 1.0);

        // Create the text block first - use logical (unscaled) dimensions
        var textBlock = new TextBlock
        {
            Text = labelText,
            FontSize = 12,
            Foreground = theme.NodeText,
            Background = theme.NodeBackground,
            Padding = new Thickness(4, 2, 4, 2),
            Tag = edge,  // Store edge reference for event handling
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // Get user-specified offsets if any
        var userOffsetX = labelInfo?.OffsetX ?? 0;
        var userOffsetY = labelInfo?.OffsetY ?? 0;
        var perpOffset = labelInfo?.PerpendicularOffset ?? 0;
        var hasUserOffset = labelInfo != null && (labelInfo.OffsetX != 0 || labelInfo.OffsetY != 0);
        var hasPerpOffset = labelInfo != null && labelInfo.PerpendicularOffset != 0;

        // Calculate smart offset based on edge direction (only if no user override and no perpendicular offset)
        double autoOffsetX = 0;
        double autoOffsetY = 0;

        if (!hasUserOffset && !hasPerpOffset)
        {
            // Position label based on edge direction to avoid overlap
            switch (edgeDirection)
            {
                case EdgeDirection.Horizontal:
                    // Horizontal edge: place label above
                    autoOffsetY = -16;
                    break;

                case EdgeDirection.Vertical:
                    // Vertical edge: place label to the right
                    autoOffsetX = 8;
                    autoOffsetY = -8;  // Slight vertical offset for centering
                    break;

                case EdgeDirection.DiagonalDownRight:
                case EdgeDirection.DiagonalUpRight:
                    // Diagonal going right: place label above-right
                    autoOffsetX = 4;
                    autoOffsetY = -16;
                    break;

                case EdgeDirection.DiagonalDownLeft:
                case EdgeDirection.DiagonalUpLeft:
                    // Diagonal going left: place label above-left
                    autoOffsetX = -4;
                    autoOffsetY = -16;
                    break;
            }
        }

        // Calculate perpendicular offset (rotates with edge direction)
        // Perpendicular to edge means: if edge angle is θ, perpendicular is θ + 90°
        // Positive perpOffset moves to the "right" of the edge direction
        double perpOffsetX = 0;
        double perpOffsetY = 0;
        if (hasPerpOffset)
        {
            // Calculate perpendicular direction (90° clockwise from edge direction)
            // Edge direction vector: (cos(θ), sin(θ))
            // Perpendicular (right side): (sin(θ), -cos(θ)) but in screen coords Y is inverted
            // So right side is: (-sin(θ), cos(θ)) in math coords = (sin(θ), cos(θ)) in screen
            perpOffsetX = Math.Sin(edgeAngle) * perpOffset;
            perpOffsetY = -Math.Cos(edgeAngle) * perpOffset;  // Negative because screen Y is inverted
        }

        // Use logical dimensions - MatrixTransform handles zoom
        var finalOffsetX = userOffsetX + autoOffsetX + perpOffsetX;
        var finalOffsetY = userOffsetY + autoOffsetY + perpOffsetY;

        Canvas.SetLeft(textBlock, posX + finalOffsetX);
        Canvas.SetTop(textBlock, posY + finalOffsetY);

        canvas.Children.Add(textBlock);
        return textBlock;
    }

    /// <summary>
    /// Edge direction categories for smart label placement.
    /// </summary>
    private enum EdgeDirection
    {
        Horizontal,          // Mostly left-right
        Vertical,            // Mostly up-down
        DiagonalDownRight,   // Going down and right
        DiagonalDownLeft,    // Going down and left
        DiagonalUpRight,     // Going up and right
        DiagonalUpLeft       // Going up and left
    }

    /// <summary>
    /// Calculates the label position along the actual edge path, including waypoints,
    /// and returns the direction of the edge segment at that point along with the angle.
    /// </summary>
    private static (double X, double Y, EdgeDirection Direction, double Angle) CalculateLabelPositionOnPathWithDirectionAndAngle(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<Core.Point>? waypoints,
        double t,
        double scale)
    {
        // If no waypoints, use simple interpolation between start and end
        if (waypoints == null || waypoints.Count == 0)
        {
            var direction = DetermineEdgeDirection(start.X, start.Y, end.X, end.Y);
            var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            return (
                start.X + (end.X - start.X) * t,
                start.Y + (end.Y - start.Y) * t,
                direction,
                angle
            );
        }

        // Build the complete path: start -> waypoints (already in screen coords) -> end
        var allPoints = new List<AvaloniaPoint> { start };
        foreach (var wp in waypoints)
        {
            allPoints.Add(new AvaloniaPoint(wp.X, wp.Y));
        }
        allPoints.Add(end);

        // Calculate total path length
        double totalLength = 0;
        var segmentLengths = new List<double>();
        for (int i = 0; i < allPoints.Count - 1; i++)
        {
            var dx = allPoints[i + 1].X - allPoints[i].X;
            var dy = allPoints[i + 1].Y - allPoints[i].Y;
            var segmentLength = Math.Sqrt(dx * dx + dy * dy);
            segmentLengths.Add(segmentLength);
            totalLength += segmentLength;
        }

        // Find the position at t along the total path
        var targetDistance = totalLength * t;
        double accumulatedDistance = 0;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            if (accumulatedDistance + segmentLengths[i] >= targetDistance)
            {
                // The point is on this segment
                var segmentT = (targetDistance - accumulatedDistance) / segmentLengths[i];
                var p1 = allPoints[i];
                var p2 = allPoints[i + 1];
                var direction = DetermineEdgeDirection(p1.X, p1.Y, p2.X, p2.Y);
                var angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                return (
                    p1.X + (p2.X - p1.X) * segmentT,
                    p1.Y + (p2.Y - p1.Y) * segmentT,
                    direction,
                    angle
                );
            }
            accumulatedDistance += segmentLengths[i];
        }

        // Fallback to end point
        var fallbackDirection = DetermineEdgeDirection(start.X, start.Y, end.X, end.Y);
        var fallbackAngle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        return (end.X, end.Y, fallbackDirection, fallbackAngle);
    }

    /// <summary>
    /// Calculates the label position along the actual edge path, including waypoints,
    /// and returns the direction of the edge segment at that point.
    /// </summary>
    private static (double X, double Y, EdgeDirection Direction) CalculateLabelPositionOnPathWithDirection(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<Core.Point>? waypoints,
        double t,
        double scale)
    {
        var (x, y, direction, _) = CalculateLabelPositionOnPathWithDirectionAndAngle(start, end, waypoints, t, scale);
        return (x, y, direction);
    }

    /// <summary>
    /// Determines the direction category of an edge segment.
    /// </summary>
    private static EdgeDirection DetermineEdgeDirection(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var absDx = Math.Abs(dx);
        var absDy = Math.Abs(dy);

        // If mostly horizontal (|dx| > 2 * |dy|)
        if (absDx > absDy * 2)
            return EdgeDirection.Horizontal;

        // If mostly vertical (|dy| > 2 * |dx|)
        if (absDy > absDx * 2)
            return EdgeDirection.Vertical;

        // Diagonal - determine quadrant
        if (dx > 0 && dy > 0)
            return EdgeDirection.DiagonalDownRight;
        if (dx > 0 && dy < 0)
            return EdgeDirection.DiagonalUpRight;
        if (dx < 0 && dy > 0)
            return EdgeDirection.DiagonalDownLeft;

        return EdgeDirection.DiagonalUpLeft;
    }

    /// <summary>
    /// Calculates the label position along the actual edge path, including waypoints.
    /// </summary>
    private static (double X, double Y) CalculateLabelPositionOnPath(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<Core.Point>? waypoints,
        double t,
        double scale)
    {
        var (x, y, _) = CalculateLabelPositionOnPathWithDirection(start, end, waypoints, t, scale);
        return (x, y);
    }

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
