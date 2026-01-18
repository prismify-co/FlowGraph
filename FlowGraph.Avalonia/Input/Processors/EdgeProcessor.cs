using Avalonia.Input;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for edges (selection, waypoint manipulation).
/// </summary>
/// <remarks>
/// <para>
/// Edges require special handling because:
/// <list type="bullet">
/// <item>They're hit-tested with tolerance (not rectangular)</item>
/// <item>Waypoints can be dragged to reshape the edge</item>
/// <item>Edge labels can be clicked for editing</item>
/// </list>
/// </para>
/// <para>
/// <b>Interactions:</b>
/// <list type="bullet">
/// <item>Single click: select edge</item>
/// <item>Double click: add waypoint or edit label</item>
/// <item>Ctrl+click: toggle selection (multi-select)</item>
/// </list>
/// </para>
/// </remarks>
public class EdgeProcessor : InputProcessorBase
{
  public override HitTargetType HandledTypes => HitTargetType.Edge;

  public override int Priority => InputProcessorPriority.Edge;

  public override string Name => "EdgeProcessor";

  public override InputProcessorResult HandlePointerPressed(
      InputStateContext context,
      GraphHitTestResult hit,
      PointerPressedEventArgs e)
  {
    var point = e.GetCurrentPoint(context.RootPanel);
    if (!point.Properties.IsLeftButtonPressed)
      return InputProcessorResult.NotHandled;

    var edge = hit.Edge;
    if (edge == null)
      return InputProcessorResult.NotHandled;

    var graph = context.Graph;
    if (graph == null)
      return InputProcessorResult.NotHandled;

    var canvasPos = new AvaloniaPoint(hit.CanvasPosition.X, hit.CanvasPosition.Y);
    var isReadOnly = context.Settings.IsReadOnly;

    // Double-click handling
    if (IsDoubleClick(e))
    {
      return HandleDoubleClick(context, edge, canvasPos, isReadOnly);
    }

    // Handle selection
    var ctrlHeld = IsCtrlHeld(e);
    HandleEdgeSelection(context, graph, edge, ctrlHeld);

    // Raise edge clicked event for external handling
    context.RaiseEdgeClicked(edge, ctrlHeld);

    return InputProcessorResult.HandledStay;
  }

  #region Private Helpers

  private static InputProcessorResult HandleDoubleClick(
      InputStateContext context,
      Edge edge,
      AvaloniaPoint canvasPos,
      bool isReadOnly)
  {
    // Double-click: edit label if enabled
    if (!isReadOnly && context.Settings.EnableEdgeLabelEditing)
    {
      var screenPos = context.CanvasToViewport(canvasPos);
      context.RaiseEdgeLabelEditRequested(edge, screenPos);
    }

    return InputProcessorResult.HandledStay;
  }

  private static void HandleEdgeSelection(
      InputStateContext context,
      Graph graph,
      Edge edge,
      bool ctrlHeld)
  {
    if (!edge.IsSelectable) return;

    if (!ctrlHeld && !edge.IsSelected)
    {
      // Single select: deselect other edges
      foreach (var e in graph.Edges.Where(e => e.IsSelected && e.Id != edge.Id))
      {
        e.IsSelected = false;
      }
      // Also deselect nodes for exclusive edge selection
      foreach (var n in graph.Nodes.Where(n => n.IsSelected))
      {
        n.IsSelected = false;
      }
      edge.IsSelected = true;
    }
    else if (ctrlHeld)
    {
      // Toggle selection
      edge.IsSelected = !edge.IsSelected;
    }
  }

  #endregion
}
