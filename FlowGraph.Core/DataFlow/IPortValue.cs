namespace FlowGraph.Core.DataFlow;

/// <summary>
/// Non-generic base interface for port values.
/// Enables working with port values without knowing their concrete type.
/// </summary>
public interface IPortValue
{
    /// <summary>
    /// Gets the port ID this value belongs to.
    /// </summary>
    string PortId { get; }

    /// <summary>
    /// Gets the value as an object.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets the CLR type of the value.
    /// </summary>
    Type ValueType { get; }

    /// <summary>
    /// Sets the value from an untyped object.
    /// </summary>
    /// <param name="value">The value to set. Must be compatible with ValueType.</param>
    void SetValue(object? value);

    /// <summary>
    /// Event raised when the value changes (non-generic).
    /// </summary>
    event EventHandler<PortValueChangedEventArgs>? ValueChanged;
}

/// <summary>
/// Represents a reactive port value that notifies when changed.
/// </summary>
/// <typeparam name="T">The type of data this port carries.</typeparam>
public interface IPortValue<T> : IPortValue
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    T? TypedValue { get; set; }

    /// <summary>
    /// Event raised when the value changes with typed arguments.
    /// </summary>
    event EventHandler<PortValueChangedEventArgs<T>>? TypedValueChanged;
}

/// <summary>
/// Event args for port value changes.
/// </summary>
public class PortValueChangedEventArgs : EventArgs
{
    /// <summary>
    /// The port ID that changed.
    /// </summary>
    public required string PortId { get; init; }

    /// <summary>
    /// The previous value.
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// The new value.
    /// </summary>
    public object? NewValue { get; init; }
}

/// <summary>
/// Typed event args for port value changes.
/// Note: This class sets both the base class properties AND provides typed accessors.
/// </summary>
/// <typeparam name="T">The type of the port value.</typeparam>
public class PortValueChangedEventArgs<T> : PortValueChangedEventArgs
{
    /// <summary>
    /// The previous value (typed).
    /// </summary>
    public T? TypedOldValue { get; init; }

    /// <summary>
    /// The new value (typed).
    /// </summary>
    public T? TypedNewValue { get; init; }
}
