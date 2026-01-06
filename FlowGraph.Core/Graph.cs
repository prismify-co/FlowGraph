using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FlowGraph.Core;

/// <summary>
/// An ObservableCollection that can suppress notifications during batch operations.
/// </summary>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    /// <summary>
    /// Adds a range of items without firing individual notifications.
    /// A single Reset notification is fired after all items are added.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
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

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications)
        {
            base.OnCollectionChanged(e);
        }
    }
}

public class Graph
{
    private bool _isBatchLoading;

    public BulkObservableCollection<Node> Nodes { get; } = [];
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
    /// until EndBatchLoad is called. Use this when adding many nodes/edges at once.
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

    /// <summary>
    /// Adds multiple nodes at once with a single notification.
    /// </summary>
    public void AddNodes(IEnumerable<Node> nodes)
    {
        Nodes.AddRange(nodes);
    }

    /// <summary>
    /// Adds multiple edges at once with a single notification.
    /// </summary>
    public void AddEdges(IEnumerable<Edge> edges)
    {
        Edges.AddRange(edges);
    }

    public void AddNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Add(node);
    }

    public void RemoveNode(string nodeId)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            Nodes.Remove(node);
            var edgesToRemove = Edges.Where(e => e.Source == nodeId || e.Target == nodeId).ToList();
            foreach (var edge in edgesToRemove)
            {
                Edges.Remove(edge);
            }
        }
    }

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
        
        Edges.Add(edge);
    }

    public void RemoveEdge(string edgeId)
    {
        var edge = Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge != null)
        {
            Edges.Remove(edge);
        }
    }
}
