using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Core;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Elements.Shapes;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for shape elements (sticky notes, annotations, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Shapes are non-node visual elements that support:
/// <list type="bullet">
/// <item>Selection (like nodes)</item>
/// <item>Dragging (like nodes)</item>
/// <item>Resizing (corner handles)</item>
/// <item>Text editing (for sticky notes)</item>
/// </list>
/// </para>
/// <para>
/// <b>Shape Types:</b>
/// <list type="bullet">
/// <item>StickyNote: Text-editable, draggable, resizable</item>
/// <item>Future: Annotations, swimlanes, etc.</item>
/// </list>
/// </para>
/// </remarks>
public class ShapeProcessor : InputProcessorBase
{
  public override HitTargetType HandledTypes => HitTargetType.Shape;

  public override int Priority => InputProcessorPriority.Shape;

  public override string Name => "ShapeProcessor";

  public override InputProcessorResult HandlePointerPressed(
      InputStateContext context,
      GraphHitTestResult hit,
      PointerPressedEventArgs e)
  {
    var point = e.GetCurrentPoint(context.RootPanel);
    if (!point.Properties.IsLeftButtonPressed)
      return InputProcessorResult.NotHandled;

    // Get shape from hit result target
    var shape = hit.Target as ShapeElement;
    if (shape == null)
      return InputProcessorResult.NotHandled;

    var graph = context.Graph;
    if (graph == null)
      return InputProcessorResult.NotHandled;

    var isReadOnly = context.Settings.IsReadOnly;
    var canvasPos = new AvaloniaPoint(hit.CanvasPosition.X, hit.CanvasPosition.Y);

    // Double-click handling
    if (IsDoubleClick(e))
    {
      return HandleDoubleClick(context, shape, canvasPos, isReadOnly);
    }

    // Handle selection
    HandleShapeSelection(context, graph, shape, IsCtrlHeld(e));

    // Start drag if allowed
    if (!isReadOnly && shape.IsSelected)
    {
      var viewportPos = context.CanvasToViewport(canvasPos);
      var dragState = new DraggingShapesState(
          graph,
          viewportPos,
          canvasPos,
          context.Viewport,
          context.Settings);

      CapturePointer(e, context.RootPanel);
      return InputProcessorResult.TransitionTo(dragState);
    }

    return InputProcessorResult.HandledStay;
  }

  #region Private Helpers

  private static InputProcessorResult HandleDoubleClick(
      InputStateContext context,
      ShapeElement shape,
      AvaloniaPoint canvasPos,
      bool isReadOnly)
  {
    // For text-editable shapes (TextElement, CommentElement, etc.), start text editing
    if (!isReadOnly && (shape is TextElement || shape is CommentElement))
    {
      var screenPos = context.CanvasToViewport(canvasPos);
      context.RaiseShapeTextEditRequested(shape, screenPos);
    }

    return InputProcessorResult.HandledStay;
  }

  private static void HandleShapeSelection(
      InputStateContext context,
      Graph graph,
      ShapeElement shape,
      bool ctrlHeld)
  {
    if (!shape.IsSelectable) return;

    if (!ctrlHeld && !shape.IsSelected)
    {
      // Single select: deselect other shapes and update their visuals
      foreach (var s in graph.Elements.Shapes.Where(s => s.IsSelected && s.Id != shape.Id))
      {
        s.IsSelected = false;
        // Update visual to reflect deselection
        context.ShapeVisualManager?.UpdateSelection(s.Id, false);
      }
      // Also deselect nodes for exclusive shape selection
      foreach (var n in graph.Nodes.Where(n => n.IsSelected))
      {
        n.IsSelected = false;
      }
      // Also deselect edges
      foreach (var e in graph.Elements.Edges.Where(e => e.IsSelected))
      {
        e.IsSelected = false;
      }
      shape.IsSelected = true;
      // Update visual to reflect selection
      context.ShapeVisualManager?.UpdateSelection(shape.Id, true);

      // Notify selection manager
      context.RaiseSelectionChanged();
    }
    else if (ctrlHeld)
    {
      // Toggle selection
      shape.IsSelected = !shape.IsSelected;
      context.ShapeVisualManager?.UpdateSelection(shape.Id, shape.IsSelected);
      context.RaiseSelectionChanged();
    }
  }

  #endregion
}
