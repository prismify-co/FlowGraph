// CS0618: Suppress obsolete warnings - this file intentionally uses Graph.Nodes/Edges
// for internal synchronization with the Elements collection.
#pragma warning disable CS0618

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using FlowGraph.Core.Elements;

namespace FlowGraph.Core;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that can suppress notifications during batch operations.
/// Use <see cref="AddRange"/> to add multiple items with a single notification.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    /// <summary>
    /// Adds a range of items without firing individual notifications.
    /// A single <see cref="NotifyCollectionChangedAction.Reset"/> notification is fired after all items are added.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _suppressNotifications = true;
        try
        {
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotifications = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <inheritdoc/>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications)
        {
            base.OnCollectionChanged(e);
        }
    }
}

/// <summary>
/// Represents a flow graph containing nodes, edges, and other canvas elements.
/// Provides methods for adding, removing, and querying graph elements.
/// </summary>
/// <remarks>
/// <para>
/// The Graph class supports the canvas-first architecture where any
/// <see cref="ICanvasElement"/> can be added to the canvas, not just nodes and edges.
/// </para>
/// <para>
/// For backward compatibility, the <see cref="Nodes"/> and <see cref="Edges"/>
/// collections continue to work as before. New code can use <see cref="Elements"/>
/// to access all elements uniformly.
/// </para>
/// </remarks>
public class Graph
{
    private bool _isBatchLoading;
    private bool _isSyncing; // Prevent infinite recursion during sync

    /// <summary>
    /// Initializes a new instance of the Graph class.
    /// Sets up bidirectional sync between legacy collections (Nodes/Edges) and Elements.
    /// </summary>
    public Graph()
    {
        // Sync legacy Nodes collection → Elements (for backward compat when code uses Nodes.Add directly)
        Nodes.CollectionChanged += (_, e) =>
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                SyncNodesToElements(e);
            }
            finally
            {
                _isSyncing = false;
            }
        };

        // Sync legacy Edges collection → Elements (for backward compat when code uses Edges.Add directly)
        Edges.CollectionChanged += (_, e) =>
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                SyncEdgesToElements(e);
            }
            finally
            {
                _isSyncing = false;
            }
        };
    }

    private void SyncNodesToElements(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (Node node in e.NewItems)
            {
                if (!Elements.Contains(node))
                    Elements.Add(node);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (Node node in e.OldItems)
            {
                Elements.Remove(node);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // For Reset (e.g. after AddRange), sync the full state
            var elementsNodes = Elements.Nodes.ToList();
            foreach (var node in elementsNodes)
            {
                if (!Nodes.Contains(node))
                    Elements.Remove(node);
            }
            foreach (var node in Nodes)
            {
                if (!Elements.Contains(node))
                    Elements.Add(node);
            }
        }
    }

    private void SyncEdgesToElements(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (Edge edge in e.NewItems)
            {
                if (!Elements.Contains(edge))
                    Elements.Add(edge);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (Edge edge in e.OldItems)
            {
                Elements.Remove(edge);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // For Reset (e.g. after AddRange), sync the full state
            var elementsEdges = Elements.Edges.ToList();
            foreach (var edge in elementsEdges)
            {
                if (!Edges.Contains(edge))
                    Elements.Remove(edge);
            }
            foreach (var edge in Edges)
            {
                if (!Elements.Contains(edge))
                    Elements.Add(edge);
            }
        }
    }

    /// <summary>
    /// Gets the collection of all canvas elements (nodes, edges, shapes, etc.).
    /// </summary>
    /// <remarks>
    /// This is the primary collection for the canvas-first architecture.
    /// Use typed accessors like <see cref="Nodes"/> and <see cref="Edges"/> for 
    /// graph-specific operations.
    /// </remarks>
    public ElementCollection Elements { get; } = new();

    /// <summary>
    /// Gets the collection of node elements.
    /// This is a convenience accessor that returns nodes from <see cref="Elements"/>.
    /// </summary>
    [Obsolete("Use Elements.Nodes for new code. This property is retained for backward compatibility.")]
    public BulkObservableCollection<Node> Nodes { get; } = [];

    /// <summary>
    /// Gets the collection of edge elements.
    /// This is a convenience accessor that returns edges from <see cref="Elements"/>.
    /// </summary>
    [Obsolete("Use Elements.Edges for new code. This property is retained for backward compatibility.")]
    public BulkObservableCollection<Edge> Edges { get; } = [];

    /// <summary>
    /// Gets whether the graph is currently in batch loading mode.
    /// During batch loading, collection change notifications are suppressed.
    /// </summary>
    public bool IsBatchLoading => _isBatchLoading;

    /// <summary>
    /// Event raised when batch loading completes.
    /// Subscribe to this to refresh UI after bulk operations.
    /// </summary>
    public event EventHandler? BatchLoadCompleted;

    /// <summary>
    /// Begins batch loading mode. Collection change notifications are suppressed
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

    #region Element Operations (New API)

    /// <summary>
    /// Adds any canvas element to the graph.
    /// </summary>
    /// <param name="element">The element to add.</param>
    public void AddElement(ICanvasElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Elements.Add(element);

        // Sync with legacy collections for backward compat
        if (element is Node node)
            Nodes.Add(node);
        else if (element is Edge edge)
            Edges.Add(edge);
    }

    /// <summary>
    /// Removes any canvas element from the graph.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    public void RemoveElement(ICanvasElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Elements.Remove(element);

        // Sync with legacy collections for backward compat
        if (element is Node node)
            Nodes.Remove(node);
        else if (element is Edge edge)
            Edges.Remove(edge);
    }

    /// <summary>
    /// Adds multiple elements at once with a single notification.
    /// </summary>
    /// <param name="elements">The elements to add.</param>
    public void AddElements(IEnumerable<ICanvasElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var elementList = elements.ToList();
        
        _isSyncing = true;
        try
        {
            Elements.AddRange(elementList);

            // Sync with legacy collections for backward compat
            Nodes.AddRange(elementList.OfType<Node>());
            Edges.AddRange(elementList.OfType<Edge>());
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Removes multiple elements at once with a single notification.
    /// </summary>
    /// <param name="elements">The elements to remove.</param>
    public void RemoveElements(IEnumerable<ICanvasElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var elementList = elements.ToList();
        
        _isSyncing = true;
        try
        {
            Elements.RemoveRange(elementList);

            // Sync with legacy collections for backward compat
            var nodes = elementList.OfType<Node>().ToList();
            var edges = elementList.OfType<Edge>().ToList();

            foreach (var node in nodes)
                Nodes.Remove(node);
            foreach (var edge in edges)
                Edges.Remove(edge);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    #endregion

    #region Node Operations (Legacy API - Backward Compatible)

    /// <summary>
    /// Adds multiple nodes at once with a single notification.
    /// </summary>
    public void AddNodes(IEnumerable<Node> nodes)
    {
        var nodeList = nodes.ToList();
        _isSyncing = true;
        try
        {
            Nodes.AddRange(nodeList);
            Elements.AddRange(nodeList);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    public void AddNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _isSyncing = true;
        try
        {
            Nodes.Add(node);
            Elements.Add(node);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Removes a node and all connected edges from the graph.
    /// </summary>
    public void RemoveNode(string nodeId)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            _isSyncing = true;
            try
            {
                // Remove connected edges first
                var edgesToRemove = Edges.Where(e => e.Source == nodeId || e.Target == nodeId).ToList();
                foreach (var edge in edgesToRemove)
                {
                    Edges.Remove(edge);
                    Elements.Remove(edge);
                }

                Nodes.Remove(node);
                Elements.Remove(node);
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }

    #endregion

    #region Edge Operations (Legacy API - Backward Compatible)

    /// <summary>
    /// Adds multiple edges at once with a single notification.
    /// </summary>
    public void AddEdges(IEnumerable<Edge> edges)
    {
        var edgeList = edges.ToList();
        _isSyncing = true;
        try
        {
            Edges.AddRange(edgeList);
            Elements.AddRange(edgeList);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Adds an edge to the graph.
    /// </summary>
    public void AddEdge(Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        // Skip validation during batch load
        if (!_isBatchLoading)
        {
            var sourceExists = Nodes.Any(n => n.Id == edge.Source);
            var targetExists = Nodes.Any(n => n.Id == edge.Target);

            if (!sourceExists || !targetExists)
            {
                throw new InvalidOperationException("Source and target nodes must exist before adding an edge.");
            }
        }

        _isSyncing = true;
        try
        {
            Edges.Add(edge);
            Elements.Add(edge);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
    public void RemoveEdge(string edgeId)
    {
        var edge = Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge != null)
        {
            _isSyncing = true;
            try
            {
                Edges.Remove(edge);
                Elements.Remove(edge);
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }

    #endregion
}
