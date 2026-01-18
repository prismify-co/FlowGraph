using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input on empty canvas space (panning, box selection).
/// </summary>
/// <remarks>
/// <para>
/// This processor has the LOWEST priority and serves as the fallback
/// when no element is hit. It handles:
/// <list type="bullet">
/// <item>Left click: deselect all, optionally start box selection</item>
/// <item>Middle click: start panning</item>
/// <item>Right click: context menu (handled elsewhere)</item>
/// </list>
/// </para>
/// </remarks>
public class CanvasProcessor : InputProcessorBase
{
  public override HitTargetType HandledTypes => HitTargetType.Canvas | HitTargetType.None;

  public override int Priority => InputProcessorPriority.Canvas;

  public override string Name => "CanvasProcessor";

  public override InputProcessorResult HandlePointerPressed(
      InputStateContext context,
      GraphHitTestResult hit,
      PointerPressedEventArgs e)
  {
    var point = e.GetCurrentPoint(context.RootPanel);
    var canvasPos = new AvaloniaPoint(hit.CanvasPosition.X, hit.CanvasPosition.Y);
    var viewportPos = context.CanvasToViewport(canvasPos);

    // Middle mouse button: always start panning (allowed in read-only mode)
    if (point.Properties.IsMiddleButtonPressed)
    {
      var panState = new PanningState(viewportPos, context.Viewport);
      CapturePointer(e, context.RootPanel);
      return InputProcessorResult.TransitionTo(panState);
    }

    // Left click on canvas
    if (point.Properties.IsLeftButtonPressed)
    {
      return HandleLeftClick(context, e, viewportPos, canvasPos);
    }

    return InputProcessorResult.NotHandled;
  }

  private static InputProcessorResult HandleLeftClick(
      InputStateContext context,
      PointerPressedEventArgs e,
      AvaloniaPoint viewportPos,
      AvaloniaPoint canvasPos)
  {
    bool ctrlHeld = IsCtrlHeld(e);

    // If not Ctrl-clicking, deselect all via the context event
    // This lets FlowCanvas handle the actual deselection through SelectionManager
    if (!ctrlHeld)
    {
      context.RaiseDeselectAll();
    }

    // Start box selection if PanOnDrag is disabled
    // Otherwise, this is just a deselect click followed by pan
    if (!context.Settings.PanOnDrag)
    {
      var boxState = new BoxSelectingState(canvasPos, context.Settings, context.Viewport);
      CapturePointer(e, context.RootPanel);
      return InputProcessorResult.TransitionTo(boxState);
    }

    // Left-click-drag pans mode: check for shift for box selection
    if (IsShiftHeld(e))
    {
      var boxState = new BoxSelectingState(canvasPos, context.Settings, context.Viewport);
      CapturePointer(e, context.RootPanel);
      return InputProcessorResult.TransitionTo(boxState);
    }

    // Start panning
    var panState = new PanningState(viewportPos, context.Viewport);
    CapturePointer(e, context.RootPanel);
    return InputProcessorResult.TransitionTo(panState);
  }
}
