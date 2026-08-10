namespace FlowGraph.Core.DataFlow;

/// <summary>
/// Interface for nodes that can process data.
/// Implement this to create nodes that transform input values to output values.
/// </summary>
public interface INodeProcessor
{
    /// <summary>
    /// Gets the node this processor is attached to.
    /// </summary>
    Node Node { get; }

    /// <summary>
    /// Gets all input port values.
    /// </summary>
    IReadOnlyDictionary<string, IPortValue> InputValues { get; }

    /// <summary>
    /// Gets all output port values.
    /// </summary>
    IReadOnlyDictionary<string, IPortValue> OutputValues { get; }

    /// <summary>
    /// Processes inputs and updates outputs.
    /// Called automatically when any input changes (if auto-execute is enabled).
    /// </summary>
    void Process();

    /// <summary>
    /// Gets whether this processor should auto-execute when inputs change.
    /// </summary>
    bool AutoExecute { get; }

    /// <summary>
    /// Gets whether this processor is currently valid (all required inputs connected).
    /// </summary>
    bool IsValid { get; }
}
