// CS0618: Suppress obsolete warnings - GraphExecutor subscribes to CollectionChanged
// events on Graph.Nodes/Edges which require the ObservableCollection properties.
#pragma warning disable CS0618

using System.Collections.Concurrent;
using System.Collections.Specialized;

namespace FlowGraph.Core.DataFlow;

/// <summary>
/// Executes a graph by propagating data through connected nodes.
/// Uses topological sorting for correct execution order.
/// Optimized for incremental updates - only re-executes affected nodes.
/// Thread-safe for concurrent access from multiple threads.
/// </summary>
public sealed class GraphExecutor : IDisposable
{
    private readonly Graph _graph;
    private readonly Dictionary<string, INodeProcessor> _processors = new();
    private readonly Dictionary<string, HashSet<string>> _dependencyGraph = new(); // nodeId -> dependent nodeIds
    private readonly Dictionary<string, int> _topologicalOrder = new();
    private readonly ConcurrentDictionary<string, byte> _dirtyNodes = new(); // Using ConcurrentDictionary as a thread-safe set
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private volatile bool _isBatchUpdate;
    private volatile bool _isExecuting;
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a new graph executor.
    /// </summary>
    /// <param name="graph">The graph to execute.</param>
    public GraphExecutor(Graph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _graph.Edges.CollectionChanged += OnEdgesChanged;
        _graph.Nodes.CollectionChanged += OnNodesChanged;
        RebuildDependencyGraph();
    }

    /// <summary>
    /// Event raised when execution starts.
    /// </summary>
    public event EventHandler? ExecutionStarted;

    /// <summary>
    /// Event raised when execution completes.
    /// </summary>
    public event EventHandler? ExecutionCompleted;

    /// <summary>
    /// Event raised when a node is processed.
    /// </summary>
    public event EventHandler<NodeProcessedEventArgs>? NodeProcessed;

    /// <summary>
    /// Gets all registered processors.
    /// </summary>
    public IReadOnlyDictionary<string, INodeProcessor> Processors => _processors;

    /// <summary>
    /// Registers a processor for a node.
    /// </summary>
    /// <param name="processor">The processor to register.</param>
    public void RegisterProcessor(INodeProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        _lock.EnterWriteLock();
        try
        {
            _processors[processor.Node.Id] = processor;

            // Subscribe to output changes for propagation
            foreach (var output in processor.OutputValues.Values)
            {
                // Remove any existing subscription to avoid duplicates
                output.ValueChanged -= OnOutputValueChanged;
                output.ValueChanged += OnOutputValueChanged;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        UpdateTopologicalOrder();
    }

    /// <summary>
    /// Unregisters a processor.
    /// </summary>
    /// <param name="nodeId">The node ID to unregister.</param>
    /// <returns>True if the processor was found and removed.</returns>
    public bool UnregisterProcessor(string nodeId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_processors.TryGetValue(nodeId, out var processor))
            {
                foreach (var output in processor.OutputValues.Values)
                {
                    output.ValueChanged -= OnOutputValueChanged;
                }
                _processors.Remove(nodeId);
                UpdateTopologicalOrderUnsafe();
                return true;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return false;
    }

    /// <summary>
    /// Gets a processor by node ID.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>The processor, or null if not found.</returns>
    public INodeProcessor? GetProcessor(string nodeId)
    {
        _lock.EnterReadLock();
        try
        {
            return _processors.GetValueOrDefault(nodeId);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets a typed processor by node ID.
    /// </summary>
    /// <typeparam name="T">The processor type.</typeparam>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>The processor, or null if not found or wrong type.</returns>
    public T? GetProcessor<T>(string nodeId) where T : class, INodeProcessor
    {
        return GetProcessor(nodeId) as T;
    }

    /// <summary>
    /// Begins a batch update. Changes won't propagate until EndBatchUpdate is called.
    /// Use this when making multiple changes to prevent intermediate executions.
    /// </summary>
    public void BeginBatchUpdate()
    {
        _isBatchUpdate = true;
    }

    /// <summary>
    /// Ends batch update and executes all dirty nodes.
    /// </summary>
    public void EndBatchUpdate()
    {
        _isBatchUpdate = false;
        ExecuteDirtyNodes();
    }

    /// <summary>
    /// Executes the entire graph in topological order.
    /// </summary>
    public void ExecuteAll()
    {
        if (_isExecuting) return;

        try
        {
            _isExecuting = true;
            ExecutionStarted?.Invoke(this, EventArgs.Empty);

            List<INodeProcessor> sortedNodes;
            _lock.EnterReadLock();
            try
            {
                sortedNodes = _processors.Values
                    .OrderBy(p => _topologicalOrder.GetValueOrDefault(p.Node.Id, int.MaxValue))
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }

            foreach (var processor in sortedNodes)
            {
                processor.Process();
                NodeProcessed?.Invoke(this, new NodeProcessedEventArgs(processor.Node.Id));
            }
        }
        finally
        {
            _isExecuting = false;
            ExecutionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Executes only nodes affected by a change to the specified node.
    /// More efficient than ExecuteAll when only one input changed.
    /// </summary>
    /// <param name="nodeId">The node that changed.</param>
    public void ExecuteFrom(string nodeId)
    {
        if (_isExecuting) return;

        try
        {
            _isExecuting = true;
            ExecutionStarted?.Invoke(this, EventArgs.Empty);

            var affectedNodes = GetAffectedNodes(nodeId);

            List<(string Id, INodeProcessor Processor)> sortedAffected;
            _lock.EnterReadLock();
            try
            {
                sortedAffected = affectedNodes
                    .Where(id => _processors.ContainsKey(id))
                    .Select(id => (Id: id, Processor: _processors[id]))
                    .OrderBy(x => _topologicalOrder.GetValueOrDefault(x.Id, int.MaxValue))
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }

            foreach (var (id, processor) in sortedAffected)
            {
                processor.Process();
                NodeProcessed?.Invoke(this, new NodeProcessedEventArgs(id));
            }
        }
        finally
        {
            _isExecuting = false;
            ExecutionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Propagates the current value from a source port to all connected target ports.
    /// </summary>
    /// <param name="sourceNodeId">The source node ID.</param>
    /// <param name="sourcePortId">The source port ID.</param>
    public void PropagateFromPort(string sourceNodeId, string sourcePortId)
    {
        INodeProcessor? sourceProcessor;
        _lock.EnterReadLock();
        try
        {
            if (!_processors.TryGetValue(sourceNodeId, out sourceProcessor)) return;
        }
        finally
        {
            _lock.ExitReadLock();
        }

        if (!sourceProcessor.OutputValues.TryGetValue(sourcePortId, out var sourcePort)) return;

        PropagateValue(sourceNodeId, sourcePortId, sourcePort.Value);
    }

    private void OnOutputValueChanged(object? sender, PortValueChangedEventArgs e)
    {
        if (sender is not IPortValue portValue) return;
        if (_isExecuting) return; // Don't propagate during execution (handled by execution order)

        // Find the node that owns this output
        INodeProcessor? sourceProcessor = null;
        _lock.EnterReadLock();
        try
        {
            sourceProcessor = _processors.Values
                .FirstOrDefault(p => p.OutputValues.Values.Contains(portValue));
        }
        finally
        {
            _lock.ExitReadLock();
        }

        if (sourceProcessor == null) return;

        if (_isBatchUpdate)
        {
            // Mark dependent nodes as dirty using thread-safe ConcurrentDictionary
            var affected = GetAffectedNodes(sourceProcessor.Node.Id);
            foreach (var nodeId in affected)
            {
                _dirtyNodes.TryAdd(nodeId, 0);
            }
        }
        else
        {
            // Propagate immediately
            PropagateValue(sourceProcessor.Node.Id, e.PortId, e.NewValue);
        }
    }

    private void PropagateValue(string sourceNodeId, string sourcePortId, object? value)
    {
        // Find all edges from this output
        var outgoingEdges = _graph.Edges
            .Where(e => e.Source == sourceNodeId && e.SourcePort == sourcePortId)
            .ToList();

        _lock.EnterReadLock();
        try
        {
            foreach (var edge in outgoingEdges)
            {
                if (_processors.TryGetValue(edge.Target, out var targetProcessor))
                {
                    if (targetProcessor.InputValues.TryGetValue(edge.TargetPort, out var input))
                    {
                        input.SetValue(value);
                    }
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void ExecuteDirtyNodes()
    {
        if (_isExecuting) return;
        if (_dirtyNodes.IsEmpty) return;

        // Atomically extract and clear dirty nodes
        var nodesToExecute = _dirtyNodes.Keys.ToHashSet();
        _dirtyNodes.Clear();

        if (nodesToExecute.Count == 0) return;

        try
        {
            _isExecuting = true;
            ExecutionStarted?.Invoke(this, EventArgs.Empty);

            List<(string Id, INodeProcessor Processor)> sorted;
            _lock.EnterReadLock();
            try
            {
                sorted = nodesToExecute
                    .Where(id => _processors.ContainsKey(id))
                    .Select(id => (Id: id, Processor: _processors[id]))
                    .OrderBy(x => _topologicalOrder.GetValueOrDefault(x.Id, int.MaxValue))
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }

            foreach (var (id, processor) in sorted)
            {
                processor.Process();
                NodeProcessed?.Invoke(this, new NodeProcessedEventArgs(id));
            }
        }
        finally
        {
            _isExecuting = false;
            ExecutionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private HashSet<string> GetAffectedNodes(string startNodeId)
    {
        var affected = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startNodeId);

        _lock.EnterReadLock();
        try
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!affected.Add(current)) continue;

                if (_dependencyGraph.TryGetValue(current, out var dependents))
                {
                    foreach (var dependent in dependents)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return affected;
    }

    private void RebuildDependencyGraph()
    {
        _lock.EnterWriteLock();
        try
        {
            _dependencyGraph.Clear();

            foreach (var edge in _graph.Edges)
            {
                if (!_dependencyGraph.TryGetValue(edge.Source, out var dependents))
                {
                    dependents = new HashSet<string>();
                    _dependencyGraph[edge.Source] = dependents;
                }
                dependents.Add(edge.Target);
            }

            UpdateTopologicalOrderUnsafe();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UpdateTopologicalOrder()
    {
        _lock.EnterWriteLock();
        try
        {
            UpdateTopologicalOrderUnsafe();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Updates topological order without acquiring lock. Caller must hold write lock.
    /// </summary>
    private void UpdateTopologicalOrderUnsafe()
    {
        _topologicalOrder.Clear();

        // Kahn's algorithm for topological sort
        var inDegree = new Dictionary<string, int>();
        var allNodes = new HashSet<string>(_processors.Keys);

        foreach (var nodeId in allNodes)
        {
            inDegree[nodeId] = 0;
        }

        foreach (var edge in _graph.Edges)
        {
            if (inDegree.ContainsKey(edge.Target))
            {
                inDegree[edge.Target]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = 0;

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            _topologicalOrder[node] = order++;

            if (_dependencyGraph.TryGetValue(node, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    if (inDegree.ContainsKey(dependent))
                    {
                        inDegree[dependent]--;
                        if (inDegree[dependent] == 0)
                        {
                            queue.Enqueue(dependent);
                        }
                    }
                }
            }
        }
    }

    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildDependencyGraph();

        // When edges change, propagate values along new connections
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (Edge edge in e.NewItems)
            {
                PropagateFromPort(edge.Source, edge.SourcePort);
            }
        }
    }

    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Unregister processors for removed nodes
        if (e.OldItems != null)
        {
            foreach (Node node in e.OldItems)
            {
                UnregisterProcessor(node.Id);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _graph.Edges.CollectionChanged -= OnEdgesChanged;
        _graph.Nodes.CollectionChanged -= OnNodesChanged;

        _lock.EnterWriteLock();
        try
        {
            foreach (var processor in _processors.Values)
            {
                foreach (var output in processor.OutputValues.Values)
                {
                    output.ValueChanged -= OnOutputValueChanged;
                }
            }
            _processors.Clear();
            _dependencyGraph.Clear();
            _topologicalOrder.Clear();
            _dirtyNodes.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _lock.Dispose();
    }
}

/// <summary>
/// Event args for node processed events.
/// </summary>
public class NodeProcessedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new event args.
    /// </summary>
    /// <param name="nodeId">The processed node ID.</param>
    public NodeProcessedEventArgs(string nodeId)
    {
        NodeId = nodeId;
    }

    /// <summary>
    /// Gets the ID of the node that was processed.
    /// </summary>
    public string NodeId { get; }
}
