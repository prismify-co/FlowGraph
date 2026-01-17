using System.Collections.Specialized;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Events;

namespace FlowGraph.Core;

/// <summary>
/// Represents a flow graph containing nodes, edges, and other canvas elements.
/// Provides methods for adding, removing, and querying graph elements.
/// </summary>
/// <remarks>
/// <para>
/// The Graph class uses the canvas-first architecture where the <see cref="Elements"/>
/// collection is the single source of truth. All element types (nodes, edges, shapes, etc.)
/// are stored in this unified collection.
/// </para>
/// <para>
/// <see cref="Nodes"/> and <see cref="Edges"/> provide typed read-only views into Elements
/// for convenience when working with graph-specific operations.
/// </para>
/// <para>
/// Subscribe to <see cref="Elements"/>.CollectionChanged for all element changes, or use
/// the convenience events <see cref="NodesChanged"/> and <see cref="EdgesChanged"/> for
/// type-specific notifications.
/// </para>
/// </remarks>
public class Graph
{
    private bool _isBatchLoading;
    private bool _isSubscribedToNodeBounds;
    private EventHandler<NodeBoundsChangedEventArgs>? _nodeBoundsChanged;

    /// <summary>
    /// Initializes a new instance of the Graph class.
    /// </summary>
    public Graph()
    {
        // Forward Elements.CollectionChanged to typed events
        Elements.CollectionChanged += OnElementsCollectionChanged;
    }

    private void OnElementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // For Add/Remove actions, we can determine which typed event to raise
        // For Reset, we raise both events since we don't know what changed
        bool hasNodeChanges = false;
        bool hasEdgeChanges = false;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                foreach (var item in e.NewItems)
                {
                    if (item is Node node)
                    {
                        hasNodeChanges = true;
                        ManageNodeBoundsSubscriptions(node, subscribe: true);
                    }
                    else if (item is Edge) hasEdgeChanges = true;
                }
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (var item in e.OldItems)
                {
                    if (item is Node node)
                    {
                        hasNodeChanges = true;
                        ManageNodeBoundsSubscriptions(node, subscribe: false);
                    }
                    else if (item is Edge) hasEdgeChanges = true;
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                // For Reset, raise both events since we can't tell what changed
                hasNodeChanges = true;
                hasEdgeChanges = true;
                // Note: Reset typically means the collection was cleared or replaced entirely.
                // Previous node subscriptions become orphaned (harmless), and we need to
                // subscribe to whatever nodes remain. This is handled in ManageNodeBoundsSubscriptions
                // when new nodes are added after the reset.
                break;

            case NotifyCollectionChangedAction.Replace:
                // Check both old and new items
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        if (item is Node node)
                        {
                            hasNodeChanges = true;
                            ManageNodeBoundsSubscriptions(node, subscribe: false);
                        }
                        else if (item is Edge) hasEdgeChanges = true;
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is Node node)
                        {
                            hasNodeChanges = true;
                            ManageNodeBoundsSubscriptions(node, subscribe: true);
                        }
                        else if (item is Edge) hasEdgeChanges = true;
                    }
                }
                break;
        }

        if (hasNodeChanges)
            NodesChanged?.Invoke(this, e);
        if (hasEdgeChanges)
            EdgesChanged?.Invoke(this, e);
    }

    #region Collections

    /// <summary>
    /// Gets the collection of all canvas elements (nodes, edges, shapes, etc.).
    /// This is the single source of truth for all graph elements.
    /// </summary>
    /// <remarks>
    /// Subscribe to <c>Elements.CollectionChanged</c> for change notifications.
    /// Use typed accessors <see cref="Nodes"/> and <see cref="Edges"/> for read-only typed access.
    /// </remarks>
    public ElementCollection Elements { get; } = new();

    /// <summary>
    /// Gets a read-only view of all node elements.
    /// </summary>
    /// <remarks>
    /// This is a live view into <see cref="Elements"/>. To add/remove nodes, use
    /// <see cref="AddNode"/>, <see cref="AddNodes"/>, <see cref="RemoveNode"/>, or
    /// modify <see cref="Elements"/> directly.
    /// </remarks>
    public IReadOnlyList<Node> Nodes => Elements.Nodes;

    /// <summary>
    /// Gets a read-only view of all edge elements.
    /// </summary>
    /// <remarks>
    /// This is a live view into <see cref="Elements"/>. To add/remove edges, use
    /// <see cref="AddEdge"/>, <see cref="AddEdges"/>, <see cref="RemoveEdge"/>, or
    /// modify <see cref="Elements"/> directly.
    /// </remarks>
    public IReadOnlyList<Edge> Edges => Elements.Edges;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when nodes are added, removed, or the collection is reset.
    /// This is a convenience event derived from <see cref="Elements"/>.CollectionChanged.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? NodesChanged;

    /// <summary>
    /// Event raised when edges are added, removed, or the collection is reset.
    /// This is a convenience event derived from <see cref="Elements"/>.CollectionChanged.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? EdgesChanged;

    /// <summary>
    /// Event raised when any node's bounds (position or size) change.
    /// Uses lazy subscription - only subscribes to individual node events when this event has subscribers.
    /// Ideal for spatial index invalidation and layout systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event uses a lazy subscription pattern for performance. When no handlers are subscribed,
    /// no per-node subscriptions are maintained. Once a handler subscribes, the Graph automatically
    /// subscribes to all existing nodes' BoundsChanged events and manages subscriptions as nodes
    /// are added/removed.
    /// </para>
    /// <para>
    /// For high-frequency operations like dragging many nodes, consider using explicit invalidation
    /// methods instead of relying on this event, as it fires for each individual node change.
    /// </para>
    /// </remarks>
    public event EventHandler<NodeBoundsChangedEventArgs>? NodeBoundsChanged
    {
        add
        {
            if (!_isSubscribedToNodeBounds && value != null)
            {
                SubscribeToAllNodeBounds();
                _isSubscribedToNodeBounds = true;
            }
            _nodeBoundsChanged += value;
        }
        remove
        {
            _nodeBoundsChanged -= value;
            if (_nodeBoundsChanged == null && _isSubscribedToNodeBounds)
            {
                UnsubscribeFromAllNodeBounds();
                _isSubscribedToNodeBounds = false;
            }
        }
    }

    /// <summary>
    /// Gets whether the graph is currently in batch loading mode.
    /// During batch loading, edge validation is skipped.
    /// </summary>
    public bool IsBatchLoading => _isBatchLoading;

    /// <summary>
    /// Event raised when batch loading completes.
    /// Subscribe to this to refresh UI after bulk operations.
    /// </summary>
    public event EventHandler? BatchLoadCompleted;

    /// <summary>
    /// Begins batch loading mode. Edge validation is skipped
    /// until EndBatchLoad is called. Use this when adding many elements at once.
    /// </summary>
    public void BeginBatchLoad()
    {
        _isBatchLoading = true;
    }

    /// <summary>
    /// Ends batch loading mode and raises BatchLoadCompleted event.
    /// </summary>
    public void EndBatchLoad()
    {
        _isBatchLoading = false;
        BatchLoadCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void SubscribeToAllNodeBounds()
    {
        foreach (var node in Nodes)
        {
            node.BoundsChanged += OnNodeBoundsChanged;
        }
    }

    private void UnsubscribeFromAllNodeBounds()
    {
        foreach (var node in Nodes)
        {
            node.BoundsChanged -= OnNodeBoundsChanged;
        }
    }

    private void OnNodeBoundsChanged(object? sender, BoundsChangedEventArgs e)
    {
        if (sender is Node node)
        {
            _nodeBoundsChanged?.Invoke(this, new NodeBoundsChangedEventArgs(node, e));
        }
    }

    /// <summary>
    /// Called when nodes are added/removed to manage NodeBoundsChanged subscriptions.
    /// </summary>
    private void ManageNodeBoundsSubscriptions(Node node, bool subscribe)
    {
        if (!_isSubscribedToNodeBounds) return;

        if (subscribe)
            node.BoundsChanged += OnNodeBoundsChanged;
        else
            node.BoundsChanged -= OnNodeBoundsChanged;
    }

    #endregion

    #region Element Operations (Primary API)

    /// <summary>
    /// Adds any canvas element to the graph.
    /// </summary>
    /// <param name="element">The element to add.</param>
    public void AddElement(ICanvasElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Elements.Add(element);
    }

    /// <summary>
    /// Removes any canvas element from the graph.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    public void RemoveElement(ICanvasElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Elements.Remove(element);
    }

    /// <summary>
    /// Adds multiple elements at once with a single notification.
    /// </summary>
    /// <param name="elements">The elements to add.</param>
    public void AddElements(IEnumerable<ICanvasElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        Elements.AddRange(elements);
    }

    /// <summary>
    /// Removes multiple elements at once with a single notification.
    /// </summary>
    /// <param name="elements">The elements to remove.</param>
    public void RemoveElements(IEnumerable<ICanvasElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        Elements.RemoveRange(elements);
    }

    #endregion

    #region Node Operations (Convenience API)

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void AddNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Elements.Add(node);
    }

    /// <summary>
    /// Adds multiple nodes at once with a single notification.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    public void AddNodes(IEnumerable<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        Elements.AddRange(nodes);
    }

    /// <summary>
    /// Removes a node and all connected edges from the graph.
    /// </summary>
    /// <param name="nodeId">The ID of the node to remove.</param>
    public void RemoveNode(string nodeId)
    {
        var node = Elements.FindById<Node>(nodeId);
        if (node != null)
        {
            // Remove connected edges first
            var edgesToRemove = Elements.GetEdgesForNode(nodeId).ToList();
            if (edgesToRemove.Count > 0)
            {
                Elements.RemoveRange(edgesToRemove);
            }
            Elements.Remove(node);
        }
    }

    /// <summary>
    /// Removes multiple nodes and their connected edges at once.
    /// </summary>
    /// <param name="nodeIds">The IDs of nodes to remove.</param>
    public void RemoveNodes(IEnumerable<string> nodeIds)
    {
        var nodeIdList = nodeIds.ToList();
        var nodesToRemove = nodeIdList
            .Select(id => Elements.FindById<Node>(id))
            .Where(n => n != null)
            .Cast<Node>()
            .ToList();

        if (nodesToRemove.Count == 0) return;

        // Get all connected edges
        var edgesToRemove = Elements.GetEdgesForNodes(nodeIdList);

        // Remove all in one batch
        var allToRemove = new List<ICanvasElement>();
        allToRemove.AddRange(edgesToRemove);
        allToRemove.AddRange(nodesToRemove);
        Elements.RemoveRange(allToRemove);
    }

    #endregion

    #region Edge Operations (Convenience API)

    /// <summary>
    /// Adds an edge to the graph.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if source or target node doesn't exist (unless in batch loading mode).</exception>
    public void AddEdge(Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        // Skip validation during batch load
        if (!_isBatchLoading)
        {
            var sourceExists = Elements.ContainsId(edge.Source);
            var targetExists = Elements.ContainsId(edge.Target);

            if (!sourceExists || !targetExists)
            {
                throw new InvalidOperationException(
                    $"Source and target nodes must exist before adding an edge. " +
                    $"Source '{edge.Source}' exists: {sourceExists}, Target '{edge.Target}' exists: {targetExists}");
            }
        }

        Elements.Add(edge);
    }

    /// <summary>
    /// Adds multiple edges at once with a single notification.
    /// </summary>
    /// <param name="edges">The edges to add.</param>
    public void AddEdges(IEnumerable<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        Elements.AddRange(edges);
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
    /// <param name="edgeId">The ID of the edge to remove.</param>
    public void RemoveEdge(string edgeId)
    {
        var edge = Elements.FindById<Edge>(edgeId);
        if (edge != null)
        {
            Elements.Remove(edge);
        }
    }

    /// <summary>
    /// Removes multiple edges at once.
    /// </summary>
    /// <param name="edgeIds">The IDs of edges to remove.</param>
    public void RemoveEdges(IEnumerable<string> edgeIds)
    {
        var edgesToRemove = edgeIds
            .Select(id => Elements.FindById<Edge>(id))
            .Where(e => e != null)
            .Cast<Edge>()
            .ToList();

        if (edgesToRemove.Count > 0)
        {
            Elements.RemoveRange(edgesToRemove);
        }
    }

    #endregion

    #region Query Operations

    /// <summary>
    /// Finds a node by its ID.
    /// </summary>
    /// <param name="nodeId">The node ID to search for.</param>
    /// <returns>The node if found; otherwise, null.</returns>
    public Node? FindNode(string nodeId) => Elements.FindById<Node>(nodeId);

    /// <summary>
    /// Finds an edge by its ID.
    /// </summary>
    /// <param name="edgeId">The edge ID to search for.</param>
    /// <returns>The edge if found; otherwise, null.</returns>
    public Edge? FindEdge(string edgeId) => Elements.FindById<Edge>(edgeId);

    /// <summary>
    /// Gets all edges connected to a node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>Enumerable of connected edges.</returns>
    public IEnumerable<Edge> GetEdgesForNode(string nodeId) => Elements.GetEdgesForNode(nodeId);

    /// <summary>
    /// Determines whether a node with the specified ID exists.
    /// </summary>
    /// <param name="nodeId">The node ID to check.</param>
    public bool ContainsNode(string nodeId) => Elements.FindById<Node>(nodeId) != null;

    /// <summary>
    /// Determines whether an edge with the specified ID exists.
    /// </summary>
    /// <param name="edgeId">The edge ID to check.</param>
    public bool ContainsEdge(string edgeId) => Elements.FindById<Edge>(edgeId) != null;

    #endregion
}
