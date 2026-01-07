using Avalonia.Controls;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;
using FlowGraph.Avalonia.Rendering.NodeRenderers;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Data flow integration for reactive node processing.
/// </summary>
public partial class FlowCanvas
{
    private GraphExecutor? _graphExecutor;
    private readonly Dictionary<string, INodeProcessor> _nodeProcessors = new();

    #region Public Properties - Data Flow

    /// <summary>
    /// Gets the graph executor for data flow processing.
    /// Returns null if data flow has not been enabled.
    /// </summary>
    public GraphExecutor? DataFlow => _graphExecutor;

    /// <summary>
    /// Gets whether data flow is enabled for this canvas.
    /// </summary>
    public bool IsDataFlowEnabled => _graphExecutor != null;

    #endregion

    #region Public Methods - Data Flow

    /// <summary>
    /// Enables data flow processing for this canvas.
    /// Creates a GraphExecutor that will handle value propagation between connected nodes.
    /// </summary>
    /// <returns>The created GraphExecutor.</returns>
    public GraphExecutor EnableDataFlow()
    {
        if (_graphExecutor != null)
            return _graphExecutor;

        if (Graph == null)
            throw new InvalidOperationException("Cannot enable data flow without a Graph.");

        _graphExecutor = new GraphExecutor(Graph);
        
        // Register any processors that were added before data flow was enabled
        foreach (var (nodeId, processor) in _nodeProcessors)
        {
            _graphExecutor.RegisterProcessor(processor);
        }

        return _graphExecutor;
    }

    /// <summary>
    /// Disables data flow processing and disposes the executor.
    /// </summary>
    public void DisableDataFlow()
    {
        _graphExecutor?.Dispose();
        _graphExecutor = null;
    }

    /// <summary>
    /// Registers a processor for a node.
    /// If data flow is enabled, also registers with the executor.
    /// </summary>
    /// <param name="processor">The processor to register.</param>
    public void RegisterProcessor(INodeProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        _nodeProcessors[processor.Node.Id] = processor;
        _graphExecutor?.RegisterProcessor(processor);

        // If the renderer supports data binding, update the visual
        UpdateNodeVisualForProcessor(processor);
    }

    /// <summary>
    /// Unregisters a processor for a node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>True if a processor was found and removed.</returns>
    public bool UnregisterProcessor(string nodeId)
    {
        var removed = _nodeProcessors.Remove(nodeId);
        _graphExecutor?.UnregisterProcessor(nodeId);
        return removed;
    }

    /// <summary>
    /// Gets a processor by node ID.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>The processor, or null if not found.</returns>
    public INodeProcessor? GetProcessor(string nodeId)
    {
        return _nodeProcessors.GetValueOrDefault(nodeId);
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
    /// Creates and registers an input processor for a node.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="node">The node.</param>
    /// <param name="outputPortId">The output port ID.</param>
    /// <param name="initialValue">The initial value.</param>
    /// <returns>The created processor.</returns>
    public InputNodeProcessor<T> CreateInputProcessor<T>(Node node, string outputPortId = "out", T? initialValue = default)
    {
        var processor = new InputNodeProcessor<T>(node, outputPortId, initialValue);
        RegisterProcessor(processor);
        return processor;
    }

    /// <summary>
    /// Creates and registers an output processor for a node.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="node">The node.</param>
    /// <param name="inputPortId">The input port ID.</param>
    /// <returns>The created processor.</returns>
    public OutputNodeProcessor<T> CreateOutputProcessor<T>(Node node, string inputPortId = "in")
    {
        var processor = new OutputNodeProcessor<T>(node, inputPortId);
        RegisterProcessor(processor);
        return processor;
    }

    /// <summary>
    /// Executes the entire graph in topological order.
    /// </summary>
    public void ExecuteGraph()
    {
        _graphExecutor?.ExecuteAll();
    }

    /// <summary>
    /// Executes only nodes affected by a change to the specified node.
    /// </summary>
    /// <param name="nodeId">The node that changed.</param>
    public void ExecuteFromNode(string nodeId)
    {
        _graphExecutor?.ExecuteFrom(nodeId);
    }

    /// <summary>
    /// Begins a batch update for data flow.
    /// Changes won't propagate until EndDataFlowBatch is called.
    /// </summary>
    public void BeginDataFlowBatch()
    {
        _graphExecutor?.BeginBatchUpdate();
    }

    /// <summary>
    /// Ends a batch update and executes all affected nodes.
    /// </summary>
    public void EndDataFlowBatch()
    {
        _graphExecutor?.EndBatchUpdate();
    }

    #endregion

    #region Private Methods - Data Flow

    private void UpdateNodeVisualForProcessor(INodeProcessor processor)
    {
        if (_mainCanvas == null || _theme == null) return;

        var node = processor.Node;
        var renderer = NodeRenderers.GetRenderer(node.Type);

        // Check if the renderer supports data binding
        if (renderer is IDataNodeRenderer dataRenderer)
        {
            var context = new NodeRenderContext
            {
                Theme = _theme,
                Settings = Settings,
                Scale = _viewport.Zoom
            };

            // Get the existing visual
            var existingVisual = _graphRenderer.GetNodeVisual(node.Id);
            if (existingVisual != null)
            {
                // Attach the processor to the renderer
                dataRenderer.OnProcessorAttached(existingVisual, processor);
            }
        }
    }

    /// <summary>
    /// Creates a data-bound visual for a node if its renderer supports it.
    /// Called during node rendering when a processor is registered.
    /// </summary>
    internal Control? CreateDataBoundNodeVisual(Node node, NodeRenderContext context)
    {
        var renderer = NodeRenderers.GetRenderer(node.Type);

        if (renderer is IDataNodeRenderer dataRenderer && _nodeProcessors.TryGetValue(node.Id, out var processor))
        {
            var visual = dataRenderer.CreateDataBoundVisual(node, processor, context);
            dataRenderer.OnProcessorAttached(visual, processor);
            return visual;
        }

        return null;
    }

    #endregion
}
