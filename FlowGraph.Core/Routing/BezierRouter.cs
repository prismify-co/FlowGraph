namespace FlowGraph.Core.Routing;

/// <summary>
/// Simple bezier router that creates smooth curves between ports.
/// Does not perform obstacle avoidance - for that, use <see cref="SmartBezierRouter"/>.
/// </summary>
/// <remarks>
/// <para>
/// This router creates direction-aware bezier curves:
/// <list type="bullet">
/// <item>Left-to-right: Standard horizontal control points</item>
/// <item>Vertical edges: Control points along the vertical axis</item>
/// <item>Backward edges: Curves that loop appropriately</item>
/// </list>
/// </para>
/// <para>
/// For obstacle avoidance, use <see cref="SmartBezierRouter"/> which extends
/// this behavior to route around intermediate nodes.
/// </para>
/// </remarks>
public class BezierRouter : IEdgeRouter
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static BezierRouter Instance { get; } = new();

    /// <summary>
    /// Minimum horizontal offset from port for control point placement.
    /// </summary>
    public double MinControlPointOffset { get; set; } = 50;

    /// <inheritdoc />
    public IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge)
    {
        var sourceNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return [];

        var start = GetPortPosition(sourceNode, edge.SourcePort, true, context);
        var end = GetPortPosition(targetNode, edge.TargetPort, false, context);

        // Simple bezier - just return start and end points
        // The rendering layer will create the bezier curve with proper control points
        return [start, end];
    }

    /// <summary>
    /// Calculates bezier control points based on edge direction.
    /// </summary>
    /// <param name="start">Start point.</param>
    /// <param name="end">End point.</param>
    /// <param name="offset">Control point offset from endpoints.</param>
    /// <returns>Tuple of (control point 1, control point 2).</returns>
    public static (Point c1, Point c2) CalculateControlPoints(Point start, Point end, double offset)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        // If edge goes mostly left-to-right, use horizontal control points
        if (dx > Math.Abs(dy) * 0.5)
        {
            return (
                new Point(start.X + offset, start.Y),
                new Point(end.X - offset, end.Y)
            );
        }

        // If edge goes mostly right-to-left (backwards), curve around
        if (dx < -Math.Abs(dy) * 0.5)
        {
            return (
                new Point(start.X + offset, start.Y),
                new Point(end.X - offset, end.Y)
            );
        }

        // For mostly vertical edges
        if (Math.Abs(dx) < 10)
        {
            var verticalDist = Math.Abs(dy) / 3;
            return (
                new Point(start.X, start.Y + (dy > 0 ? verticalDist : -verticalDist)),
                new Point(end.X, end.Y - (dy > 0 ? verticalDist : -verticalDist))
            );
        }

        // Diagonal edges
        if (dx >= 0)
        {
            var hOffset = Math.Min(offset * 0.3, Math.Abs(dx) * 0.5);
            return (
                new Point(start.X + hOffset, start.Y + offset * 0.4),
                new Point(end.X - hOffset, end.Y - offset * 0.4)
            );
        }
        else
        {
            return (
                new Point(start.X + offset * 0.3, start.Y + Math.Abs(dy) * 0.3),
                new Point(end.X - offset * 0.3, end.Y - Math.Abs(dy) * 0.3)
            );
        }
    }

    private static Point GetPortPosition(Node node, string? portId, bool isOutput, EdgeRoutingContext context)
    {
        var nodeWidth = node.Width ?? context.DefaultNodeWidth;
        var nodeHeight = node.Height ?? context.DefaultNodeHeight;

        var ports = isOutput ? node.Outputs : node.Inputs;
        var portIndex = 0;
        if (!string.IsNullOrEmpty(portId))
        {
            var idx = ports.FindIndex(p => p.Id == portId);
            if (idx >= 0) portIndex = idx;
        }

        var totalPorts = Math.Max(1, ports.Count);
        var spacing = nodeHeight / (totalPorts + 1);
        var portY = node.Position.Y + spacing * (portIndex + 1);
        var portX = isOutput ? node.Position.X + nodeWidth : node.Position.X;

        return new Point(portX, portY);
    }
}
