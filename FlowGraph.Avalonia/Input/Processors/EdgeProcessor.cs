using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for edges (selection, reconnection, waypoint manipulation).
/// </summary>
/// <remarks>
/// <para>
/// Edges require special handling because:
/// <list type="bullet">
/// <item>They're hit-tested with tolerance (not rectangular)</item>
/// <item>Endpoints can be dragged to reconnect to different ports</item>
/// <item>Waypoints can be dragged to reshape the edge</item>
/// <item>Edge labels can be clicked for editing</item>
/// </list>
/// </para>
/// <para>
/// <b>Interactions:</b>
/// <list type="bullet">
/// <item>Single click: select edge</item>
/// <item>Click near endpoint: start reconnection (ReconnectingState)</item>
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
    var viewportPos = context.CanvasToViewport(canvasPos);
    var isReadOnly = context.Settings.IsReadOnly;

    // Double-click handling
    if (IsDoubleClick(e))
    {
      return HandleDoubleClick(context, edge, canvasPos, isReadOnly);
    }

    // Check for edge endpoint reconnection (not in read-only mode)
    if (!isReadOnly && context.Settings.ShowEdgeEndpointHandles)
    {
      var reconnectResult = TryStartReconnection(context, graph, edge, canvasPos, viewportPos, e);
      if (reconnectResult != null)
      {
        return reconnectResult;
      }
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

  /// <summary>
  /// Checks if the click is near an edge endpoint and starts reconnection if so.
  /// </summary>
  private static InputProcessorResult? TryStartReconnection(
      InputStateContext context,
      Graph graph,
      Edge edge,
      AvaloniaPoint canvasPos,
      AvaloniaPoint viewportPos,
      PointerPressedEventArgs e)
  {
    var reconnectInfo = CheckEdgeEndpointClick(context, graph, edge, canvasPos);
    if (!reconnectInfo.HasValue)
      return null;

    var (draggingTarget, fixedNode, fixedPort, movingNode, movingPort) = reconnectInfo.Value;

    // Ensure we have theme and canvas for the temp line
    if (context.Theme == null || context.MainCanvas == null)
      return null;

    var reconnectState = new ReconnectingState(
        edge, draggingTarget, fixedNode, fixedPort, movingNode, movingPort, viewportPos, context.Theme);
    reconnectState.CreateTempLine(context.MainCanvas);
    CapturePointer(e, context.RootPanel);

    return InputProcessorResult.TransitionTo(reconnectState);
  }

  /// <summary>
  /// Checks if a click is near an edge endpoint and returns reconnection info if so.
  /// Uses canvas coordinates for distance calculation.
  /// </summary>
  private static (bool draggingTarget, Node fixedNode, Port fixedPort, Node movingNode, Port movingPort)?
      CheckEdgeEndpointClick(InputStateContext context, Graph graph, Edge edge, AvaloniaPoint canvasPos)
  {
    var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
    var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

    if (sourceNode == null || targetNode == null) return null;

    var sourcePort = sourceNode.Outputs.FirstOrDefault(p => p.Id == edge.SourcePort);
    var targetPort = targetNode.Inputs.FirstOrDefault(p => p.Id == edge.TargetPort);

    if (sourcePort == null || targetPort == null) return null;

    // Get port positions in canvas coordinates
    var sourceCanvasPos = context.GraphRenderer.GetPortCanvasPosition(sourceNode, sourcePort, true);
    var targetCanvasPos = context.GraphRenderer.GetPortCanvasPosition(targetNode, targetPort, false);

    // Scale snap distance to canvas coordinates (handle size is in screen pixels)
    var snapDistance = context.Settings.EdgeEndpointHandleSize * 2 / context.Viewport.Zoom;

    // Calculate distances
    var distToSource = Distance(canvasPos, sourceCanvasPos);
    var distToTarget = Distance(canvasPos, targetCanvasPos);

    if (distToTarget < snapDistance && distToTarget < distToSource)
    {
      // Dragging target end - source stays fixed
      return (true, sourceNode, sourcePort, targetNode, targetPort);
    }
    else if (distToSource < snapDistance)
    {
      // Dragging source end - target stays fixed
      return (false, targetNode, targetPort, sourceNode, sourcePort);
    }

    return null;
  }

  private static double Distance(AvaloniaPoint a, AvaloniaPoint b)
  {
    var dx = a.X - b.X;
    var dy = a.Y - b.Y;
    return Math.Sqrt(dx * dx + dy * dy);
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
