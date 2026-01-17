using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FlowGraph.Core;
using FlowGraph.Core.Coordinates;
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
    /// Gets the current pointer position relative to the root panel (viewport coordinates).
    /// </summary>
    /// <remarks>
    /// <b>DEPRECATED:</b> Use <see cref="GetTypedViewportPosition"/> instead.
    /// This method returns an untyped <see cref="AvaloniaPoint"/> which doesn't indicate
    /// whether it's in canvas or viewport coordinates, making it easy to accidentally
    /// pass to the wrong conversion method.
    /// </remarks>
    [Obsolete("Use GetTypedViewportPosition() instead. Returns typed ViewportPoint that prevents coordinate confusion.")]
    protected static AvaloniaPoint GetScreenPosition(InputStateContext context, PointerEventArgs e)
        => e.GetPosition(context.RootPanel);

    /// <summary>
    /// Gets the current pointer position in canvas coordinates.
    /// </summary>
    /// <remarks>
    /// <b>DEPRECATED:</b> Use <see cref="GetTypedCanvasPosition"/> instead.
    /// This method only works correctly in Visual Tree mode. The typed method
    /// handles both rendering modes correctly.
    /// </remarks>
    [Obsolete("Use GetTypedCanvasPosition() instead. The typed method works in both Visual Tree and Direct Rendering modes.")]
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

    #region Typed Coordinate Helpers

    /// <summary>
    /// Gets the current pointer position in canvas coordinates.
    /// </summary>
    /// <remarks>
    /// This is the <b>preferred method</b> for getting pointer positions when you need canvas coordinates.
    /// It uses the type-safe coordinate system and works correctly in both Visual Tree and Direct Rendering modes.
    /// 
    /// <para>Use canvas coordinates for:</para>
    /// <list type="bullet">
    /// <item>Hit testing on the canvas</item>
    /// <item>Node/edge positions</item>
    /// <item>Selection box bounds</item>
    /// <item>Any position that needs to be stored or compared with node data</item>
    /// </list>
    /// </remarks>
    protected static CanvasPoint GetTypedCanvasPosition(InputStateContext context, PointerEventArgs e)
        => context.Coordinates.GetPointerCanvasPosition(e);

    /// <summary>
    /// Gets the current pointer position in viewport coordinates.
    /// </summary>
    /// <remarks>
    /// This is the <b>preferred method</b> for getting pointer positions when you need viewport coordinates.
    /// It uses the type-safe coordinate system and works correctly in both Visual Tree and Direct Rendering modes.
    /// 
    /// <para>Use viewport coordinates for:</para>
    /// <list type="bullet">
    /// <item>Pan calculations (delta from last position)</item>
    /// <item>Auto-pan edge detection</item>
    /// <item>UI overlay positioning</item>
    /// <item>Any position relative to the visible screen area</item>
    /// </list>
    /// </remarks>
    protected static ViewportPoint GetTypedViewportPosition(InputStateContext context, PointerEventArgs e)
        => context.Coordinates.GetPointerViewportPosition(e);

    /// <summary>
    /// Converts a canvas point to an Avalonia Point for backward compatibility.
    /// </summary>
    protected static AvaloniaPoint ToAvaloniaPoint(CanvasPoint canvas)
        => new(canvas.X, canvas.Y);

    /// <summary>
    /// Converts a viewport point to an Avalonia Point for backward compatibility.
    /// </summary>
    protected static AvaloniaPoint ToAvaloniaPoint(ViewportPoint viewport)
        => new(viewport.X, viewport.Y);

    /// <summary>
    /// Converts an Avalonia Point to a CanvasPoint.
    /// </summary>
    protected static CanvasPoint ToCanvasPoint(AvaloniaPoint point)
        => new(point.X, point.Y);

    /// <summary>
    /// Converts an Avalonia Point to a ViewportPoint.
    /// </summary>
    protected static ViewportPoint ToViewportPoint(AvaloniaPoint point)
        => new(point.X, point.Y);

    #endregion
}
