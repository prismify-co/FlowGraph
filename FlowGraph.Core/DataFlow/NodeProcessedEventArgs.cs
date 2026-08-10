// CS0618: Suppress obsolete warnings - GraphExecutor subscribes to CollectionChanged
// events on Graph.Nodes/Edges which require the ObservableCollection properties.
#pragma warning disable CS0618

namespace FlowGraph.Core.DataFlow;

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
