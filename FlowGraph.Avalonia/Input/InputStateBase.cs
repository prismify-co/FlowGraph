using Avalonia.Controls;
using Avalonia.Input;
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
    /// Gets the current pointer position relative to the root panel.
    /// </summary>
    protected static AvaloniaPoint GetPosition(InputStateContext context, PointerEventArgs e)
        => e.GetPosition(context.RootPanel);

    /// <summary>
    /// Gets the pointer point for button state information.
    /// </summary>
    protected static PointerPoint GetPointerPoint(InputStateContext context, PointerEventArgs e)
        => e.GetCurrentPoint(context.RootPanel);

    /// <summary>
    /// Performs hit testing on the main canvas.
    /// </summary>
    protected static IInputElement? HitTest(InputStateContext context, AvaloniaPoint position)
        => context.MainCanvas?.InputHitTest(position);

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
