namespace FlowGraph.Core.DataFlow.Processors;

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
