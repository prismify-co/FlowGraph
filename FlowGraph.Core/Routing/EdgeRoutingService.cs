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
    /// Gets a router by algorithm name.
    /// </summary>
    /// <param name="algorithm">The algorithm to use (Direct, Orthogonal, SmartBezier).</param>
    /// <param name="fallbackEdgeType">Edge type to use for Auto selection.</param>
    public IEdgeRouter GetRouterByAlgorithm(string algorithm, EdgeType fallbackEdgeType = EdgeType.Bezier)
    {
        return algorithm.ToLowerInvariant() switch
        {
            "direct" => _directRouter,
            "orthogonal" => _orthogonalRouter,
            "smartbezier" or "bezier" => _smartBezierRouter,
            "auto" or _ => GetRouter(fallbackEdgeType)
        };
    }

    /// <summary>
    /// Routes an edge through a graph, returning waypoints.
    /// </summary>
    public IReadOnlyList<Point> RouteEdge(Graph graph, Edge edge)
    {
        if (!IsRoutingEnabled)
        {
            // Return simple start/end points
            return GetSimpleRoute(graph, edge);
        }

        var context = CreateContext(graph);
        var router = GetRouter(edge.Type);
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
        var context = CreateContext(graph);
        return router.Route(context, edge);
    }

    /// <summary>
    /// Routes all edges in a graph.
    /// </summary>
    public Dictionary<string, IReadOnlyList<Point>> RouteAllEdges(Graph graph)
    {
        var result = new Dictionary<string, IReadOnlyList<Point>>();
        var context = CreateContext(graph);

        foreach (var edge in graph.Elements.Edges)
        {
            if (IsRoutingEnabled)
            {
                var router = GetRouter(edge.Type);
                result[edge.Id] = router.Route(context, edge);
            }
            else
            {
                result[edge.Id] = GetSimpleRoute(graph, edge);
            }
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

    private EdgeRoutingContext CreateContext(Graph graph)
    {
        return new EdgeRoutingContext
        {
            Graph = graph,
            Options = Options,
            DefaultNodeWidth = DefaultNodeWidth,
            DefaultNodeHeight = DefaultNodeHeight
        };
    }

    private IReadOnlyList<Point> GetSimpleRoute(Graph graph, Edge edge)
    {
        var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return [];

        var sourcePos = GetPortPosition(sourceNode, edge.SourcePort, true);
        var targetPos = GetPortPosition(targetNode, edge.TargetPort, false);

        return [sourcePos, targetPos];
    }

    private Point GetPortPosition(Node node, string? portId, bool isOutput)
    {
        var nodeWidth = node.Width ?? DefaultNodeWidth;
        var nodeHeight = node.Height ?? DefaultNodeHeight;

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
