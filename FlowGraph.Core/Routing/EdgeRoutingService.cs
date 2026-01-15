namespace FlowGraph.Core.Routing;

/// <summary>
/// Provides edge routing services for a graph.
/// </summary>
public class EdgeRoutingService
{
    private readonly IEdgeRouter _directRouter = Routing.DirectRouter.Instance;
    private readonly IEdgeRouter _bezierRouter = Routing.BezierRouter.Instance;
    private readonly IEdgeRouter _orthogonalRouter = new OrthogonalRouter();
    private readonly IEdgeRouter _smartBezierRouter = Routing.SmartBezierRouter.Instance;
    private readonly IEdgeRouter _manualRouter = Routing.ManualRouter.Instance;

    /// <summary>
    /// Whether automatic routing is enabled.
    /// </summary>
    public bool IsRoutingEnabled { get; set; } = false;

    /// <summary>
    /// Default node width for routing calculations.
    /// </summary>
    public double DefaultNodeWidth { get; set; } = 150;

    /// <summary>
    /// Default node height for routing calculations.
    /// </summary>
    public double DefaultNodeHeight { get; set; } = 80;

    /// <summary>
    /// Padding around nodes for routing.
    /// Deprecated: Use Options.NodePadding instead.
    /// </summary>
    public double NodePadding
    {
        get => Options.NodePadding;
        set => Options.NodePadding = value;
    }

    /// <summary>
    /// Routing options controlling corner radius, edge spacing, etc.
    /// </summary>
    public EdgeRoutingOptions Options { get; set; } = new();

    /// <summary>
    /// Gets the direct router instance.
    /// </summary>
    public IEdgeRouter DirectRouter => _directRouter;

    /// <summary>
    /// Gets the simple bezier router instance (no obstacle avoidance).
    /// </summary>
    public IEdgeRouter BezierRouter => _bezierRouter;

    /// <summary>
    /// Gets the orthogonal router instance.
    /// </summary>
    public IEdgeRouter OrthogonalRouter => _orthogonalRouter;

    /// <summary>
    /// Gets the smart bezier router instance (with obstacle avoidance).
    /// </summary>
    public IEdgeRouter SmartBezierRouter => _smartBezierRouter;

    /// <summary>
    /// Gets the manual router instance (user waypoints only).
    /// </summary>
    public IEdgeRouter ManualRouter => _manualRouter;

    /// <summary>
    /// Gets the appropriate router for an edge type.
    /// </summary>
    public IEdgeRouter GetRouter(EdgeType edgeType)
    {
        return edgeType switch
        {
            EdgeType.Step or EdgeType.SmoothStep => _orthogonalRouter,
            EdgeType.Bezier => _smartBezierRouter,
            _ => _directRouter
        };
    }

    /// <summary>
    /// Gets the appropriate router for an edge, considering its routing mode.
    /// </summary>
    /// <param name="edge">The edge to get a router for.</param>
    /// <returns>The appropriate router based on the edge's routing mode and type.</returns>
    public IEdgeRouter GetRouterForEdge(Edge edge)
    {
        return edge.Definition.RoutingMode switch
        {
            Models.EdgeRoutingMode.Manual => _manualRouter,
            Models.EdgeRoutingMode.Guided => GetRouter(edge.Type), // Use type-appropriate router with constraints
            _ => GetRouter(edge.Type) // Auto mode
        };
    }

    /// <summary>
    /// Gets a router by algorithm name.
    /// </summary>
    /// <param name="algorithm">The algorithm to use (Direct, Orthogonal, SmartBezier, Manual).</param>
    /// <param name="fallbackEdgeType">Edge type to use for Auto selection.</param>
    public IEdgeRouter GetRouterByAlgorithm(string algorithm, EdgeType fallbackEdgeType = EdgeType.Bezier)
    {
        return algorithm.ToLowerInvariant() switch
        {
            "direct" => _directRouter,
            "orthogonal" => _orthogonalRouter,
            "smartbezier" or "bezier" => _smartBezierRouter,
            "manual" => _manualRouter,
            "auto" or _ => GetRouter(fallbackEdgeType)
        };
    }

    /// <summary>
    /// Routes an edge through a graph, returning waypoints.
    /// Respects the edge's <see cref="Models.EdgeRoutingMode"/>.
    /// </summary>
    public IReadOnlyList<Point> RouteEdge(Graph graph, Edge edge)
    {
        // Manual mode always uses manual router regardless of IsRoutingEnabled
        if (edge.Definition.RoutingMode == Models.EdgeRoutingMode.Manual)
        {
            var manualContext = CreateContext(graph, edge);
            return _manualRouter.Route(manualContext, edge);
        }

        if (!IsRoutingEnabled && edge.Definition.RoutingMode == Models.EdgeRoutingMode.Auto)
        {
            // Return simple start/end points
            return GetSimpleRoute(graph, edge);
        }

        var context = CreateContext(graph, edge);
        var router = GetRouterForEdge(edge);
        return router.Route(context, edge);
    }

    /// <summary>
    /// Routes an edge using a specific router.
    /// </summary>
    /// <param name="graph">The graph containing the edge.</param>
    /// <param name="edge">The edge to route.</param>
    /// <param name="router">The router to use.</param>
    public IReadOnlyList<Point> RouteEdgeWithRouter(Graph graph, Edge edge, IEdgeRouter router)
    {
        var context = CreateContext(graph, edge);
        return router.Route(context, edge);
    }

    /// <summary>
    /// Routes all edges in a graph.
    /// Respects each edge's <see cref="Models.EdgeRoutingMode"/>.
    /// </summary>
    public Dictionary<string, IReadOnlyList<Point>> RouteAllEdges(Graph graph)
    {
        var result = new Dictionary<string, IReadOnlyList<Point>>();

        foreach (var edge in graph.Elements.Edges)
        {
            result[edge.Id] = RouteEdge(graph, edge);
        }

        return result;
    }

    /// <summary>
    /// Checks if an edge path intersects any obstacles.
    /// </summary>
    public bool DoesPathIntersectNodes(Graph graph, Edge edge, IReadOnlyList<Point> path)
    {
        if (path.Count < 2)
            return false;

        var context = CreateContext(graph);
        var obstacles = context.GetObstacles(edge.Source, edge.Target).ToList();

        for (int i = 0; i < path.Count - 1; i++)
        {
            foreach (var obstacle in obstacles)
            {
                if (obstacle.IntersectsLine(path[i], path[i + 1]))
                    return true;
            }
        }

        return false;
    }

    private EdgeRoutingContext CreateContext(Graph graph, Edge? edge = null)
    {
        // Include user constraints for Guided mode
        IReadOnlyList<Point>? userConstraints = null;
        if (edge?.Definition.RoutingMode == Models.EdgeRoutingMode.Guided)
        {
            userConstraints = edge.State.UserWaypoints;
        }

        return new EdgeRoutingContext
        {
            Graph = graph,
            Options = Options,
            DefaultNodeWidth = DefaultNodeWidth,
            DefaultNodeHeight = DefaultNodeHeight,
            UserConstraints = userConstraints
        };
    }

    private IReadOnlyList<Point> GetSimpleRoute(Graph graph, Edge edge)
    {
        var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return [];

        // Reuse DirectRouter.GetPortPosition to avoid code duplication
        var context = new EdgeRoutingContext
        {
            Graph = graph,
            Options = Options,
            DefaultNodeWidth = DefaultNodeWidth,
            DefaultNodeHeight = DefaultNodeHeight
        };

        var sourcePos = Routing.DirectRouter.GetPortPosition(sourceNode, edge.SourcePort, true, context);
        var targetPos = Routing.DirectRouter.GetPortPosition(targetNode, edge.TargetPort, false, context);

        return [sourcePos, targetPos];
    }
}
