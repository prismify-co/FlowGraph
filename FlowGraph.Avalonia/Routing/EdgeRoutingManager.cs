using FlowGraph.Core;
using FlowGraph.Core.Routing;

namespace FlowGraph.Avalonia.Routing;

/// <summary>
/// Manages edge routing for the FlowCanvas, integrating the Core routing service with the UI layer.
/// </summary>
public class EdgeRoutingManager
{
    private readonly IFlowCanvasContext _context;
    private readonly Action _refreshEdges;
    private readonly EdgeRoutingService _routingService;

    /// <summary>
    /// Event raised when edges have been routed and need to be re-rendered.
    /// </summary>
    public event EventHandler? EdgesRouted;

    /// <summary>
    /// Creates a new edge routing manager.
    /// </summary>
    /// <param name="context">The context providing access to the graph and settings.</param>
    /// <param name="refreshEdges">Action to call when edges need re-rendering.</param>
    public EdgeRoutingManager(
        IFlowCanvasContext context,
        Action refreshEdges)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _refreshEdges = refreshEdges ?? throw new ArgumentNullException(nameof(refreshEdges));
        _routingService = new EdgeRoutingService();
    }

    // Backwards compatibility constructor
    [Obsolete("Use the constructor that accepts IFlowCanvasContext instead.")]
    public EdgeRoutingManager(
        Func<Graph?> getGraph,
        Func<FlowCanvasSettings> getSettings,
        Action refreshEdges)
        : this(new FuncFlowCanvasContext(getGraph, getSettings), refreshEdges)
    {
    }

    /// <summary>
    /// Gets whether automatic edge routing is currently enabled.
    /// </summary>
    public bool IsEnabled => _context.Settings.AutoRouteEdges;

    /// <summary>
    /// Gets whether routing during drag is enabled.
    /// </summary>
    public bool RouteOnDrag => _context.Settings.RouteEdgesOnDrag;

    /// <summary>
    /// Gets whether newly created edges should be routed.
    /// </summary>
    public bool RouteNewEdges => _context.Settings.RouteNewEdges;

    /// <summary>
    /// Gets the underlying routing service for advanced configuration.
    /// </summary>
    public EdgeRoutingService RoutingService => _routingService;

    /// <summary>
    /// Updates the routing service configuration from settings.
    /// </summary>
    private void SyncSettings()
    {
        var settings = _context.Settings;
        _routingService.IsRoutingEnabled = settings.AutoRouteEdges;
        _routingService.DefaultNodeWidth = settings.NodeWidth;
        _routingService.DefaultNodeHeight = settings.NodeHeight;
        _routingService.NodePadding = settings.RoutingNodePadding;
    }

    /// <summary>
    /// Gets the appropriate router based on settings and edge type.
    /// </summary>
    private IEdgeRouter GetRouterForEdge(Edge edge)
    {
        var settings = _context.Settings;
        return settings.DefaultRouterAlgorithm switch
        {
            RouterAlgorithm.Direct => _routingService.DirectRouter,
            RouterAlgorithm.Orthogonal => _routingService.OrthogonalRouter,
            RouterAlgorithm.SmartBezier => _routingService.SmartBezierRouter,
            RouterAlgorithm.Auto or _ => _routingService.GetRouter(edge.Type)
        };
    }

    /// <summary>
    /// Routes all edges in the graph.
    /// </summary>
    /// <param name="forceRefresh">If true, refreshes edge rendering after routing.</param>
    public void RouteAllEdges(bool forceRefresh = true)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        SyncSettings();

        var routes = _routingService.RouteAllEdges(graph);
        ApplyRoutes(graph, routes);

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Routes edges connected to specific nodes.
    /// </summary>
    /// <param name="nodeIds">IDs of nodes whose connected edges should be routed.</param>
    /// <param name="forceRefresh">If true, refreshes edge rendering after routing.</param>
    public void RouteEdgesForNodes(IEnumerable<string> nodeIds, bool forceRefresh = true)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        SyncSettings();

        var nodeIdSet = nodeIds.ToHashSet();
        var affectedEdges = graph.Edges
            .Where(e => nodeIdSet.Contains(e.Source) || nodeIdSet.Contains(e.Target))
            .ToList();

        foreach (var edge in affectedEdges)
        {
            var waypoints = _routingService.RouteEdge(graph, edge);
            ApplyRouteToEdge(edge, waypoints);
        }

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Routes a single edge.
    /// </summary>
    /// <param name="edge">The edge to route.</param>
    /// <param name="forceRefresh">If true, refreshes edge rendering after routing.</param>
    public void RouteEdge(Edge edge, bool forceRefresh = true)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        SyncSettings();

        var waypoints = _routingService.RouteEdge(graph, edge);
        ApplyRouteToEdge(edge, waypoints);

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Routes a single edge using a specific router algorithm.
    /// </summary>
    /// <param name="edge">The edge to route.</param>
    /// <param name="algorithm">The routing algorithm to use.</param>
    /// <param name="forceRefresh">If true, refreshes edge rendering after routing.</param>
    public void RouteEdgeWithAlgorithm(Edge edge, RouterAlgorithm algorithm, bool forceRefresh = true)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        SyncSettings();

        var router = algorithm switch
        {
            RouterAlgorithm.Direct => _routingService.DirectRouter,
            RouterAlgorithm.Orthogonal => _routingService.OrthogonalRouter,
            RouterAlgorithm.SmartBezier => _routingService.SmartBezierRouter,
            RouterAlgorithm.Auto or _ => _routingService.GetRouter(edge.Type)
        };

        var waypoints = _routingService.RouteEdgeWithRouter(graph, edge, router);
        ApplyRouteToEdge(edge, waypoints);

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Routes edges connected to a single node.
    /// </summary>
    /// <param name="nodeId">ID of the node whose connected edges should be routed.</param>
    /// <param name="forceRefresh">If true, refreshes edge rendering after routing.</param>
    public void RouteEdgesForNode(string nodeId, bool forceRefresh = true)
    {
        RouteEdgesForNodes([nodeId], forceRefresh);
    }

    /// <summary>
    /// Routes a newly created edge if routing is enabled.
    /// </summary>
    /// <param name="edge">The newly created edge.</param>
    /// <param name="forceRefresh">If true, refreshes edge rendering after routing.</param>
    /// <returns>True if the edge was routed, false otherwise.</returns>
    public bool RouteNewEdge(Edge edge, bool forceRefresh = true)
    {
        var settings = _context.Settings;

        // Check if we should route new edges
        if (!settings.AutoRouteEdges || !settings.RouteNewEdges)
        {
            return false;
        }

        var graph = _context.Graph;
        if (graph == null) return false;

        SyncSettings();

        // Use the configured router
        var router = GetRouterForEdge(edge);
        var waypoints = _routingService.RouteEdgeWithRouter(graph, edge, router);
        ApplyRouteToEdge(edge, waypoints);

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }

    /// <summary>
    /// Clears all waypoints from edges, returning them to default paths.
    /// </summary>
    /// <param name="forceRefresh">If true, refreshes edge rendering after clearing.</param>
    public void ClearAllRoutes(bool forceRefresh = true)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        foreach (var edge in graph.Edges)
        {
            edge.Waypoints?.Clear();
        }

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Clears waypoints from edges connected to specific nodes.
    /// </summary>
    /// <param name="nodeIds">IDs of nodes whose connected edges should be cleared.</param>
    /// <param name="forceRefresh">If true, refreshes edge rendering after clearing.</param>
    public void ClearRoutesForNodes(IEnumerable<string> nodeIds, bool forceRefresh = true)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var nodeIdSet = nodeIds.ToHashSet();
        var affectedEdges = graph.Edges
            .Where(e => nodeIdSet.Contains(e.Source) || nodeIdSet.Contains(e.Target));

        foreach (var edge in affectedEdges)
        {
            edge.Waypoints?.Clear();
        }

        if (forceRefresh)
        {
            _refreshEdges();
            EdgesRouted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Called when nodes are being dragged. Re-routes edges if configured to do so.
    /// </summary>
    /// <param name="draggedNodeIds">IDs of nodes being dragged.</param>
    public void OnNodesDragging(IEnumerable<string> draggedNodeIds)
    {
        if (!IsEnabled || !RouteOnDrag) return;

        var settings = _context.Settings;
        if (settings.RouteOnlyAffectedEdges)
        {
            RouteEdgesForNodes(draggedNodeIds, forceRefresh: true);
        }
        else
        {
            RouteAllEdges(forceRefresh: true);
        }
    }

    /// <summary>
    /// Called when node dragging completes. Re-routes edges if configured.
    /// </summary>
    /// <param name="draggedNodeIds">IDs of nodes that were dragged.</param>
    public void OnNodesDragCompleted(IEnumerable<string> draggedNodeIds)
    {
        if (!IsEnabled) return;

        // Always do a final route after drag completes for consistency
        var settings = _context.Settings;
        if (settings.RouteOnlyAffectedEdges)
        {
            RouteEdgesForNodes(draggedNodeIds, forceRefresh: true);
        }
        else
        {
            RouteAllEdges(forceRefresh: true);
        }
    }

    private void ApplyRoutes(Graph graph, Dictionary<string, IReadOnlyList<Point>> routes)
    {
        foreach (var (edgeId, waypoints) in routes)
        {
            var edge = graph.Edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge != null)
            {
                ApplyRouteToEdge(edge, waypoints);
            }
        }
    }

    private void ApplyRouteToEdge(Edge edge, IReadOnlyList<Point> waypoints)
    {
        // Skip start and end points (they're just port positions)
        // Only store intermediate waypoints
        if (waypoints.Count <= 2)
        {
            edge.Waypoints?.Clear();
            return;
        }

        edge.Waypoints ??= new List<Point>();
        edge.Waypoints.Clear();

        // Store intermediate waypoints (skip first and last)
        for (int i = 1; i < waypoints.Count - 1; i++)
        {
            edge.Waypoints.Add(waypoints[i]);
        }
    }
}
