using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Represents the result of a state transition.
/// </summary>
public record StateTransitionResult(IInputState? NewState, bool Handled)
{
    public static StateTransitionResult Stay(bool handled = true) => new(null, handled);
    public static StateTransitionResult TransitionTo(IInputState newState) => new(newState, true);
    public static StateTransitionResult Unhandled() => new(null, false);
}

/// <summary>
/// Base interface for all canvas input states.
/// Implements the State Pattern for clean input handling.
/// </summary>
public interface IInputState
{
    /// <summary>
    /// Gets the name of this state for debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called when entering this state.
    /// </summary>
    void Enter(InputStateContext context);

    /// <summary>
    /// Called when exiting this state.
    /// </summary>
    void Exit(InputStateContext context);

    /// <summary>
    /// Handles pointer pressed events.
    /// </summary>
    StateTransitionResult HandlePointerPressed(InputStateContext context, PointerPressedEventArgs e, Control? source);

    /// <summary>
    /// Handles pointer moved events.
    /// </summary>
    StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e);

    /// <summary>
    /// Handles pointer released events.
    /// </summary>
    StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e);

    /// <summary>
    /// Handles pointer wheel events.
    /// </summary>
    StateTransitionResult HandlePointerWheel(InputStateContext context, PointerWheelEventArgs e);

    /// <summary>
    /// Handles key down events.
    /// </summary>
    StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e);

    /// <summary>
    /// Called each frame for states that need continuous updates (e.g., animations).
    /// </summary>
    void Update(InputStateContext context, double deltaTime);

    /// <summary>
    /// Gets whether this state blocks other interactions.
    /// </summary>
    bool IsModal { get; }
}
