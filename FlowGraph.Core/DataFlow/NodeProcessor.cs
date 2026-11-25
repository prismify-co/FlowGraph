namespace FlowGraph.Core.DataFlow;

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
