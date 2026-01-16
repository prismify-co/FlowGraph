using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Base class for input states with default implementations.
/// </summary>
public abstract class InputStateBase : IInputState
{
    public abstract string Name { get; }

    public virtual bool IsModal => false;

    public virtual void Enter(InputStateContext context) { }

    public virtual void Exit(InputStateContext context) { }

    public virtual StateTransitionResult HandlePointerPressed(InputStateContext context, PointerPressedEventArgs e, Control? source)
        => StateTransitionResult.Unhandled();

    public virtual StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
        => StateTransitionResult.Unhandled();

    public virtual StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
        => StateTransitionResult.Unhandled();

    public virtual StateTransitionResult HandlePointerWheel(InputStateContext context, PointerWheelEventArgs e)
        => StateTransitionResult.Unhandled();

    public virtual StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
        => StateTransitionResult.Unhandled();

    public virtual void Update(InputStateContext context, double deltaTime) { }

    #region Helper Methods

    /// <summary>
    /// Gets the current pointer position relative to the root panel (screen/viewport coordinates).
    /// Use this for pan calculations, auto-pan edge detection, etc.
    /// </summary>
    protected static AvaloniaPoint GetScreenPosition(InputStateContext context, PointerEventArgs e)
        => e.GetPosition(context.RootPanel);

    /// <summary>
    /// Gets the current pointer position in canvas coordinates.
    /// This uses GetPosition(MainCanvas) which automatically applies the inverse transform,
    /// giving us direct canvas coordinates without needing ScreenToCanvas conversion.
    /// Use this for hit testing, node positions, edge endpoints, etc.
    /// </summary>
    protected static AvaloniaPoint GetCanvasPosition(InputStateContext context, PointerEventArgs e)
        => e.GetPosition(context.MainCanvas);

    /// <summary>
    /// Gets the pointer point for button state information.
    /// </summary>
    protected static PointerPoint GetPointerPoint(InputStateContext context, PointerEventArgs e)
        => e.GetCurrentPoint(context.RootPanel);

    /// <summary>
    /// Performs hit testing on the main canvas. The position must be in canvas coordinates
    /// (use GetCanvasPosition, not GetScreenPosition).
    /// </summary>
    protected static IInputElement? HitTestCanvas(InputStateContext context, AvaloniaPoint canvasPosition)
        => context.MainCanvas?.InputHitTest(canvasPosition);

    /// <summary>
    /// Performs hit testing on the main canvas specifically for finding ports.
    /// Skips edge paths and markers that might be blocking the port.
    /// The position must be in canvas coordinates.
    /// </summary>
    protected static Control? HitTestForPort(InputStateContext context, AvaloniaPoint canvasPosition)
    {
        if (context.MainCanvas == null) return null;

        // Get all elements at this position using GetVisualsAt
        var visualsAtPoint = context.MainCanvas.GetVisualsAt(canvasPosition);

        foreach (var visual in visualsAtPoint)
        {
            if (visual is Control control)
            {
                // Check if this is a port (Ellipse with port Tag)
                if (control.Tag is (Node, Port, bool))
                {
                    return control;
                }
            }
        }

        // Fallback to regular hit test
        var hitElement = context.MainCanvas.InputHitTest(canvasPosition);
        return hitElement as Control;
    }

    /// <summary>
    /// Captures the pointer to the specified element.
    /// </summary>
    protected static void CapturePointer(PointerEventArgs e, IInputElement? element)
        => e.Pointer.Capture(element);

    /// <summary>
    /// Releases pointer capture.
    /// </summary>
    protected static void ReleasePointer(PointerEventArgs e)
        => e.Pointer.Capture(null);

    #endregion
}
