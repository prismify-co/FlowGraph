namespace FlowGraph.Core.DataFlow.Processors;

/// <summary>
/// A processor that passes through a single value unchanged.
/// Useful for debugging or as a base for more complex processors.
/// </summary>
/// <typeparam name="T">The type of value to pass through.</typeparam>
public class PassThroughProcessor<T> : NodeProcessor
{
    private readonly ReactivePort<T> _input;
    private readonly ReactivePort<T> _output;

    /// <summary>
    /// Creates a new pass-through processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="inputPortId">The input port ID.</param>
    /// <param name="outputPortId">The output port ID.</param>
    public PassThroughProcessor(Node node, string inputPortId = "in", string outputPortId = "out")
        : base(node)
    {
        _input = RegisterInput<T>(inputPortId);
        _output = RegisterOutput<T>(outputPortId);
    }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public T? Value => _input.TypedValue;

    /// <inheritdoc />
    public override void Process()
    {
        _output.TypedValue = _input.TypedValue;
    }
}
