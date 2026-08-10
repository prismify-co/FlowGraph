namespace FlowGraph.Core.DataFlow.Processors;

/// <summary>
/// A processor that transforms a value using a function.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class TransformProcessor<TIn, TOut> : NodeProcessor
{
    private readonly ReactivePort<TIn> _input;
    private readonly ReactivePort<TOut> _output;
    private readonly Func<TIn?, TOut?> _transform;

    /// <summary>
    /// Creates a new transform processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="transform">The transformation function.</param>
    /// <param name="inputPortId">The input port ID.</param>
    /// <param name="outputPortId">The output port ID.</param>
    public TransformProcessor(
        Node node,
        Func<TIn?, TOut?> transform,
        string inputPortId = "in",
        string outputPortId = "out")
        : base(node)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        _input = RegisterInput<TIn>(inputPortId);
        _output = RegisterOutput<TOut>(outputPortId);
    }

    /// <summary>
    /// Gets the input value.
    /// </summary>
    public TIn? InputValue => _input.TypedValue;

    /// <summary>
    /// Gets the output value.
    /// </summary>
    public TOut? OutputValue => _output.TypedValue;

    /// <inheritdoc />
    public override void Process()
    {
        _output.TypedValue = _transform(_input.TypedValue);
    }
}
