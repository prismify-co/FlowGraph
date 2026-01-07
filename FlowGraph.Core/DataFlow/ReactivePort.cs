using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowGraph.Core.DataFlow;

/// <summary>
/// A reactive port that stores a value and notifies when it changes.
/// Thread-safe for UI binding scenarios.
/// </summary>
/// <typeparam name="T">The type of value this port holds.</typeparam>
public sealed class ReactivePort<T> : IPortValue<T>, INotifyPropertyChanged
{
    private T? _value;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new reactive port.
    /// </summary>
    /// <param name="portId">The port identifier.</param>
    /// <param name="initialValue">The initial value.</param>
    public ReactivePort(string portId, T? initialValue = default)
    {
        PortId = portId;
        _value = initialValue;
    }

    /// <inheritdoc />
    public string PortId { get; }

    /// <inheritdoc />
    public Type ValueType => typeof(T);

    /// <summary>
    /// Gets or sets the typed value.
    /// </summary>
    public T? TypedValue
    {
        get
        {
            lock (_lock) return _value;
        }
        set
        {
            T? oldValue;
            lock (_lock)
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;
                oldValue = _value;
                _value = value;
            }

            OnValueChanged(oldValue, value);
            OnPropertyChanged(nameof(TypedValue));
        }
    }

    /// <inheritdoc />
    object? IPortValue.Value => TypedValue;

    /// <inheritdoc />
    public void SetValue(object? value)
    {
        if (value is T typedValue)
        {
            TypedValue = typedValue;
        }
        else if (value == null && !typeof(T).IsValueType)
        {
            TypedValue = default;
        }
        else if (value != null)
        {
            // Try to convert
            try
            {
                TypedValue = (T)Convert.ChangeType(value, typeof(T));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"Cannot convert value of type {value.GetType()} to {typeof(T)}", nameof(value));
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<PortValueChangedEventArgs<T>>? TypedValueChanged;

    /// <inheritdoc />
    public event EventHandler<PortValueChangedEventArgs>? ValueChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnValueChanged(T? oldValue, T? newValue)
    {
        var typedArgs = new PortValueChangedEventArgs<T>
        {
            PortId = PortId,
            // Set BASE class properties (for handlers that receive PortValueChangedEventArgs)
            OldValue = oldValue,
            NewValue = newValue,
            // Set TYPED properties (for handlers that receive PortValueChangedEventArgs<T>)
            TypedOldValue = oldValue,
            TypedNewValue = newValue
        };

        TypedValueChanged?.Invoke(this, typedArgs);
        ValueChanged?.Invoke(this, typedArgs);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Implicitly converts a ReactivePort to its value.
    /// </summary>
    public static implicit operator T?(ReactivePort<T> port) => port.TypedValue;

    /// <inheritdoc />
    public override string ToString() => $"Port[{PortId}] = {TypedValue}";
}
