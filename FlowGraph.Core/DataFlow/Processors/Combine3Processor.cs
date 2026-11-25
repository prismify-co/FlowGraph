namespace FlowGraph.Core.DataFlow.Processors;

/// <summary>
/// A processor that combines three inputs into one output.
/// </summary>
/// <typeparam name="TIn1">The first input type.</typeparam>
/// <typeparam name="TIn2">The second input type.</typeparam>
/// <typeparam name="TIn3">The third input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class Combine3Processor<TIn1, TIn2, TIn3, TOut> : NodeProcessor
{
    private readonly ReactivePort<TIn1> _input1;
    private readonly ReactivePort<TIn2> _input2;
    private readonly ReactivePort<TIn3> _input3;
    private readonly ReactivePort<TOut> _output;
    private readonly Func<TIn1?, TIn2?, TIn3?, TOut?> _combine;

    /// <summary>
    /// Creates a new combine processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="combine">The combination function.</param>
    /// <param name="input1PortId">The first input port ID.</param>
    /// <param name="input2PortId">The second input port ID.</param>
    /// <param name="input3PortId">The third input port ID.</param>
    /// <param name="outputPortId">The output port ID.</param>
    public Combine3Processor(
        Node node,
        Func<TIn1?, TIn2?, TIn3?, TOut?> combine,
        string input1PortId = "in1",
        string input2PortId = "in2",
        string input3PortId = "in3",
        string outputPortId = "out")
        : base(node)
    {
        _combine = combine ?? throw new ArgumentNullException(nameof(combine));
        _input1 = RegisterInput<TIn1>(input1PortId);
        _input2 = RegisterInput<TIn2>(input2PortId);
        _input3 = RegisterInput<TIn3>(input3PortId);
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
    /// Gets the third input value.
    /// </summary>
    public TIn3? Input3Value => _input3.TypedValue;

    /// <summary>
    /// Gets the output value.
    /// </summary>
    public TOut? OutputValue => _output.TypedValue;

    /// <inheritdoc />
    public override void Process()
    {
        _output.TypedValue = _combine(_input1.TypedValue, _input2.TypedValue, _input3.TypedValue);
    }
}
