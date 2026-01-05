using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Avalonia.Input;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// Input state used while animations are running.
/// While active, user input is suppressed to prevent conflicts with animated transitions.
/// </summary>
public sealed class AnimatingState : IInputState
{
    public static AnimatingState Instance { get; } = new();

    public string Name => "Animating";

    public bool IsModal => true;

    private AnimatingState()
    {
    }

    public void Enter(InputStateContext context)
    {
    }

    public void Exit(InputStateContext context)
    {
    }

    public StateTransitionResult HandlePointerPressed(InputStateContext context, PointerPressedEventArgs e, Control? source)
        => StateTransitionResult.Stay(handled: true);

    public StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
        => StateTransitionResult.Stay(handled: true);

    public StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
        => StateTransitionResult.Stay(handled: true);

    public StateTransitionResult HandlePointerWheel(InputStateContext context, PointerWheelEventArgs e)
        => StateTransitionResult.Stay(handled: true);

    public StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
        => StateTransitionResult.Stay(handled: true);

    public void Update(InputStateContext context, double deltaTime)
    {
    }
}
