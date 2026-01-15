using Avalonia;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// DirectGraphRenderer partial - Hit testing methods.
/// </summary>
public partial class DirectGraphRenderer
{
  /// <summary>
  /// Performs hit testing to find an edge endpoint handle at the given screen coordinates.
  /// </summary>
  /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <returns>Tuple of (edge, isSource) or null if no handle hit.</returns>
  public (Edge edge, bool isSource)? HitTestEdgeEndpointHandle(double screenX, double screenY)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    if (_graph == null || !_settings.ShowEdgeEndpointHandles || _nodeById == null) return null;

    var ctx = CreateHitTestContext(screenX, screenY);
    if (ctx == null) return null;
    var context = ctx.Value;

    var handleRadius = _settings.EdgeEndpointHandleSize / 2 + 4; // Extra padding for easier clicking

    int edgesChecked = 0;
    int edgesSkippedViewport = 0;
    foreach (var edge in _graph.Elements.Edges)
    {
      if (!edge.IsSelected) continue;
      if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) continue;
      if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) continue;
      if (!IsNodeVisibleFast(sourceNode) || !IsNodeVisibleFast(targetNode)) continue;

      // VIEWPORT CULLING: Skip edges where BOTH endpoints are outside viewport
      var sourceInViewport = IsInVisibleBounds(sourceNode, context.Zoom, context.OffsetX, context.OffsetY, context.ViewBounds);
      var targetInViewport = IsInVisibleBounds(targetNode, context.Zoom, context.OffsetX, context.OffsetY, context.ViewBounds);
      if (!sourceInViewport && !targetInViewport)
      {
        edgesSkippedViewport++;
        continue;
      }

      edgesChecked++;
      var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);

      // Check source handle
      if (IsPointInCircle(context.CanvasPoint, startCanvas, handleRadius))
        return (edge, true);

      // Check target handle
      if (IsPointInCircle(context.CanvasPoint, endCanvas, handleRadius))
        return (edge, false);
    }

    LogHitTestNoHit("HitTestEdgeEndpointHandle", sw, edgesChecked, edgesSkippedViewport);
    return null;
  }

  /// <summary>
  /// Performs hit testing to find a resize handle at the given screen coordinates.
  /// </summary>
  /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <returns>Tuple of (node, position) or null if no handle hit.</returns>
  public (Node node, ResizeHandlePosition position)? HitTestResizeHandle(double screenX, double screenY)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    if (_graph == null) return null;

    var ctx = CreateHitTestContext(screenX, screenY);
    if (ctx == null) return null;
    var context = ctx.Value;

    int nodesChecked = 0;
    int nodesSkippedViewport = 0;
    foreach (var node in _graph.Elements.Nodes)
    {
      if (!node.IsSelected || !node.IsResizable) continue;
      if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;

      // VIEWPORT CULLING: Skip nodes outside visible area
      if (!IsInVisibleBounds(node, context.Zoom, context.OffsetX, context.OffsetY, context.ViewBounds))
      {
        nodesSkippedViewport++;
        continue;
      }

      nodesChecked++;
      foreach (var (pos, center) in _model.GetResizeHandlePositions(node))
      {
        if (_model.IsPointInResizeHandle(context.CanvasPoint, center))
        {
          return (node, pos);
        }
      }
    }

    LogHitTestNoHit("HitTestResizeHandle", sw, nodesChecked, nodesSkippedViewport);
    return null;
  }

  /// <summary>
  /// Performs hit testing to find a node at the given screen coordinates.
  /// </summary>
  /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <returns>The hit node, or null if no node was hit.</returns>
  public Node? HitTestNode(double screenX, double screenY)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();

    if (_graph == null) return null;

    long rebuildTime = 0;
    if (_indexDirty)
    {
      var rebuildSw = System.Diagnostics.Stopwatch.StartNew();
      RebuildSpatialIndex();
      rebuildTime = rebuildSw.ElapsedMilliseconds;
    }

    if (_nodeIndex == null) return null;

    var ctx = CreateHitTestContext(screenX, screenY);
    if (ctx == null) return null;
    var context = ctx.Value;

    // Check regular nodes first (they're on top)
    var regularCheckStart = sw.ElapsedMilliseconds;
    int nodesChecked = 0;
    int nodesSkippedViewport = 0;
    for (int i = _nodeIndex.Count - 1; i >= 0; i--)
    {
      var (node, nx, ny, nw, nh) = _nodeIndex[i];

      // VIEWPORT CULLING: Skip nodes outside visible area
      if (!context.IsNodeIndexEntryVisible(nx, ny, nw, nh))
      {
        nodesSkippedViewport++;
        continue;
      }

      nodesChecked++;
      var bounds = new Rect(nx, ny, nw, nh);

      if (bounds.Contains(context.CanvasPoint))
      {
        // Get screen dimensions for zoom-adaptive click detection
        var (_, _, screenW, screenH) = context.GetScreenBounds(nx, ny, nw, nh);

        // Check if click passes zoom-adaptive proximity test
        if (!IsClickValidForNodeSize(context.CanvasPoint, nx, ny, nw, nh, screenW, screenH, node.Id))
          continue;

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HitTest] Regular node found in {sw.ElapsedMilliseconds}ms (rebuild:{rebuildTime}ms, checked:{nodesChecked}, skippedViewport:{nodesSkippedViewport})");
        return node;
      }
    }
    var regularCheckTime = sw.ElapsedMilliseconds - regularCheckStart;

    // Check groups (they're behind regular nodes)
    var groupCheckStart = sw.ElapsedMilliseconds;
    int groupsChecked = 0;
    if (_nodeById != null)
    {
      foreach (var kvp in _nodeById)
      {
        var node = kvp.Value;
        if (!node.IsGroup) continue;

        groupsChecked++;
        if (!IsNodeVisibleFast(node)) continue;

        var bounds = _model.GetNodeBounds(node);
        if (bounds.Contains(context.CanvasPoint))
        {
          sw.Stop();
          System.Diagnostics.Debug.WriteLine($"[HitTest] Group found in {sw.ElapsedMilliseconds}ms | Rebuild:{rebuildTime}ms, Regular:{regularCheckTime}ms, Groups:{sw.ElapsedMilliseconds - groupCheckStart}ms, GroupsChecked:{groupsChecked}");
          return node;
        }
      }
    }
    var groupCheckTime = sw.ElapsedMilliseconds - groupCheckStart;

    sw.Stop();
    System.Diagnostics.Debug.WriteLine($"[HitTest] No hit in {sw.ElapsedMilliseconds}ms | Rebuild:{rebuildTime}ms, Regular:{regularCheckTime}ms, Groups:{groupCheckTime}ms, GroupsChecked:{groupsChecked}");
    return null;
  }

  /// <summary>
  /// Validates if a click is close enough to node center based on screen size.
  /// At low zoom (small screen size), only inner portion of node is clickable.
  /// </summary>
  private static bool IsClickValidForNodeSize(
      AvaloniaPoint canvasPoint, double nx, double ny, double nw, double nh,
      double screenW, double screenH, string nodeId)
  {
    const double MinClickableScreenSize = 60.0; // Below this, restrict clickable area
    const double MinCenterProximity = 0.4;      // At tiny sizes, only inner 40% is clickable

    var smallestScreenDim = Math.Min(screenW, screenH);

    // Calculate how far from center (as percentage of dimensions, 0=center, 1=edge)
    var centerX = nx + nw / 2;
    var centerY = ny + nh / 2;
    var distFromCenterX = Math.Abs(canvasPoint.X - centerX) / (nw / 2);
    var distFromCenterY = Math.Abs(canvasPoint.Y - centerY) / (nh / 2);

    // Scale clickable area based on screen pixel size
    double maxDistFromCenter;
    if (smallestScreenDim >= MinClickableScreenSize)
    {
      maxDistFromCenter = 1.0; // Full bounds clickable
    }
    else
    {
      // Linear interpolation: at 60px=1.0, at 0px=0.4
      var t = smallestScreenDim / MinClickableScreenSize;
      maxDistFromCenter = MinCenterProximity + t * (1.0 - MinCenterProximity);
    }

    if (distFromCenterX > maxDistFromCenter || distFromCenterY > maxDistFromCenter)
    {
      System.Diagnostics.Debug.WriteLine($"[HitTest] Node {nodeId} skipped: click at edge ({distFromCenterX:P0},{distFromCenterY:P0}) > maxDist {maxDistFromCenter:P0} (screenSize={smallestScreenDim:F0}px)");
      return false;
    }

    return true;
  }

  /// <summary>
  /// Performs hit testing to find a port at the given screen coordinates.
  /// </summary>
  /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <returns>Tuple of (node, port, isOutput) or null if no port hit.</returns>
  public (Node node, Port port, bool isOutput)? HitTestPort(double screenX, double screenY)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    if (_graph == null) return null;

    if (_indexDirty) RebuildSpatialIndex();
    if (_nodeIndex == null) return null;

    var ctx = CreateHitTestContext(screenX, screenY);
    if (ctx == null) return null;
    var context = ctx.Value;
    var portBuffer = _settings.PortSize * context.Zoom;

    // Check regular nodes - with viewport culling
    int nodesChecked = 0;
    int nodesSkippedViewport = 0;
    foreach (var (node, nx, ny, nw, nh) in _nodeIndex)
    {
      // VIEWPORT CULLING: Quick bounds check using cached spatial index data
      if (!context.IsNodeIndexEntryVisible(nx, ny, nw, nh, portBuffer))
      {
        nodesSkippedViewport++;
        continue;
      }

      nodesChecked++;
      // Check input ports
      for (int i = 0; i < node.Inputs.Count; i++)
      {
        var portPos = _model.GetPortPositionByIndex(node, i, node.Inputs.Count, false);
        if (_model.IsPointInPort(context.CanvasPoint, portPos))
        {
          return (node, node.Inputs[i], false);
        }
      }

      // Check output ports
      for (int i = 0; i < node.Outputs.Count; i++)
      {
        var portPos = _model.GetPortPositionByIndex(node, i, node.Outputs.Count, true);
        if (_model.IsPointInPort(context.CanvasPoint, portPos))
        {
          return (node, node.Outputs[i], true);
        }
      }
    }

    // Check group ports
    int groupsChecked = 0;
    int groupsSkippedViewport = 0;
    foreach (var group in _graph.Elements.Nodes.Where(n => n.IsGroup))
    {
      if (!GraphRenderModel.IsNodeVisible(_graph, group)) continue;

      // VIEWPORT CULLING: Skip groups outside visible area
      if (!IsInVisibleBounds(group, context.Zoom, context.OffsetX, context.OffsetY, context.ViewBounds))
      {
        groupsSkippedViewport++;
        continue;
      }

      groupsChecked++;
      // Check input ports
      for (int i = 0; i < group.Inputs.Count; i++)
      {
        var portPos = _model.GetPortPositionByIndex(group, i, group.Inputs.Count, false);
        if (_model.IsPointInPort(context.CanvasPoint, portPos))
        {
          return (group, group.Inputs[i], false);
        }
      }

      // Check output ports
      for (int i = 0; i < group.Outputs.Count; i++)
      {
        var portPos = _model.GetPortPositionByIndex(group, i, group.Outputs.Count, true);
        if (_model.IsPointInPort(context.CanvasPoint, portPos))
        {
          return (group, group.Outputs[i], true);
        }
      }
    }

    sw.Stop();
    System.Diagnostics.Debug.WriteLine($"[HitTestPort] No hit in {sw.ElapsedMilliseconds}ms | NodesChecked:{nodesChecked}, SkippedViewport:{nodesSkippedViewport}, Groups:{groupsChecked}, GroupsSkipped:{groupsSkippedViewport}");
    return null;
  }

  /// <summary>
  /// Performs hit testing to find an edge at the given screen coordinates.
  /// </summary>
  /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
  /// <returns>The hit edge, or null if no edge was hit.</returns>
  public Edge? HitTestEdge(double screenX, double screenY)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    if (_graph == null || _nodeById == null) return null;

    var ctx = CreateHitTestContext(screenX, screenY);
    if (ctx == null) return null;
    var context = ctx.Value;

    // Edge hit distance: When zoomed OUT, cap at base value for tight screen pixels.
    // When zoomed IN, scale to maintain consistent screen pixel distance.
    var hitDistance = Math.Min(_settings.EdgeHitAreaWidth / context.Zoom, (double)_settings.EdgeHitAreaWidth);

    int edgesChecked = 0;
    int edgesSkippedViewport = 0;

    foreach (var edge in _graph.Elements.Edges)
    {
      if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) continue;
      if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) continue;
      if (!IsNodeVisibleFast(sourceNode) || !IsNodeVisibleFast(targetNode)) continue;

      // VIEWPORT CULLING: Skip edges where BOTH endpoints are far outside viewport
      var sourceInViewport = IsInVisibleBounds(sourceNode, context.Zoom, context.OffsetX, context.OffsetY, context.ViewBounds);
      var targetInViewport = IsInVisibleBounds(targetNode, context.Zoom, context.OffsetX, context.OffsetY, context.ViewBounds);
      if (!sourceInViewport && !targetInViewport)
      {
        edgesSkippedViewport++;
        continue;
      }

      edgesChecked++;
      var (start, end) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);

      if (_model.IsPointNearEdge(context.CanvasPoint, start, end, hitDistance))
      {
        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HitTestEdge] Hit in {sw.ElapsedMilliseconds}ms | EdgesChecked:{edgesChecked}, SkippedViewport:{edgesSkippedViewport}");
        return edge;
      }
    }

    LogHitTestNoHit("HitTestEdge", sw, edgesChecked, edgesSkippedViewport);
    return null;
  }

  /// <summary>
  /// Gets the screen position for a port (for creating connection temp lines, etc.)
  /// </summary>
  public AvaloniaPoint GetPortScreenPosition(Node node, Port port, bool isOutput)
  {
    if (_viewport == null) return default;

    var canvasPos = _model.GetPortPosition(node, port, isOutput);
    return CanvasToScreen(canvasPos, _viewport.Zoom, _viewport.OffsetX, _viewport.OffsetY);
  }

  /// <summary>
  /// Gets the screen position for an edge endpoint (source or target).
  /// </summary>
  public AvaloniaPoint GetEdgeEndpointScreenPosition(Edge edge, bool isSource)
  {
    if (_graph == null || _viewport == null || _nodeById == null) return default;

    // Use O(1) dictionary lookup instead of O(n) FirstOrDefault
    if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) return default;
    if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) return default;

    // Use optimized overload with pre-looked-up nodes
    var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);
    var canvasPos = isSource ? startCanvas : endCanvas;
    return CanvasToScreen(canvasPos, _viewport.Zoom, _viewport.OffsetX, _viewport.OffsetY);
  }

  /// <summary>
  /// Hit tests for shapes at the given screen coordinates.
  /// Shapes with higher ZIndex are tested first (topmost shapes have priority).
  /// </summary>
  /// <param name="screenX">Screen X coordinate.</param>
  /// <param name="screenY">Screen Y coordinate.</param>
  /// <returns>The hit shape, or null if no shape was hit.</returns>
  public Core.Elements.Shapes.ShapeElement? HitTestShape(double screenX, double screenY)
  {
    if (_graph == null || _viewport == null) return null;

    var canvasPoint = ScreenToCanvas(screenX, screenY);
    var shapes = _graph.Elements.Shapes;
    if (shapes.Count == 0) return null;

    // Test shapes in reverse ZIndex order (highest first = topmost)
    // Filter to only visible and selectable shapes
    var testOrder = shapes
        .Where(s => s.IsVisible && s.IsSelectable)
        .OrderByDescending(s => s.ZIndex)
        .ToList();

    foreach (var shape in testOrder)
    {
      var bounds = shape.GetBounds();
      if (bounds.Contains(new Core.Point(canvasPoint.X, canvasPoint.Y)))
      {
        System.Diagnostics.Debug.WriteLine($"[HitTestShape] Hit shape {shape.Id} (type: {shape.Type}) at canvas ({canvasPoint.X:F0},{canvasPoint.Y:F0})");
        return shape;
      }
    }

    return null;
  }
}
