namespace FlowGraph.Core.Routing;

/// <summary>
/// Interface for edge routing algorithms.
/// </summary>
public interface IEdgeRouter
{
    /// <summary>
    /// Calculates waypoints for an edge to route around obstacles.
    /// </summary>
    /// <param name="context">The routing context containing graph information.</param>
    /// <param name="edge">The edge to route.</param>
    /// <returns>A list of waypoints (including start and end points).</returns>
    IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge);
}

/// <summary>
/// Context information for edge routing.
/// </summary>
public class EdgeRoutingContext
{
    /// <summary>
    /// The graph being routed.
    /// </summary>
    public required Graph Graph { get; init; }

    /// <summary>
    /// Default node width when not specified.
    /// </summary>
    public double DefaultNodeWidth { get; init; } = 150;

    /// <summary>
    /// Default node height when not specified.
    /// </summary>
    public double DefaultNodeHeight { get; init; } = 80;

    /// <summary>
    /// Padding around nodes for routing.
    /// </summary>
    public double NodePadding { get; init; } = 10;

    /// <summary>
    /// Gets the bounding rectangle for a node.
    /// </summary>
    public Rect GetNodeBounds(Node node)
    {
        var width = node.Width ?? DefaultNodeWidth;
        var height = node.Height ?? DefaultNodeHeight;
        return new Rect(
            node.Position.X - NodePadding,
            node.Position.Y - NodePadding,
            width + NodePadding * 2,
            height + NodePadding * 2);
    }

    /// <summary>
    /// Gets all obstacle rectangles (nodes that edges should route around).
    /// </summary>
    /// <param name="excludeNodeIds">Node IDs to exclude from obstacles (typically source and target).</param>
    public IEnumerable<Rect> GetObstacles(params string[] excludeNodeIds)
    {
        var excludeSet = new HashSet<string>(excludeNodeIds);
        
        foreach (var node in Graph.Nodes)
        {
            if (excludeSet.Contains(node.Id))
                continue;
                
            // Skip collapsed group children (they're not visible)
            if (!string.IsNullOrEmpty(node.ParentGroupId))
            {
                var parent = Graph.Nodes.FirstOrDefault(n => n.Id == node.ParentGroupId);
                if (parent?.IsCollapsed == true)
                    continue;
            }
            
            yield return GetNodeBounds(node);
        }
    }
}

/// <summary>
/// Represents a rectangle for collision detection.
/// </summary>
public readonly struct Rect
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public Point Center => new(X + Width / 2, Y + Height / 2);

    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Checks if this rectangle intersects with another.
    /// </summary>
    public bool Intersects(Rect other)
    {
        return Left < other.Right && Right > other.Left &&
               Top < other.Bottom && Bottom > other.Top;
    }

    /// <summary>
    /// Checks if this rectangle contains a point.
    /// </summary>
    public bool Contains(Point point)
    {
        return point.X >= Left && point.X <= Right &&
               point.Y >= Top && point.Y <= Bottom;
    }

    /// <summary>
    /// Checks if a line segment intersects this rectangle.
    /// </summary>
    public bool IntersectsLine(Point start, Point end)
    {
        // Check if either endpoint is inside
        if (Contains(start) || Contains(end))
            return true;

        // Check intersection with each edge
        return LineIntersectsLine(start, end, new Point(Left, Top), new Point(Right, Top)) ||
               LineIntersectsLine(start, end, new Point(Right, Top), new Point(Right, Bottom)) ||
               LineIntersectsLine(start, end, new Point(Right, Bottom), new Point(Left, Bottom)) ||
               LineIntersectsLine(start, end, new Point(Left, Bottom), new Point(Left, Top));
    }

    private static bool LineIntersectsLine(Point a1, Point a2, Point b1, Point b2)
    {
        var d1 = CrossProduct(b2 - b1, a1 - b1);
        var d2 = CrossProduct(b2 - b1, a2 - b1);
        var d3 = CrossProduct(a2 - a1, b1 - a1);
        var d4 = CrossProduct(a2 - a1, b2 - a1);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        if (Math.Abs(d1) < 0.0001 && OnSegment(b1, a1, b2)) return true;
        if (Math.Abs(d2) < 0.0001 && OnSegment(b1, a2, b2)) return true;
        if (Math.Abs(d3) < 0.0001 && OnSegment(a1, b1, a2)) return true;
        if (Math.Abs(d4) < 0.0001 && OnSegment(a1, b2, a2)) return true;

        return false;
    }

    private static double CrossProduct(Point a, Point b) => a.X * b.Y - a.Y * b.X;

    private static bool OnSegment(Point p, Point q, Point r)
    {
        return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
               q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
    }

    /// <summary>
    /// Returns an inflated copy of this rectangle.
    /// </summary>
    public Rect Inflate(double amount)
    {
        return new Rect(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);
    }
}
