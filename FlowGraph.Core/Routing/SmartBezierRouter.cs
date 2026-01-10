namespace FlowGraph.Core.Routing;

/// <summary>
/// Routes edges around obstacles using smooth curves.
/// Generates waypoints that can be used to create multi-segment bezier curves.
/// </summary>
public class SmartBezierRouter : IEdgeRouter
{
    /// <summary>
    /// Minimum distance to maintain from obstacles.
    /// </summary>
    public double Margin { get; set; } = 20;

    /// <summary>
    /// Minimum horizontal offset from port before routing.
    /// </summary>
    public double MinPortOffset { get; set; } = 50;

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

        // If no obstacles or direct path is clear, use simple bezier
        if (obstacles.Count == 0 || IsDirectPathClear(start, end, obstacles))
        {
            return [start, end];
        }

        // Find path around obstacles
        return FindSmartPath(start, end, obstacles, context);
    }

    private bool IsDirectPathClear(Point start, Point end, List<Rect> obstacles)
    {
        // For bezier curves, check the bounding box of the curve
        var minX = Math.Min(start.X, end.X);
        var maxX = Math.Max(start.X, end.X);
        var minY = Math.Min(start.Y, end.Y);
        var maxY = Math.Max(start.Y, end.Y);

        // Bezier control points extend horizontally
        var controlOffset = Math.Max(MinPortOffset, Math.Abs(end.X - start.X) / 2);
        var bezierBounds = new Rect(
            minX,
            minY - 20,  // Some vertical margin for curve
            maxX - minX + controlOffset * 2,
            maxY - minY + 40);

        foreach (var obs in obstacles)
        {
            if (obs.Intersects(bezierBounds))
            {
                // More detailed check - sample the bezier curve
                if (BezierIntersectsObstacle(start, end, controlOffset, obs))
                    return false;
            }
        }

        return true;
    }

    private bool BezierIntersectsObstacle(Point start, Point end, double controlOffset, Rect obstacle)
    {
        var c1 = new Point(start.X + controlOffset, start.Y);
        var c2 = new Point(end.X - controlOffset, end.Y);

        // Sample the bezier curve at intervals
        const int samples = 20;
        var prevPoint = start;

        for (int i = 1; i <= samples; i++)
        {
            var t = (double)i / samples;
            var point = EvaluateBezier(start, c1, c2, end, t);

            if (obstacle.IntersectsLine(prevPoint, point))
                return true;

            prevPoint = point;
        }

        return false;
    }

    private Point EvaluateBezier(Point p0, Point p1, Point p2, Point p3, double t)
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

        // Determine if we need to go around obstacles
        var blockingObstacles = FindBlockingObstacles(start, end, obstacles);

        if (blockingObstacles.Count == 0)
        {
            path.Add(end);
            return path;
        }

        // Calculate combined bounding box of blocking obstacles
        var combinedBounds = GetCombinedBounds(blockingObstacles);

        // Determine routing direction (go above or below)
        var goAbove = ShouldRouteAbove(start, end, combinedBounds);

        // Create waypoints to route around
        var waypoints = CreateWaypoints(start, end, combinedBounds, goAbove);
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

    private Rect GetCombinedBounds(List<Rect> obstacles)
    {
        var minX = obstacles.Min(o => o.Left);
        var minY = obstacles.Min(o => o.Top);
        var maxX = obstacles.Max(o => o.Right);
        var maxY = obstacles.Max(o => o.Bottom);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private bool ShouldRouteAbove(Point start, Point end, Rect combinedBounds)
    {
        // Calculate distances to go above vs below
        var midY = (start.Y + end.Y) / 2;
        var distanceToTop = Math.Abs(midY - combinedBounds.Top);
        var distanceToBottom = Math.Abs(combinedBounds.Bottom - midY);

        // Also consider if we're starting above or below
        var startAbove = start.Y < combinedBounds.Center.Y;
        var endAbove = end.Y < combinedBounds.Center.Y;

        if (startAbove && endAbove)
            return true;
        if (!startAbove && !endAbove)
            return false;

        return distanceToTop <= distanceToBottom;
    }

    private List<Point> CreateWaypoints(Point start, Point end, Rect bounds, bool goAbove)
    {
        var waypoints = new List<Point>();

        // Calculate the Y level to route at
        var routeY = goAbove
            ? bounds.Top - Margin
            : bounds.Bottom + Margin;

        // Create intermediate points
        var exitX = start.X + MinPortOffset;
        var entryX = end.X - MinPortOffset;

        // If we're routing backwards (end is to the left of start)
        if (end.X < start.X)
        {
            exitX = start.X + MinPortOffset;
            entryX = end.X - MinPortOffset;

            // Need to go further out to avoid crossing
            exitX = Math.Max(exitX, bounds.Right + Margin);
            entryX = Math.Min(entryX, bounds.Left - Margin);
        }

        // First waypoint - exit horizontally from start
        if (Math.Abs(start.Y - routeY) > Margin)
        {
            waypoints.Add(new Point(exitX, start.Y));
            waypoints.Add(new Point(exitX, routeY));
        }
        else
        {
            waypoints.Add(new Point(exitX, routeY));
        }

        // Add horizontal routing segment if needed
        if (Math.Abs(exitX - entryX) > Margin)
        {
            waypoints.Add(new Point(entryX, routeY));
        }

        // Final waypoint - enter horizontally to end
        if (Math.Abs(routeY - end.Y) > Margin)
        {
            waypoints.Add(new Point(entryX, end.Y));
        }

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
