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

    // Debug logging - disabled by default
    private static void Log(string message) { }
    // private static void Log(string message) => 
    //     System.IO.File.AppendAllText(@"C:\temp\flowgraph_debug.log",
    //         $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");

    public IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge)
    {
        var sourceNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
        {
            Log($"[SmartBezierRouter] Edge {edge.Id}: Source or target node not found");
            return [];
        }

        var start = GetPortPosition(sourceNode, edge.SourcePort, true, context);
        var end = GetPortPosition(targetNode, edge.TargetPort, false, context);

        Log($"[SmartBezierRouter] Edge {edge.Id}: {edge.Source} -> {edge.Target}");
        Log($"[SmartBezierRouter]   Source node pos: ({sourceNode.Position.X:F0}, {sourceNode.Position.Y:F0}), size: {sourceNode.Width}x{sourceNode.Height}");
        Log($"[SmartBezierRouter]   Target node pos: ({targetNode.Position.X:F0}, {targetNode.Position.Y:F0}), size: {targetNode.Width}x{targetNode.Height}");
        Log($"[SmartBezierRouter]   Start port: ({start.X:F0}, {start.Y:F0}), End port: ({end.X:F0}, {end.Y:F0})");

        // Get obstacles (excluding source and target nodes)
        var obstacles = context.GetObstacles(edge.Source, edge.Target).ToList();
        Log($"[SmartBezierRouter]   Found {obstacles.Count} potential obstacles");
        foreach (var obs in obstacles)
        {
            Log($"[SmartBezierRouter]     Obstacle: ({obs.Left:F0},{obs.Top:F0}) to ({obs.Right:F0},{obs.Bottom:F0}) = {obs.Width:F0}x{obs.Height:F0}");
        }

        // If no obstacles or direct path is clear, use simple bezier
        if (obstacles.Count == 0)
        {
            Log($"[SmartBezierRouter]   No obstacles - returning direct path");
            return [start, end];
        }

        var isPathClear = IsDirectPathClear(start, end, obstacles);
        Log($"[SmartBezierRouter]   IsDirectPathClear: {isPathClear}");

        if (isPathClear)
        {
            Log($"[SmartBezierRouter]   Path is clear - returning direct path");
            return [start, end];
        }

        // Find path around obstacles
        Log($"[SmartBezierRouter]   Finding smart path around obstacles...");
        var result = FindSmartPath(start, end, obstacles, context);
        Log($"[SmartBezierRouter]   Smart path has {result.Count} points");
        foreach (var pt in result)
        {
            Log($"[SmartBezierRouter]     Point: ({pt.X:F0}, {pt.Y:F0})");
        }
        return result;
    }

    private bool IsDirectPathClear(Point start, Point end, List<Rect> obstacles)
    {
        // For bezier curves, check the bounding box of the curve
        var controlOffset = Math.Max(MinPortOffset, Math.Abs(end.X - start.X) / 2);

        // Calculate direction-aware control points
        var (c1, c2) = CalculateControlPoints(start, end, controlOffset);

        // The bezier bounds must include all control points
        var minX = Math.Min(Math.Min(start.X, end.X), Math.Min(c1.X, c2.X));
        var maxX = Math.Max(Math.Max(start.X, end.X), Math.Max(c1.X, c2.X));
        var minY = Math.Min(Math.Min(start.Y, end.Y), Math.Min(c1.Y, c2.Y));
        var maxY = Math.Max(Math.Max(start.Y, end.Y), Math.Max(c1.Y, c2.Y));

        var bezierBounds = new Rect(
            minX - Margin,
            minY - 20,  // Some vertical margin for curve
            maxX - minX + Margin * 2,
            maxY - minY + 40);

        Log($"[SmartBezierRouter.IsDirectPathClear] BezierBounds: ({bezierBounds.Left:F0},{bezierBounds.Top:F0}) to ({bezierBounds.Right:F0},{bezierBounds.Bottom:F0})");
        Log($"[SmartBezierRouter.IsDirectPathClear]   Control points: c1=({c1.X:F0},{c1.Y:F0}), c2=({c2.X:F0},{c2.Y:F0})");

        foreach (var obs in obstacles)
        {
            Log($"[SmartBezierRouter.IsDirectPathClear]   Checking obstacle: ({obs.Left:F0},{obs.Top:F0}) to ({obs.Right:F0},{obs.Bottom:F0})");

            if (obs.Intersects(bezierBounds))
            {
                Log($"[SmartBezierRouter.IsDirectPathClear]   -> Bounds intersect, checking bezier curve...");
                // More detailed check - sample the bezier curve
                if (BezierIntersectsObstacle(start, end, controlOffset, obs))
                {
                    Log($"[SmartBezierRouter.IsDirectPathClear]   -> BEZIER INTERSECTS OBSTACLE!");
                    return false;
                }
                Log($"[SmartBezierRouter.IsDirectPathClear]   -> Bezier does NOT intersect");
            }
            else
            {
                Log($"[SmartBezierRouter.IsDirectPathClear]   -> Bounds do NOT intersect");
            }
        }

        return true;
    }

    private bool BezierIntersectsObstacle(Point start, Point end, double controlOffset, Rect obstacle)
    {
        // Calculate direction-aware control points
        var (c1, c2) = CalculateControlPoints(start, end, controlOffset);

        Log($"[BezierIntersectsObstacle] start=({start.X:F0},{start.Y:F0}), end=({end.X:F0},{end.Y:F0})");
        Log($"[BezierIntersectsObstacle] c1=({c1.X:F0},{c1.Y:F0}), c2=({c2.X:F0},{c2.Y:F0})");
        Log($"[BezierIntersectsObstacle] obstacle=({obstacle.Left:F0},{obstacle.Top:F0}) to ({obstacle.Right:F0},{obstacle.Bottom:F0})");

        // Sample the bezier curve at intervals and check for intersection
        // Note: obstacles already include padding from RoutingNodePadding setting
        const int samples = 20;
        var prevPoint = start;

        for (int i = 1; i <= samples; i++)
        {
            var t = (double)i / samples;
            var point = EvaluateBezier(start, c1, c2, end, t);

            Log($"[BezierIntersectsObstacle] t={t:F2}: ({point.X:F0},{point.Y:F0})");

            // Check direct intersection with obstacle (already padded)
            if (obstacle.IntersectsLine(prevPoint, point))
            {
                Log($"[BezierIntersectsObstacle] INTERSECTION at t={t:F2}");
                return true;
            }

            // Check if the curve actually passes through the obstacle
            if (obstacle.Contains(point))
            {
                Log($"[BezierIntersectsObstacle] POINT INSIDE OBSTACLE at t={t:F2}");
                return true;
            }

            prevPoint = point;
        }

        Log($"[BezierIntersectsObstacle] No intersection found");
        return false;
    }

    /// <summary>
    /// Calculates bezier control points based on edge direction.
    /// For left-to-right edges, uses horizontal control points.
    /// For vertical/backward edges, uses more appropriate control point placement.
    /// </summary>
    private static (Point c1, Point c2) CalculateControlPoints(Point start, Point end, double offset)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        // If edge goes mostly left-to-right, use horizontal control points
        if (dx > Math.Abs(dy) * 0.5)
        {
            // Standard left-to-right bezier
            return (
                new Point(start.X + offset, start.Y),
                new Point(end.X - offset, end.Y)
            );
        }

        // If edge goes mostly right-to-left (backwards), curve around
        if (dx < -Math.Abs(dy) * 0.5)
        {
            // Both control points go outward to create a looping curve
            return (
                new Point(start.X + offset, start.Y),
                new Point(end.X - offset, end.Y)
            );
        }

        // For mostly vertical edges
        // Check if it's a pure vertical edge (same X coordinate)
        if (Math.Abs(dx) < 10)
        {
            // Pure vertical - keep control points on the same X line
            var verticalDist = Math.Abs(dy) / 3;
            return (
                new Point(start.X, start.Y + (dy > 0 ? verticalDist : -verticalDist)),
                new Point(end.X, end.Y - (dy > 0 ? verticalDist : -verticalDist))
            );
        }

        if (dx >= 0)
        {
            // Going down-right - slight horizontal offset
            var hOffset = Math.Min(offset * 0.3, Math.Abs(dx) * 0.5);
            return (
                new Point(start.X + hOffset, start.Y + offset * 0.4),
                new Point(end.X - hOffset, end.Y - offset * 0.4)
            );
        }
        else
        {
            // Going down-left - create a smooth arc that doesn't swing too wide
            return (
                new Point(start.X + offset * 0.3, start.Y + Math.Abs(dy) * 0.3),
                new Point(end.X - offset * 0.3, end.Y - Math.Abs(dy) * 0.3)
            );
        }
    }

    private static bool IsPointNearObstacle(Point point, Rect obstacle, double margin)
    {
        // Check if point is within margin distance of the obstacle rectangle
        var expandedObstacle = new Rect(
            obstacle.Left - margin,
            obstacle.Top - margin,
            obstacle.Width + margin * 2,
            obstacle.Height + margin * 2);

        return expandedObstacle.Contains(point);
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

        // Calculate combined bounding box of blocking obstacles only
        var blockingBounds = GetCombinedBounds(blockingObstacles);

        // Check if the edge stays mostly to the right of blocking obstacles
        // In this case, route around the right side instead of going all the way around
        var minEdgeX = Math.Min(start.X, end.X);
        var edgeIsRightOfObstacles = minEdgeX >= blockingBounds.Right - Margin;

        if (edgeIsRightOfObstacles)
        {
            Log($"[SmartBezierRouter.FindSmartPath] Edge is right of obstacles - routing right side");
            // Simple right-side routing - just go out and down/up
            var routeX = Math.Max(start.X, end.X) + MinPortOffset;

            // Add intermediate points
            path.Add(new Point(routeX, start.Y));
            path.Add(new Point(routeX, end.Y));
            path.Add(end);
            return path;
        }

        // Otherwise, calculate combined bounding box of ALL obstacles
        var allObstaclesBounds = GetCombinedBounds(obstacles);

        // Determine routing direction - prefer the route with more space
        var goAbove = ShouldRouteAbove(start, end, blockingBounds, allObstaclesBounds);

        Log($"[SmartBezierRouter.FindSmartPath] goAbove={goAbove}, blockingBounds=({blockingBounds.Left:F0},{blockingBounds.Top:F0})-({blockingBounds.Right:F0},{blockingBounds.Bottom:F0})");
        Log($"[SmartBezierRouter.FindSmartPath] allObstaclesBounds=({allObstaclesBounds.Left:F0},{allObstaclesBounds.Top:F0})-({allObstaclesBounds.Right:F0},{allObstaclesBounds.Bottom:F0})");

        // Create waypoints to route around ALL obstacles
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

    private Rect GetCombinedBounds(List<Rect> obstacles)
    {
        var minX = obstacles.Min(o => o.Left);
        var minY = obstacles.Min(o => o.Top);
        var maxX = obstacles.Max(o => o.Right);
        var maxY = obstacles.Max(o => o.Bottom);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private bool ShouldRouteAbove(Point start, Point end, Rect blockingBounds, Rect allObstaclesBounds)
    {
        // Calculate available space above and below ALL obstacles
        var spaceAbove = allObstaclesBounds.Top;  // Space from top of canvas to top of obstacles
        var minY = Math.Min(start.Y, end.Y);
        var maxY = Math.Max(start.Y, end.Y);

        // Check if routing above is feasible (start and end are above the blocking area, or there's more space above)
        var startAboveBlocking = start.Y <= blockingBounds.Top;
        var endAboveBlocking = end.Y <= blockingBounds.Top;

        // If we're connecting two nodes that are in the top row (above the blocking obstacle),
        // we should route ABOVE
        if (startAboveBlocking || endAboveBlocking)
        {
            // Check if there's enough space above (at least Margin * 2)
            if (allObstaclesBounds.Top >= Margin * 2)
            {
                Log($"[ShouldRouteAbove] Routing ABOVE - start/end near top, space above = {allObstaclesBounds.Top:F0}");
                return true;
            }
        }

        // Otherwise, route above if there's more space above than below
        // For this, we compare distance from our path to the edges of obstacle bounds
        var distToTop = allObstaclesBounds.Top;
        var distToBottom = 1000 - allObstaclesBounds.Bottom; // Assume canvas is ~1000 tall

        var routeAbove = distToTop >= Margin * 2;
        Log($"[ShouldRouteAbove] distToTop={distToTop:F0}, distToBottom={distToBottom:F0}, routeAbove={routeAbove}");

        return routeAbove;
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
