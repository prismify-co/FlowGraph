using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowGraph.Core.Models;

/// <summary>
/// Minimal base class for observable objects using pure .NET INotifyPropertyChanged.
/// This class provides framework-agnostic property change notification without
/// external dependencies like CommunityToolkit.Mvvm.
/// </summary>
public abstract class ObservableBase : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a property value and raises <see cref="PropertyChanged"/> if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property name (auto-populated by compiler).</param>
    /// <returns>True if the value changed; otherwise false.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets a property value, raises <see cref="PropertyChanged"/>, and invokes a callback if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="onChanged">Callback to invoke after the property changes.</param>
    /// <param name="propertyName">The property name (auto-populated by compiler).</param>
    /// <returns>True if the value changed; otherwise false.</returns>
    protected bool SetField<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (!SetField(ref field, value, propertyName))
            return false;

        onChanged();
        return true;
    }

    /// <summary>
    /// Sets a property value, raises <see cref="PropertyChanged"/>, and invokes a callback with old/new values if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="onChanged">Callback to invoke with old and new values after the property changes.</param>
    /// <param name="propertyName">The property name (auto-populated by compiler).</param>
    /// <returns>True if the value changed; otherwise false.</returns>
    protected bool SetField<T>(ref T field, T value, Action<T, T> onChanged, [CallerMemberName] string? propertyName = null)
    {
        var oldValue = field;
        if (!SetField(ref field, value, propertyName))
            return false;

        onChanged(oldValue, value);
        return true;
    }
}
