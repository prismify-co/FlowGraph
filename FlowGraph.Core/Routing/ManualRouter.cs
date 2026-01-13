namespace FlowGraph.Core.Routing;

/// <summary>
/// Routes edges using user-defined waypoints without automatic path calculation.
/// Used when <see cref="Models.EdgeRoutingMode.Manual"/> is set.
/// </summary>
/// <remarks>
/// This router simply interpolates through the user's waypoints without
/// obstacle avoidance. For guided routing that respects waypoints while
/// avoiding obstacles, use <see cref="OrthogonalRouter"/> with Guided mode.
/// </remarks>
public class ManualRouter : IEdgeRouter
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static ManualRouter Instance { get; } = new();

    /// <summary>
    /// Whether to smooth corners at waypoints.
    /// </summary>
    public bool SmoothCorners { get; set; }

    /// <summary>
    /// Radius for corner smoothing (if enabled).
    /// </summary>
    public double CornerRadius { get; set; } = 10;

    public IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge)
    {
        var sourceNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return [];

        var sourcePort = DirectRouter.GetPortPosition(sourceNode, edge.SourcePort, true, context);
        var targetPort = DirectRouter.GetPortPosition(targetNode, edge.TargetPort, false, context);

        // Get user waypoints from edge state or context
        var userWaypoints = context.UserConstraints ?? edge.State.UserWaypoints;

        if (userWaypoints == null || userWaypoints.Count == 0)
        {
            // No user waypoints - direct line
            return [sourcePort, targetPort];
        }

        // Build path: source -> user waypoints -> target
        var path = new List<Point>(userWaypoints.Count + 2)
        {
            sourcePort
        };
        path.AddRange(userWaypoints);
        path.Add(targetPort);

        return path;
    }
}
