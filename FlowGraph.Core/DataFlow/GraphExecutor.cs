using System.Collections.Specialized;

namespace FlowGraph.Core.DataFlow;

/// <summary>
/// Executes a graph by propagating data through connected nodes.
/// Uses topological sorting for correct execution order.
/// Optimized for incremental updates - only re-executes affected nodes.
/// </summary>
public sealed class GraphExecutor : IDisposable
{
    private readonly Graph _graph;
    private readonly Dictionary<string, INodeProcessor> _processors = new();
    private readonly Dictionary<string, HashSet<string>> _dependencyGraph = new(); // nodeId -> dependent nodeIds
    private readonly Dictionary<string, int> _topologicalOrder = new();
    private readonly HashSet<string> _dirtyNodes = new();
    private readonly object _lock = new();
    private bool _isBatchUpdate;
    private bool _isExecuting;
    private bool _isDisposed;

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

        lock (_lock)
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

        UpdateTopologicalOrder();
    }

    /// <summary>
    /// Unregisters a processor.
    /// </summary>
    /// <param name="nodeId">The node ID to unregister.</param>
    /// <returns>True if the processor was found and removed.</returns>
    public bool UnregisterProcessor(string nodeId)
    {
        lock (_lock)
        {
            if (_processors.TryGetValue(nodeId, out var processor))
            {
                foreach (var output in processor.OutputValues.Values)
                {
                    output.ValueChanged -= OnOutputValueChanged;
                }
                _processors.Remove(nodeId);
                UpdateTopologicalOrder();
                return true;
            }
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
        lock (_lock)
        {
            return _processors.GetValueOrDefault(nodeId);
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
        lock (_lock)
        {
            _isBatchUpdate = true;
        }
    }

    /// <summary>
    /// Ends batch update and executes all dirty nodes.
    /// </summary>
    public void EndBatchUpdate()
    {
        lock (_lock)
        {
            _isBatchUpdate = false;
        }

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
            lock (_lock)
            {
                sortedNodes = _processors.Values
                    .OrderBy(p => _topologicalOrder.GetValueOrDefault(p.Node.Id, int.MaxValue))
                    .ToList();
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

            List<string> sortedAffected;
            lock (_lock)
            {
                sortedAffected = affectedNodes
                    .Where(id => _processors.ContainsKey(id))
                    .OrderBy(id => _topologicalOrder.GetValueOrDefault(id, int.MaxValue))
                    .ToList();
            }

            foreach (var id in sortedAffected)
            {
                if (_processors.TryGetValue(id, out var processor))
                {
                    processor.Process();
                    NodeProcessed?.Invoke(this, new NodeProcessedEventArgs(id));
                }
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
        if (!_processors.TryGetValue(sourceNodeId, out var sourceProcessor)) return;
        if (!sourceProcessor.OutputValues.TryGetValue(sourcePortId, out var sourcePort)) return;

        PropagateValue(sourceNodeId, sourcePortId, sourcePort.Value);
    }

    private void OnOutputValueChanged(object? sender, PortValueChangedEventArgs e)
    {
        if (sender is not IPortValue portValue) return;
        if (_isExecuting) return; // Don't propagate during execution (handled by execution order)

        // Find the node that owns this output
        INodeProcessor? sourceProcessor = null;
        lock (_lock)
        {
            sourceProcessor = _processors.Values
                .FirstOrDefault(p => p.OutputValues.Values.Contains(portValue));
        }

        if (sourceProcessor == null) return;

        if (_isBatchUpdate)
        {
            // Mark dependent nodes as dirty
            lock (_lock)
            {
                var affected = GetAffectedNodes(sourceProcessor.Node.Id);
                foreach (var nodeId in affected)
                {
                    _dirtyNodes.Add(nodeId);
                }
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

    private void ExecuteDirtyNodes()
    {
        if (_isExecuting) return;

        HashSet<string> nodesToExecute;
        lock (_lock)
        {
            if (_dirtyNodes.Count == 0) return;
            nodesToExecute = new HashSet<string>(_dirtyNodes);
            _dirtyNodes.Clear();
        }

        try
        {
            _isExecuting = true;
            ExecutionStarted?.Invoke(this, EventArgs.Empty);

            var sorted = nodesToExecute
                .Where(id => _processors.ContainsKey(id))
                .OrderBy(id => _topologicalOrder.GetValueOrDefault(id, int.MaxValue))
                .ToList();

            foreach (var id in sorted)
            {
                if (_processors.TryGetValue(id, out var processor))
                {
                    processor.Process();
                    NodeProcessed?.Invoke(this, new NodeProcessedEventArgs(id));
                }
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

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!affected.Add(current)) continue;

            lock (_lock)
            {
                if (_dependencyGraph.TryGetValue(current, out var dependents))
                {
                    foreach (var dependent in dependents)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
        }

        return affected;
    }

    private void RebuildDependencyGraph()
    {
        lock (_lock)
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
        }

        UpdateTopologicalOrder();
    }

    private void UpdateTopologicalOrder()
    {
        lock (_lock)
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

        lock (_lock)
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
