using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for shape resize handles.
/// </summary>
/// <remarks>
/// <para>
/// Shape resize handles appear when a shape element (e.g., sticky note, comment)
/// is selected. Clicking and dragging a handle resizes the shape.
/// </para>
/// <para>
/// Priority is set high (95) to ensure resize handles are detected before
/// the shape body itself.
/// </para>
/// </remarks>
public class ShapeResizeHandleProcessor : InputProcessorBase
{
  public override HitTargetType HandledTypes => HitTargetType.ShapeResizeHandle;

  public override int Priority => InputProcessorPriority.ResizeHandle - 5; // 95, just below node resize

  public override string Name => "ShapeResizeHandleProcessor";

  public override InputProcessorResult HandlePointerPressed(
      InputStateContext context,
      GraphHitTestResult hit,
      PointerPressedEventArgs e)
  {
    var point = e.GetCurrentPoint(context.RootPanel);
    if (!point.Properties.IsLeftButtonPressed)
      return InputProcessorResult.NotHandled;

    // Block in read-only mode
    if (context.Settings.IsReadOnly)
      return InputProcessorResult.NotHandled;

    var shape = hit.ShapeResizeHandleOwner;
    var handlePosition = hit.ShapeResizeHandle;

    if (shape == null || !handlePosition.HasValue)
      return InputProcessorResult.NotHandled;

    // Convert Core.Input.ResizeHandlePosition to Rendering.ResizeHandlePosition
    var renderHandlePosition = ConvertHandlePosition(handlePosition.Value);

    // Get viewport position for the resize state
    var canvasPos = new AvaloniaPoint(hit.CanvasPosition.X, hit.CanvasPosition.Y);
    var viewportPos = context.CanvasToViewport(canvasPos);

    // Create and transition to resizing state
    var resizeState = new ResizingShapeState(
        shape,
        renderHandlePosition,
        viewportPos,
        context.Settings,
        context.Viewport);

    CapturePointer(e, context.RootPanel);
    return InputProcessorResult.TransitionTo(resizeState);
  }

  /// <summary>
  /// Converts from Core.Input.ResizeHandlePosition to Rendering.ResizeHandlePosition.
  /// </summary>
  private static Rendering.ResizeHandlePosition ConvertHandlePosition(ResizeHandlePosition corePos)
  {
    return corePos switch
    {
      ResizeHandlePosition.TopLeft => Rendering.ResizeHandlePosition.TopLeft,
      ResizeHandlePosition.TopCenter => Rendering.ResizeHandlePosition.Top,
      ResizeHandlePosition.TopRight => Rendering.ResizeHandlePosition.TopRight,
      ResizeHandlePosition.MiddleLeft => Rendering.ResizeHandlePosition.Left,
      ResizeHandlePosition.MiddleRight => Rendering.ResizeHandlePosition.Right,
      ResizeHandlePosition.BottomLeft => Rendering.ResizeHandlePosition.BottomLeft,
      ResizeHandlePosition.BottomCenter => Rendering.ResizeHandlePosition.Bottom,
      ResizeHandlePosition.BottomRight => Rendering.ResizeHandlePosition.BottomRight,
      _ => Rendering.ResizeHandlePosition.BottomRight
    };
  }
}
