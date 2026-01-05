using Avalonia.Controls;
using Avalonia.Input;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// Input state active during viewport animations (pan/zoom transitions).
/// Allows cancellation via click or key press, and permits zoom wheel interactions.
/// </summary>
public class AnimatingViewportState : InputStateBase
{
    private readonly Action? _cancelAnimation;
    private readonly Action? _onExit;

    public override string Name => "AnimatingViewport";
    public override bool IsModal => true;

    /// <summary>
    /// Creates a new viewport animating state.
    /// </summary>
    /// <param name="cancelAnimation">Action to cancel the running viewport animation.</param>
    /// <param name="onExit">Optional callback when exiting this state.</param>
    public AnimatingViewportState(Action? cancelAnimation = null, Action? onExit = null)
    {
        _cancelAnimation = cancelAnimation;
        _onExit = onExit;
    }

    public override void Exit(InputStateContext context)
    {
        _onExit?.Invoke();
    }

    public override StateTransitionResult HandlePointerPressed(InputStateContext context, PointerPressedEventArgs e, Control? source)
    {
        // Cancel animation on any click and transition to idle
        _cancelAnimation?.Invoke();
        
        // Don't consume the event - let IdleState process it
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Allow pointer movement (no action needed)
        return StateTransitionResult.Stay(handled: false);
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        // No action needed
        return StateTransitionResult.Stay(handled: false);
    }

    public override StateTransitionResult HandlePointerWheel(InputStateContext context, PointerWheelEventArgs e)
    {
        // Cancel animation and allow zoom
        _cancelAnimation?.Invoke();
        
        var position = e.GetPosition(context.RootPanel);
        if (e.Delta.Y > 0)
            context.Viewport.ZoomIn(position);
        else
            context.Viewport.ZoomOut(position);

        context.RaiseGridRender();
        e.Handled = true;
        
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        // Cancel animation on Escape
        if (e.Key == Key.Escape)
        {
            _cancelAnimation?.Invoke();
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        // Allow other key commands to cancel and process
        _cancelAnimation?.Invoke();
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }
}
