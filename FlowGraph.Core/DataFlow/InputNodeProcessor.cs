namespace FlowGraph.Core.DataFlow;

/// <summary>
/// A simple processor for nodes that only output values (no processing needed).
/// Use this for input nodes like color pickers, sliders, text inputs, etc.
/// </summary>
/// <typeparam name="T">The type of value this node outputs.</typeparam>
public class InputNodeProcessor<T> : NodeProcessor
{
    private readonly ReactivePort<T> _output;

    /// <summary>
    /// Creates a new input node processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="outputPortId">The output port ID.</param>
    /// <param name="initialValue">The initial value.</param>
    public InputNodeProcessor(Node node, string outputPortId = "out", T? initialValue = default)
        : base(node)
    {
        _output = RegisterOutput(outputPortId, initialValue);
    }

    /// <summary>
    /// Gets or sets the output value.
    /// </summary>
    public T? Value
    {
        get => _output.TypedValue;
        set => _output.TypedValue = value;
    }

    /// <summary>
    /// Gets the output port.
    /// </summary>
    public ReactivePort<T> OutputPort => _output;

    /// <inheritdoc />
    public override bool AutoExecute => false;

    /// <inheritdoc />
    public override void Process()
    {
        // Input nodes don't process - they just provide values
    }
}
