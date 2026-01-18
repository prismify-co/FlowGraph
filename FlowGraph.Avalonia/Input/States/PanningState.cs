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
        // Panning works in viewport coordinates (screen-space delta)
        var currentPoint = GetTypedViewportPosition(context, e);
        var deltaX = currentPoint.X - _startPoint.X;
        var deltaY = currentPoint.Y - _startPoint.Y;

        // Use SetOffsetDirect to avoid firing events during pan (reduces overhead)
        // We'll manually apply the transform below
        context.Viewport.SetOffsetDirect(_startOffsetX + deltaX, _startOffsetY + deltaY);

        // Apply transform to canvas - this is O(1) update
        context.ApplyViewportTransform();

        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        // Fire the viewport changed event now that panning is complete
        // This notifies any subscribers (like viewport bounds caching) of the final position
        context.Viewport.NotifyViewportChanged();

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
