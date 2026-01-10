namespace FlowGraph.Core.Routing;

/// <summary>
/// Routes edges using orthogonal (right-angle) paths that avoid obstacles.
/// Uses A* pathfinding on a visibility graph.
/// </summary>
public class OrthogonalRouter : IEdgeRouter
{
    /// <summary>
    /// Minimum distance to maintain from obstacles.
    /// </summary>
    public double Margin { get; set; } = 15;

    /// <summary>
    /// Preferred distance for horizontal segments from source/target.
    /// </summary>
    public double PreferredOffset { get; set; } = 30;

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

        // If no obstacles or path is clear, use simple routing
        if (obstacles.Count == 0 || !PathHasObstacles(start, end, obstacles))
        {
            return CreateSimpleOrthogonalPath(start, end);
        }

        // Use A* to find path through visibility graph
        var path = FindOrthogonalPath(start, end, obstacles);

        return path.Count > 0 ? path : CreateSimpleOrthogonalPath(start, end);
    }

    private bool PathHasObstacles(Point start, Point end, List<Rect> obstacles)
    {
        // Check if a simple orthogonal path would intersect any obstacles
        var midX = (start.X + end.X) / 2;

        // Check horizontal segment from start
        foreach (var obs in obstacles)
        {
            if (obs.IntersectsLine(start, new Point(midX, start.Y)))
                return true;
            if (obs.IntersectsLine(new Point(midX, start.Y), new Point(midX, end.Y)))
                return true;
            if (obs.IntersectsLine(new Point(midX, end.Y), end))
                return true;
        }

        return false;
    }

    private List<Point> CreateSimpleOrthogonalPath(Point start, Point end)
    {
        var midX = (start.X + end.X) / 2;

        // Ensure minimum horizontal segment from ports
        if (midX < start.X + PreferredOffset)
            midX = start.X + PreferredOffset;
        if (midX > end.X - PreferredOffset)
            midX = Math.Max(start.X + PreferredOffset, end.X - PreferredOffset);

        return
        [
            start,
            new Point(midX, start.Y),
            new Point(midX, end.Y),
            end
        ];
    }

    private List<Point> FindOrthogonalPath(Point start, Point end, List<Rect> obstacles)
    {
        // Build visibility graph nodes from obstacle corners
        var graphNodes = BuildGraphNodes(start, end, obstacles);

        // A* search
        var path = AStarSearch(start, end, graphNodes, obstacles);

        if (path.Count == 0)
            return [];

        // Simplify path by removing unnecessary waypoints
        return SimplifyPath(path);
    }

    private List<Point> BuildGraphNodes(Point start, Point end, List<Rect> obstacles)
    {
        var nodes = new List<Point> { start, end };

        foreach (var obs in obstacles)
        {
            // Add corner points with margin
            nodes.Add(new Point(obs.Left - Margin, obs.Top - Margin));
            nodes.Add(new Point(obs.Right + Margin, obs.Top - Margin));
            nodes.Add(new Point(obs.Left - Margin, obs.Bottom + Margin));
            nodes.Add(new Point(obs.Right + Margin, obs.Bottom + Margin));

            // Add midpoints on edges for better orthogonal routing
            var midY = (obs.Top + obs.Bottom) / 2;
            var midX = (obs.Left + obs.Right) / 2;
            nodes.Add(new Point(obs.Left - Margin, midY));
            nodes.Add(new Point(obs.Right + Margin, midY));
            nodes.Add(new Point(midX, obs.Top - Margin));
            nodes.Add(new Point(midX, obs.Bottom + Margin));
        }

        // Add horizontal/vertical projections from start and end
        nodes.Add(new Point(start.X + PreferredOffset, start.Y));
        nodes.Add(new Point(end.X - PreferredOffset, end.Y));

        return nodes;
    }

    private List<Point> AStarSearch(Point start, Point end, List<Point> graphNodes, List<Rect> obstacles)
    {
        var openSet = new PriorityQueue<Point, double>();
        var cameFrom = new Dictionary<Point, Point>();
        var gScore = new Dictionary<Point, double> { [start] = 0 };
        var fScore = new Dictionary<Point, double> { [start] = OrthogonalDistance(start, end) };

        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (OrthogonalDistance(current, end) < 1)
            {
                return ReconstructPath(cameFrom, current, end);
            }

            // Get neighbors (orthogonally reachable points)
            var neighbors = GetOrthogonalNeighbors(current, graphNodes, end, obstacles);

            foreach (var neighbor in neighbors)
            {
                var tentativeG = gScore[current] + OrthogonalDistance(current, neighbor);

                if (!gScore.TryGetValue(neighbor, out var neighborG) || tentativeG < neighborG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    var f = tentativeG + OrthogonalDistance(neighbor, end);
                    fScore[neighbor] = f;
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

        return []; // No path found
    }

    private IEnumerable<Point> GetOrthogonalNeighbors(Point current, List<Point> graphNodes, Point end, List<Rect> obstacles)
    {
        var neighbors = new List<Point>();

        // Always try to reach the end point
        if (CanReachOrthogonally(current, end, obstacles))
            neighbors.Add(end);

        foreach (var node in graphNodes)
        {
            if (node == current)
                continue;

            if (CanReachOrthogonally(current, node, obstacles))
                neighbors.Add(node);
        }

        return neighbors;
    }

    private bool CanReachOrthogonally(Point from, Point to, List<Rect> obstacles)
    {
        // Check if we can reach via horizontal-then-vertical or vertical-then-horizontal
        var corner1 = new Point(to.X, from.Y);
        var corner2 = new Point(from.X, to.Y);

        // Try horizontal-first
        if (!LineIntersectsObstacles(from, corner1, obstacles) &&
            !LineIntersectsObstacles(corner1, to, obstacles))
            return true;

        // Try vertical-first
        if (!LineIntersectsObstacles(from, corner2, obstacles) &&
            !LineIntersectsObstacles(corner2, to, obstacles))
            return true;

        // Also check direct orthogonal connections (same X or Y)
        if (Math.Abs(from.X - to.X) < 0.1 || Math.Abs(from.Y - to.Y) < 0.1)
        {
            if (!LineIntersectsObstacles(from, to, obstacles))
                return true;
        }

        return false;
    }

    private bool LineIntersectsObstacles(Point start, Point end, List<Rect> obstacles)
    {
        foreach (var obs in obstacles)
        {
            if (obs.IntersectsLine(start, end))
                return true;
        }
        return false;
    }

    private List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current, Point end)
    {
        var path = new List<Point> { end };

        if (current != end)
            path.Insert(0, current);

        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Insert(0, prev);
            current = prev;
        }

        return path;
    }

    private List<Point> SimplifyPath(List<Point> path)
    {
        if (path.Count <= 2)
            return path;

        var simplified = new List<Point> { path[0] };

        for (int i = 1; i < path.Count - 1; i++)
        {
            var prev = simplified[^1];
            var curr = path[i];
            var next = path[i + 1];

            // Keep point if direction changes
            var directionChanges =
                (Math.Abs(prev.X - curr.X) > 0.1 && Math.Abs(curr.X - next.X) < 0.1) ||
                (Math.Abs(prev.Y - curr.Y) > 0.1 && Math.Abs(curr.Y - next.Y) < 0.1) ||
                (Math.Abs(prev.X - curr.X) < 0.1 && Math.Abs(curr.X - next.X) > 0.1) ||
                (Math.Abs(prev.Y - curr.Y) < 0.1 && Math.Abs(curr.Y - next.Y) > 0.1);

            if (directionChanges)
                simplified.Add(curr);
        }

        simplified.Add(path[^1]);
        return simplified;
    }

    private static double OrthogonalDistance(Point a, Point b)
    {
        // Manhattan distance for orthogonal paths
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static Point GetPortPosition(Node node, string? portId, bool isOutput, EdgeRoutingContext context)
    {
        return DirectRouter.Instance.Route(context, new Edge
        {
            Source = node.Id,
            Target = node.Id,
            SourcePort = portId!,
            TargetPort = portId!
        })[0];
    }
}
