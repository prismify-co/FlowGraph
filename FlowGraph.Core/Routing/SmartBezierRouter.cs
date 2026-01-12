namespace FlowGraph.Core.Routing;

/// <summary>
/// Routes edges around obstacles using smooth curves with obstacle avoidance.
/// Generates waypoints that can be used to create multi-segment bezier curves.
/// </summary>
/// <remarks>
/// <para>
/// SmartBezierRouter extends basic bezier routing with obstacle avoidance:
/// <list type="bullet">
/// <item>Detects nodes blocking the direct path</item>
/// <item>Routes around obstacles going above or below based on available space</item>
/// <item>Maintains smooth curves through waypoints</item>
/// </list>
/// </para>
/// <para>
/// For simple bezier curves without obstacle avoidance, use <see cref="BezierRouter"/>.
/// </para>
/// </remarks>
public class SmartBezierRouter : IEdgeRouter
{
    /// <summary>
    /// Singleton instance with default settings.
    /// </summary>
    public static SmartBezierRouter Instance { get; } = new();

    /// <summary>
    /// Minimum distance to maintain from obstacles.
    /// </summary>
    public double Margin { get; set; } = 20;

    /// <summary>
    /// Minimum horizontal offset from port before routing can turn.
    /// </summary>
    public double MinPortOffset { get; set; } = 50;

    /// <inheritdoc />
    public IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge)
    {
        var sourceNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return [];

        var start = GetPortPosition(sourceNode, edge.SourcePort, true, context);
        var end = GetPortPosition(targetNode, edge.TargetPort, false, context);

        // Get obstacles (excluding source and target nodes)
        var obstacles = context.GetObstacles(edge.Source, edge.Target).ToList();

        // If no obstacles, return direct path
        if (obstacles.Count == 0)
            return [start, end];

        // Check if direct bezier path is clear
        if (IsDirectPathClear(start, end, obstacles))
            return [start, end];

        // Find path around obstacles
        return FindSmartPath(start, end, obstacles, context);
    }

    private bool IsDirectPathClear(Point start, Point end, List<Rect> obstacles)
    {
        var controlOffset = Math.Max(MinPortOffset, Math.Abs(end.X - start.X) / 2);
        var (c1, c2) = BezierRouter.CalculateControlPoints(start, end, controlOffset);

        // Calculate bezier bounding box including control points
        var minX = Math.Min(Math.Min(start.X, end.X), Math.Min(c1.X, c2.X));
        var maxX = Math.Max(Math.Max(start.X, end.X), Math.Max(c1.X, c2.X));
        var minY = Math.Min(Math.Min(start.Y, end.Y), Math.Min(c1.Y, c2.Y));
        var maxY = Math.Max(Math.Max(start.Y, end.Y), Math.Max(c1.Y, c2.Y));

        var bezierBounds = new Rect(
            minX - Margin,
            minY - 20,
            maxX - minX + Margin * 2,
            maxY - minY + 40);

        foreach (var obs in obstacles)
        {
            if (obs.Intersects(bezierBounds))
            {
                if (BezierIntersectsObstacle(start, end, controlOffset, obs))
                    return false;
            }
        }

        return true;
    }

    private bool BezierIntersectsObstacle(Point start, Point end, double controlOffset, Rect obstacle)
    {
        var (c1, c2) = BezierRouter.CalculateControlPoints(start, end, controlOffset);

        // Sample the bezier curve at intervals
        const int samples = 20;
        var prevPoint = start;

        for (int i = 1; i <= samples; i++)
        {
            var t = (double)i / samples;
            var point = EvaluateBezier(start, c1, c2, end, t);

            if (obstacle.IntersectsLine(prevPoint, point) || obstacle.Contains(point))
                return true;

            prevPoint = point;
        }

        return false;
    }

    private static Point EvaluateBezier(Point p0, Point p1, Point p2, Point p3, double t)
    {
        var u = 1 - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        return new Point(
            uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X,
            uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y);
    }

    private List<Point> FindSmartPath(Point start, Point end, List<Rect> obstacles, EdgeRoutingContext context)
    {
        var path = new List<Point> { start };

        var blockingObstacles = FindBlockingObstacles(start, end, obstacles);
        if (blockingObstacles.Count == 0)
        {
            path.Add(end);
            return path;
        }

        var blockingBounds = GetCombinedBounds(blockingObstacles);

        // Check if edge is to the right of all obstacles
        var minEdgeX = Math.Min(start.X, end.X);
        if (minEdgeX >= blockingBounds.Right - Margin)
        {
            var routeX = Math.Max(start.X, end.X) + MinPortOffset;
            path.Add(new Point(routeX, start.Y));
            path.Add(new Point(routeX, end.Y));
            path.Add(end);
            return path;
        }

        var allObstaclesBounds = GetCombinedBounds(obstacles);
        var goAbove = ShouldRouteAbove(start, end, blockingBounds, allObstaclesBounds);
        var waypoints = CreateWaypoints(start, end, allObstaclesBounds, goAbove);
        path.AddRange(waypoints);
        path.Add(end);
        return path;
    }

    private List<Rect> FindBlockingObstacles(Point start, Point end, List<Rect> obstacles)
    {
        var blocking = new List<Rect>();
        var controlOffset = Math.Max(MinPortOffset, Math.Abs(end.X - start.X) / 2);

        foreach (var obs in obstacles)
        {
            if (BezierIntersectsObstacle(start, end, controlOffset, obs))
                blocking.Add(obs);
        }

        return blocking;
    }

    private static Rect GetCombinedBounds(List<Rect> obstacles)
    {
        var minX = obstacles.Min(o => o.Left);
        var minY = obstacles.Min(o => o.Top);
        var maxX = obstacles.Max(o => o.Right);
        var maxY = obstacles.Max(o => o.Bottom);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private bool ShouldRouteAbove(Point start, Point end, Rect blockingBounds, Rect allObstaclesBounds)
    {
        var startAboveBlocking = start.Y <= blockingBounds.Top;
        var endAboveBlocking = end.Y <= blockingBounds.Top;

        if ((startAboveBlocking || endAboveBlocking) && allObstaclesBounds.Top >= Margin * 2)
            return true;

        return allObstaclesBounds.Top >= Margin * 2;
    }

    private List<Point> CreateWaypoints(Point start, Point end, Rect bounds, bool goAbove)
    {
        var waypoints = new List<Point>();
        var routeY = goAbove ? bounds.Top - Margin : bounds.Bottom + Margin;
        var exitX = start.X + MinPortOffset;
        var entryX = end.X - MinPortOffset;

        if (end.X < start.X)
        {
            exitX = Math.Max(start.X + MinPortOffset, bounds.Right + Margin);
            entryX = Math.Min(end.X - MinPortOffset, bounds.Left - Margin);
        }

        if (Math.Abs(start.Y - routeY) > Margin)
        {
            waypoints.Add(new Point(exitX, start.Y));
            waypoints.Add(new Point(exitX, routeY));
        }
        else
        {
            waypoints.Add(new Point(exitX, routeY));
        }

        if (Math.Abs(exitX - entryX) > Margin)
            waypoints.Add(new Point(entryX, routeY));

        if (Math.Abs(routeY - end.Y) > Margin)
            waypoints.Add(new Point(entryX, end.Y));

        return waypoints;
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
