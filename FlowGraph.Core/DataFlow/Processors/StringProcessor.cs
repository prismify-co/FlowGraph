namespace FlowGraph.Core.DataFlow.Processors;

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
