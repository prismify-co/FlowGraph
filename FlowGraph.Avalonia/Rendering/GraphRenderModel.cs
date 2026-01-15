using Avalonia;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Central model that calculates all rendering geometry for a graph.
/// This is the single source of truth for node bounds, port positions, edge paths, etc.
/// Both VisualTree rendering and DirectDraw rendering use this model to ensure 100% visual parity.
/// </summary>
/// <remarks>
/// <para>
/// The GraphRenderModel provides geometry calculations for:
/// </para>
/// <list type="bullet">
/// <item><description>Node bounds and dimensions</description></item>
/// <item><description>Port positions (supporting all 4 sides: Left, Right, Top, Bottom)</description></item>
/// <item><description>Edge endpoints and bezier control points</description></item>
/// <item><description>Group header and collapse button positions</description></item>
/// <item><description>Resize handle positions</description></item>
/// <item><description>Hit testing utilities</description></item>
/// </list>
/// <para>
/// By centralizing all geometry calculations here, both the visual tree renderer (NodeVisualManager)
/// and the direct renderer (DirectGraphRenderer) produce identical visual output.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var model = new GraphRenderModel(settings);
/// 
/// // Get node bounds in canvas coordinates
/// var bounds = model.GetNodeBounds(node);
/// 
/// // Get port position
/// var portPos = model.GetPortPosition(node, port, isOutput: true);
/// 
/// // Calculate edge endpoints
/// var (start, end) = model.GetEdgeEndpoints(edge, graph);
/// </code>
/// </example>
public class GraphRenderModel
{
    private FlowCanvasSettings _settings;
    private NodeRendererRegistry? _nodeRenderers;

    // Constants that must match across all renderers
    public const double GroupHeaderHeight = 28;
    public const double MinGroupWidth = 200;
    public const double MinGroupHeight = 100;
    public const double GroupCollapseButtonSize = 18;
    public const double GroupBorderRadius = 8;
    public const double GroupDashedStrokeThickness = 2;
    public const double GroupHeaderMarginX = 8;
    public const double GroupHeaderMarginY = 6;
    public const double NodeCornerRadius = 6;
    public const double ResizeHandleSize = 8;
    public const double PortHitPadding = 4;

    public GraphRenderModel(FlowCanvasSettings settings, NodeRendererRegistry? nodeRenderers = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _nodeRenderers = nodeRenderers;
    }

    /// <summary>
    /// Gets the settings used by this model.
    /// </summary>
    public FlowCanvasSettings Settings => _settings;

    /// <summary>
    /// Gets or sets the node renderer registry for custom node type dimensions.
    /// </summary>
    public NodeRendererRegistry? NodeRenderers
    {
        get => _nodeRenderers;
        set => _nodeRenderers = value;
    }

    /// <summary>
    /// Updates the settings used by this model.
    /// </summary>
    /// <param name="settings">The new settings to use.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    #region Node Geometry

    /// <summary>
    /// Gets the bounds of a node in canvas coordinates.
    /// </summary>
    public Rect GetNodeBounds(Node node)
    {
        var width = GetNodeWidth(node);
        var height = GetNodeHeight(node);
        return new Rect(node.Position.X, node.Position.Y, width, height);
    }

    /// <summary>
    /// Gets the width of a node in canvas coordinates.
    /// Considers custom renderer dimensions when available.
    /// </summary>
    public double GetNodeWidth(Node node)
    {
        if (node.IsGroup)
            return node.Width ?? MinGroupWidth;

        // First check if node has explicit width set
        if (node.Width.HasValue)
            return node.Width.Value;

        // Then check if custom renderer provides a width
        if (_nodeRenderers != null && !string.IsNullOrEmpty(node.Type))
        {
            var renderer = _nodeRenderers.GetRenderer(node.Type);
            var rendererWidth = renderer.GetWidth(node, _settings);
            if (rendererWidth.HasValue)
                return rendererWidth.Value;
        }

        return _settings.NodeWidth;
    }

    /// <summary>
    /// Gets the height of a node in canvas coordinates.
    /// Considers custom renderer dimensions when available.
    /// </summary>
    public double GetNodeHeight(Node node)
    {
        if (node.IsGroup)
            return node.IsCollapsed ? GroupHeaderHeight : (node.Height ?? MinGroupHeight);

        // First check if node has explicit height set
        if (node.Height.HasValue)
            return node.Height.Value;

        // Then check if custom renderer provides a height
        if (_nodeRenderers != null && !string.IsNullOrEmpty(node.Type))
        {
            var renderer = _nodeRenderers.GetRenderer(node.Type);
            var rendererHeight = renderer.GetHeight(node, _settings);
            if (rendererHeight.HasValue)
                return rendererHeight.Value;
        }

        return _settings.NodeHeight;
    }

    /// <summary>
    /// Gets the corner radius for a node.
    /// </summary>
    public double GetNodeCornerRadius(Node node)
    {
        return node.IsGroup ? GroupBorderRadius : NodeCornerRadius;
    }

    #endregion

    #region Port Geometry

    /// <summary>
    /// Gets the position of a port in canvas coordinates.
    /// Supports all four port positions (Left, Right, Top, Bottom).
    /// </summary>
    public AvaloniaPoint GetPortPosition(Node node, Port port, bool isOutput)
    {
        var bounds = GetNodeBounds(node);
        var ports = isOutput ? node.Outputs : node.Inputs;
        var portIndex = ports.IndexOf(port);
        if (portIndex < 0) portIndex = 0;
        var totalPorts = ports.Count;

        // Determine port position - use explicit position or default based on input/output
        var position = port.Position ?? (isOutput ? PortPosition.Right : PortPosition.Left);

        return CalculatePortPosition(bounds, position, portIndex, totalPorts);
    }

    /// <summary>
    /// Gets the position of a port by index in canvas coordinates.
    /// </summary>
    public AvaloniaPoint GetPortPositionByIndex(Node node, int portIndex, int totalPorts, bool isOutput)
    {
        var bounds = GetNodeBounds(node);
        var ports = isOutput ? node.Outputs : node.Inputs;

        // Get position from port if available
        PortPosition position;
        if (portIndex >= 0 && portIndex < ports.Count)
        {
            position = ports[portIndex].Position ?? (isOutput ? PortPosition.Right : PortPosition.Left);
        }
        else
        {
            position = isOutput ? PortPosition.Right : PortPosition.Left;
        }

        return CalculatePortPosition(bounds, position, portIndex, totalPorts);
    }

    /// <summary>
    /// Calculates port position based on node bounds, port position enum, and index.
    /// </summary>
    private AvaloniaPoint CalculatePortPosition(Rect nodeBounds, PortPosition position, int portIndex, int totalPorts)
    {
        return position switch
        {
            PortPosition.Left => new AvaloniaPoint(
                nodeBounds.X,
                GetPortAlongEdge(nodeBounds.Y, nodeBounds.Height, portIndex, totalPorts)),
            PortPosition.Right => new AvaloniaPoint(
                nodeBounds.X + nodeBounds.Width,
                GetPortAlongEdge(nodeBounds.Y, nodeBounds.Height, portIndex, totalPorts)),
            PortPosition.Top => new AvaloniaPoint(
                GetPortAlongEdge(nodeBounds.X, nodeBounds.Width, portIndex, totalPorts),
                nodeBounds.Y),
            PortPosition.Bottom => new AvaloniaPoint(
                GetPortAlongEdge(nodeBounds.X, nodeBounds.Width, portIndex, totalPorts),
                nodeBounds.Y + nodeBounds.Height),
            _ => new AvaloniaPoint(nodeBounds.X, nodeBounds.Y + nodeBounds.Height / 2)
        };
    }

    /// <summary>
    /// Calculates position along an edge for evenly distributed ports.
    /// </summary>
    private static double GetPortAlongEdge(double edgeStart, double edgeLength, int portIndex, int totalPorts)
    {
        if (totalPorts <= 0) totalPorts = 1;
        if (totalPorts == 1)
            return edgeStart + edgeLength / 2;

        var spacing = edgeLength / (totalPorts + 1);
        return edgeStart + spacing * (portIndex + 1);
    }

    /// <summary>
    /// Gets the hit test radius for ports (includes padding for easier clicking).
    /// </summary>
    public double GetPortHitRadius()
    {
        return _settings.PortSize / 2 + PortHitPadding;
    }

    #endregion

    #region Edge Geometry

    /// <summary>
    /// Calculates the start and end points for an edge in canvas coordinates.
    /// </summary>
    public (AvaloniaPoint start, AvaloniaPoint end) GetEdgeEndpoints(Edge edge, Graph graph)
    {
        var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return (default, default);

        return GetEdgeEndpoints(edge, sourceNode, targetNode);
    }

    /// <summary>
    /// Calculates the start and end points for an edge in canvas coordinates.
    /// OPTIMIZED: Use this overload when you already have the source/target nodes looked up.
    /// </summary>
    public (AvaloniaPoint start, AvaloniaPoint end) GetEdgeEndpoints(Edge edge, Node sourceNode, Node targetNode)
    {
        var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
        var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);
        if (sourcePortIndex < 0) sourcePortIndex = 0;
        if (targetPortIndex < 0) targetPortIndex = 0;

        var startPoint = GetPortPositionByIndex(sourceNode, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count), true);
        var endPoint = GetPortPositionByIndex(targetNode, targetPortIndex, Math.Max(1, targetNode.Inputs.Count), false);

        return (startPoint, endPoint);
    }

    /// <summary>
    /// Calculates bezier control points for an edge.
    /// </summary>
    public (AvaloniaPoint cp1, AvaloniaPoint cp2) GetBezierControlPoints(AvaloniaPoint start, AvaloniaPoint end)
    {
        var dx = end.X - start.X;
        var controlOffset = Math.Max(50, Math.Abs(dx) * 0.5);
        controlOffset = Math.Min(controlOffset, 150);

        var cp1 = new AvaloniaPoint(start.X + controlOffset, start.Y);
        var cp2 = new AvaloniaPoint(end.X - controlOffset, end.Y);

        return (cp1, cp2);
    }

    /// <summary>
    /// Gets the midpoint of an edge (for label positioning).
    /// </summary>
    public AvaloniaPoint GetEdgeMidpoint(AvaloniaPoint start, AvaloniaPoint end)
    {
        return new AvaloniaPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
    }

    #endregion

    #region Group Geometry

    /// <summary>
    /// Gets the collapse button bounds for a group in canvas coordinates.
    /// </summary>
    public Rect GetGroupCollapseButtonBounds(Node group)
    {
        var bounds = GetNodeBounds(group);
        return new Rect(
            bounds.X + GroupHeaderMarginX,
            bounds.Y + GroupHeaderMarginY,
            GroupCollapseButtonSize,
            GroupCollapseButtonSize);
    }

    /// <summary>
    /// Gets the label position for a group in canvas coordinates.
    /// </summary>
    public AvaloniaPoint GetGroupLabelPosition(Node group)
    {
        var buttonBounds = GetGroupCollapseButtonBounds(group);
        return new AvaloniaPoint(
            buttonBounds.X + buttonBounds.Width + 4,
            buttonBounds.Y);
    }

    #endregion

    #region Resize Handle Geometry

    /// <summary>
    /// Gets all resize handle positions for a node in canvas coordinates.
    /// </summary>
    public IEnumerable<(ResizeHandlePosition position, AvaloniaPoint center)> GetResizeHandlePositions(Node node)
    {
        var bounds = GetNodeBounds(node);
        var cx = bounds.X + bounds.Width / 2;
        var cy = bounds.Y + bounds.Height / 2;

        yield return (ResizeHandlePosition.TopLeft, new AvaloniaPoint(bounds.X, bounds.Y));
        yield return (ResizeHandlePosition.Top, new AvaloniaPoint(cx, bounds.Y));
        yield return (ResizeHandlePosition.TopRight, new AvaloniaPoint(bounds.Right, bounds.Y));
        yield return (ResizeHandlePosition.Right, new AvaloniaPoint(bounds.Right, cy));
        yield return (ResizeHandlePosition.BottomRight, new AvaloniaPoint(bounds.Right, bounds.Bottom));
        yield return (ResizeHandlePosition.Bottom, new AvaloniaPoint(cx, bounds.Bottom));
        yield return (ResizeHandlePosition.BottomLeft, new AvaloniaPoint(bounds.X, bounds.Bottom));
        yield return (ResizeHandlePosition.Left, new AvaloniaPoint(bounds.X, cy));
    }

    /// <summary>
    /// Gets the bounds of a resize handle given its center position.
    /// </summary>
    public Rect GetResizeHandleBounds(AvaloniaPoint center)
    {
        var halfSize = ResizeHandleSize / 2;
        return new Rect(center.X - halfSize, center.Y - halfSize, ResizeHandleSize, ResizeHandleSize);
    }

    #endregion

    #region Visibility

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed ancestor group).
    /// </summary>
    public static bool IsNodeVisible(Graph graph, Node node)
    {
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            var parent = graph.Elements.Nodes.FirstOrDefault(n => n.Id == currentParentId);
            if (parent == null) break;
            if (parent.IsCollapsed) return false;
            currentParentId = parent.ParentGroupId;
        }
        return true;
    }

    /// <summary>
    /// Checks if a node is within visible bounds (for virtualization).
    /// </summary>
    public bool IsInVisibleBounds(Node node, Rect visibleBounds)
    {
        var nodeBounds = GetNodeBounds(node);
        // Add port buffer
        var buffer = _settings.PortSize;
        var expandedBounds = new Rect(
            nodeBounds.X - buffer,
            nodeBounds.Y - buffer,
            nodeBounds.Width + buffer * 2,
            nodeBounds.Height + buffer * 2);

        return visibleBounds.Intersects(expandedBounds);
    }

    /// <summary>
    /// Checks if an edge is within visible bounds (either endpoint visible).
    /// </summary>
    public bool IsEdgeInVisibleBounds(Edge edge, Graph graph, Rect visibleBounds)
    {
        var (start, end) = GetEdgeEndpoints(edge, graph);

        // Check if start point is in bounds
        if (visibleBounds.Contains(start)) return true;

        // Check if end point is in bounds
        if (visibleBounds.Contains(end)) return true;

        // Check if the edge might cross through the visible area
        var edgeBounds = new Rect(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));

        // Expand for bezier curves
        var curveMargin = Math.Max(50, edgeBounds.Width * 0.5);
        edgeBounds = edgeBounds.Inflate(curveMargin);

        return visibleBounds.Intersects(edgeBounds);
    }

    #endregion

    #region Hit Testing

    /// <summary>
    /// Checks if a point (in canvas coords) is within a port's hit area.
    /// </summary>
    public bool IsPointInPort(AvaloniaPoint point, AvaloniaPoint portCenter)
    {
        var hitRadius = GetPortHitRadius();
        var dx = point.X - portCenter.X;
        var dy = point.Y - portCenter.Y;
        return dx * dx + dy * dy <= hitRadius * hitRadius;
    }

    /// <summary>
    /// Checks if a point (in canvas coords) is near a bezier edge.
    /// </summary>
    public bool IsPointNearEdge(AvaloniaPoint point, AvaloniaPoint start, AvaloniaPoint end, double threshold)
    {
        // QUICK BOUNDING BOX REJECTION: Skip expensive bezier calculation if point is far from edge bounds
        // For cubic bezier with horizontal control points placed BETWEEN start and end,
        // the curve stays within the X range of [min(start.X, end.X), max(start.X, end.X)].
        // The Y range can extend beyond start.Y/end.Y due to the curve's arc, but only slightly
        // for horizontal control points. We use a small Y expansion for safety.
        var minX = Math.Min(start.X, end.X) - threshold;
        var maxX = Math.Max(start.X, end.X) + threshold;
        
        // For Y bounds, the bezier can bulge vertically when control points are offset horizontally.
        // The maximum Y deviation is approximately 0.25 * |end.Y - start.Y| for our control point setup.
        var ySpan = Math.Abs(end.Y - start.Y);
        var yBulge = ySpan * 0.25; // Conservative estimate for bezier Y bulge
        var minY = Math.Min(start.Y, end.Y) - threshold - yBulge;
        var maxY = Math.Max(start.Y, end.Y) + threshold + yBulge;
        
        if (point.X < minX || point.X > maxX || point.Y < minY || point.Y > maxY)
        {
            return false; // Point is outside the edge's bounding box
        }
        
        var (cp1, cp2) = GetBezierControlPoints(start, end);
        return DistanceToBezierSquared(point, start, cp1, cp2, end) <= threshold * threshold;
    }

    /// <summary>
    /// Calculates squared distance from a point to a cubic bezier curve.
    /// </summary>
    private static double DistanceToBezierSquared(
        AvaloniaPoint point,
        AvaloniaPoint p0,
        AvaloniaPoint p1,
        AvaloniaPoint p2,
        AvaloniaPoint p3)
    {
        double minDistSq = double.MaxValue;
        const int samples = 50;

        for (int i = 0; i <= samples; i++)
        {
            double t = i / (double)samples;
            var bx = BezierPoint(t, p0.X, p1.X, p2.X, p3.X);
            var by = BezierPoint(t, p0.Y, p1.Y, p2.Y, p3.Y);

            var dx = point.X - bx;
            var dy = point.Y - by;
            var distSq = dx * dx + dy * dy;

            if (distSq < minDistSq)
                minDistSq = distSq;
        }

        return minDistSq;
    }

    private static double BezierPoint(double t, double p0, double p1, double p2, double p3)
    {
        var mt = 1 - t;
        return mt * mt * mt * p0 + 3 * mt * mt * t * p1 + 3 * mt * t * t * p2 + t * t * t * p3;
    }

    /// <summary>
    /// Checks if a point is within a resize handle's hit area.
    /// </summary>
    public bool IsPointInResizeHandle(AvaloniaPoint point, AvaloniaPoint handleCenter)
    {
        var halfSize = ResizeHandleSize / 2 + 2; // Small extra margin for easier clicking
        return Math.Abs(point.X - handleCenter.X) <= halfSize &&
               Math.Abs(point.Y - handleCenter.Y) <= halfSize;
    }

    #endregion
}
