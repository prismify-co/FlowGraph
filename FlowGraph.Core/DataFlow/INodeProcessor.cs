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

/// <summary>
/// Base class for typed node processors with specific input/output types.
/// Provides helper methods for registering ports and managing values.
/// </summary>
public abstract class NodeProcessor : INodeProcessor
{
    private readonly Dictionary<string, IPortValue> _inputs = new();
    private readonly Dictionary<string, IPortValue> _outputs = new();
    private bool _isProcessing;

    /// <summary>
    /// Creates a new node processor.
    /// </summary>
    /// <param name="node">The node this processor is attached to.</param>
    protected NodeProcessor(Node node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <inheritdoc />
    public Node Node { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IPortValue> InputValues => _inputs;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IPortValue> OutputValues => _outputs;

    /// <inheritdoc />
    public virtual bool AutoExecute => true;

    /// <inheritdoc />
    public virtual bool IsValid => true;

    /// <summary>
    /// Registers an input port with a reactive value.
    /// </summary>
    /// <typeparam name="T">The type of the port value.</typeparam>
    /// <param name="portId">The port identifier (must match a port in Node.Inputs).</param>
    /// <param name="initialValue">The initial value.</param>
    /// <returns>The created reactive port.</returns>
    protected ReactivePort<T> RegisterInput<T>(string portId, T? initialValue = default)
    {
        var port = new ReactivePort<T>(portId, initialValue);
        _inputs[portId] = port;

        if (AutoExecute)
        {
            port.ValueChanged += OnInputValueChanged;
        }

        return port;
    }

    /// <summary>
    /// Registers an output port with a reactive value.
    /// </summary>
    /// <typeparam name="T">The type of the port value.</typeparam>
    /// <param name="portId">The port identifier (must match a port in Node.Outputs).</param>
    /// <param name="initialValue">The initial value.</param>
    /// <returns>The created reactive port.</returns>
    protected ReactivePort<T> RegisterOutput<T>(string portId, T? initialValue = default)
    {
        var port = new ReactivePort<T>(portId, initialValue);
        _outputs[portId] = port;
        return port;
    }

    /// <summary>
    /// Gets a typed input value.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="portId">The port identifier.</param>
    /// <returns>The value, or default if not found.</returns>
    protected T? GetInput<T>(string portId)
    {
        return _inputs.TryGetValue(portId, out var port) && port is ReactivePort<T> typed
            ? typed.TypedValue
            : default;
    }

    /// <summary>
    /// Sets a typed output value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="portId">The port identifier.</param>
    /// <param name="value">The value to set.</param>
    protected void SetOutput<T>(string portId, T? value)
    {
        if (_outputs.TryGetValue(portId, out var port) && port is ReactivePort<T> typed)
        {
            typed.TypedValue = value;
        }
    }

    /// <summary>
    /// Gets an input port by ID.
    /// </summary>
    protected ReactivePort<T>? GetInputPort<T>(string portId)
    {
        return _inputs.TryGetValue(portId, out var port) ? port as ReactivePort<T> : null;
    }

    /// <summary>
    /// Gets an output port by ID.
    /// </summary>
    protected ReactivePort<T>? GetOutputPort<T>(string portId)
    {
        return _outputs.TryGetValue(portId, out var port) ? port as ReactivePort<T> : null;
    }

    private void OnInputValueChanged(object? sender, PortValueChangedEventArgs e)
    {
        // Prevent re-entrant processing
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;
            Process();
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <inheritdoc />
    public abstract void Process();
}

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
