using Avalonia;
using Avalonia.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for panning the canvas viewport.
/// </summary>
public class PanningState : InputStateBase
{
    private readonly AvaloniaPoint _startPoint;
    private readonly double _startOffsetX;
    private readonly double _startOffsetY;

    public override string Name => "Panning";

    public PanningState(AvaloniaPoint startPoint, ViewportState viewport)
    {
        _startPoint = startPoint;
        _startOffsetX = viewport.OffsetX;
        _startOffsetY = viewport.OffsetY;
    }

    public override void Enter(InputStateContext context)
    {
        // Store current offset when entering (we can't do this in constructor as we don't have context)
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        var currentPoint = GetScreenPosition(context, e);
        var deltaX = currentPoint.X - _startPoint.X;
        var deltaY = currentPoint.Y - _startPoint.Y;

        context.Viewport.SetOffset(_startOffsetX + deltaX, _startOffsetY + deltaY);
        
        // Apply transform to canvas instead of full re-render
        context.ApplyViewportTransform();
        
        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        ReleasePointer(e);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }
}
