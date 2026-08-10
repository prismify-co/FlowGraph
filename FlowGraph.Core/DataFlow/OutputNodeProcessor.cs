namespace FlowGraph.Core.DataFlow;

/// <summary>
/// A processor for nodes that receive values and display/use them (no outputs).
/// Use this for output/display nodes.
/// </summary>
/// <typeparam name="T">The type of value this node receives.</typeparam>
public class OutputNodeProcessor<T> : NodeProcessor
{
    private readonly ReactivePort<T> _input;

    /// <summary>
    /// Creates a new output node processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="inputPortId">The input port ID.</param>
    public OutputNodeProcessor(Node node, string inputPortId = "in")
        : base(node)
    {
        _input = RegisterInput<T>(inputPortId);
    }

    /// <summary>
    /// Gets the current input value.
    /// </summary>
    public T? Value => _input.TypedValue;

    /// <summary>
    /// Gets the input port.
    /// </summary>
    public ReactivePort<T> InputPort => _input;

    /// <summary>
    /// Event raised when the value changes.
    /// </summary>
    public event EventHandler<T?>? ValueReceived;

    /// <inheritdoc />
    public override void Process()
    {
        ValueReceived?.Invoke(this, Value);
    }
}
