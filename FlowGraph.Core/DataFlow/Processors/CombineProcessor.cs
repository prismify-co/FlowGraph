namespace FlowGraph.Core.DataFlow.Processors;

/// <summary>
/// A processor that combines two inputs into one output.
/// </summary>
/// <typeparam name="TIn1">The first input type.</typeparam>
/// <typeparam name="TIn2">The second input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class CombineProcessor<TIn1, TIn2, TOut> : NodeProcessor
{
    private readonly ReactivePort<TIn1> _input1;
    private readonly ReactivePort<TIn2> _input2;
    private readonly ReactivePort<TOut> _output;
    private readonly Func<TIn1?, TIn2?, TOut?> _combine;

    /// <summary>
    /// Creates a new combine processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="combine">The combination function.</param>
    /// <param name="input1PortId">The first input port ID.</param>
    /// <param name="input2PortId">The second input port ID.</param>
    /// <param name="outputPortId">The output port ID.</param>
    public CombineProcessor(
        Node node,
        Func<TIn1?, TIn2?, TOut?> combine,
        string input1PortId = "in1",
        string input2PortId = "in2",
        string outputPortId = "out")
        : base(node)
    {
        _combine = combine ?? throw new ArgumentNullException(nameof(combine));
        _input1 = RegisterInput<TIn1>(input1PortId);
        _input2 = RegisterInput<TIn2>(input2PortId);
        _output = RegisterOutput<TOut>(outputPortId);
    }

    /// <summary>
    /// Gets the first input value.
    /// </summary>
    public TIn1? Input1Value => _input1.TypedValue;

    /// <summary>
    /// Gets the second input value.
    /// </summary>
    public TIn2? Input2Value => _input2.TypedValue;

    /// <summary>
    /// Gets the output value.
    /// </summary>
    public TOut? OutputValue => _output.TypedValue;

    /// <inheritdoc />
    public override void Process()
    {
        _output.TypedValue = _combine(_input1.TypedValue, _input2.TypedValue);
    }
}
