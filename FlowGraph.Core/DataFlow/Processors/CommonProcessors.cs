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

/// <summary>
/// A processor for numeric operations with two inputs.
/// </summary>
public class MathProcessor : NodeProcessor
{
    private readonly ReactivePort<double> _input1;
    private readonly ReactivePort<double> _input2;
    private readonly ReactivePort<double> _output;

    /// <summary>
    /// Creates a new math processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="operation">The math operation to perform.</param>
    public MathProcessor(Node node, MathOperation operation = MathOperation.Add)
        : base(node)
    {
        Operation = operation;
        _input1 = RegisterInput<double>("a");
        _input2 = RegisterInput<double>("b");
        _output = RegisterOutput<double>("result");
    }

    /// <summary>
    /// Gets or sets the math operation.
    /// </summary>
    public MathOperation Operation { get; set; }

    /// <summary>
    /// Gets the result.
    /// </summary>
    public double Result => _output.TypedValue;

    /// <inheritdoc />
    public override void Process()
    {
        var a = _input1.TypedValue;
        var b = _input2.TypedValue;

        _output.TypedValue = Operation switch
        {
            MathOperation.Add => a + b,
            MathOperation.Subtract => a - b,
            MathOperation.Multiply => a * b,
            MathOperation.Divide => b != 0 ? a / b : 0,
            MathOperation.Power => Math.Pow(a, b),
            MathOperation.Modulo => b != 0 ? a % b : 0,
            MathOperation.Min => Math.Min(a, b),
            MathOperation.Max => Math.Max(a, b),
            _ => 0
        };
    }
}

/// <summary>
/// Math operations for MathProcessor.
/// </summary>
public enum MathOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Power,
    Modulo,
    Min,
    Max
}

/// <summary>
/// A processor for string operations.
/// </summary>
public class StringProcessor : NodeProcessor
{
    private readonly ReactivePort<string> _input;
    private readonly ReactivePort<string> _output;

    /// <summary>
    /// Creates a new string processor.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="operation">The string operation to perform.</param>
    public StringProcessor(Node node, StringOperation operation = StringOperation.ToUpper)
        : base(node)
    {
        Operation = operation;
        _input = RegisterInput<string>("in");
        _output = RegisterOutput<string>("out");
    }

    /// <summary>
    /// Gets or sets the string operation.
    /// </summary>
    public StringOperation Operation { get; set; }

    /// <inheritdoc />
    public override void Process()
    {
        var input = _input.TypedValue ?? "";

        _output.TypedValue = Operation switch
        {
            StringOperation.ToUpper => input.ToUpperInvariant(),
            StringOperation.ToLower => input.ToLowerInvariant(),
            StringOperation.Trim => input.Trim(),
            StringOperation.Reverse => new string(input.Reverse().ToArray()),
            StringOperation.Length => input.Length.ToString(),
            _ => input
        };
    }
}

/// <summary>
/// String operations for StringProcessor.
/// </summary>
public enum StringOperation
{
    ToUpper,
    ToLower,
    Trim,
    Reverse,
    Length
}
