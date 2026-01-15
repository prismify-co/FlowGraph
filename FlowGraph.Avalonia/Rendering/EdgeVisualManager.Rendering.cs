// EdgeVisualManager.Rendering.cs
// Partial class containing edge rendering logic (RenderEdges, RenderEdge, RenderCustomEdge)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

public partial class EdgeVisualManager
{
  /// <summary>
  /// Renders all edges in the graph to the canvas.
  /// </summary>
  /// <param name="canvas">The canvas to render to.</param>
  /// <param name="graph">The graph containing edges.</param>
  /// <param name="theme">Theme resources for styling.</param>
  /// <param name="excludePath">Optional path to exclude from cleanup.</param>
  public void RenderEdges(Canvas canvas, Graph graph, ThemeResources theme, AvaloniaPath? excludePath = null)
  {
    // Remove previously tracked edge visuals from canvas
    // This is safer than pattern matching - we only remove what we created
    foreach (var (_, hitPath) in _edgeVisuals)
    {
      if (hitPath != excludePath)
        canvas.Children.Remove(hitPath);
    }
    foreach (var (_, visiblePath) in _edgeVisiblePaths)
    {
      if (visiblePath != excludePath)
        canvas.Children.Remove(visiblePath);
    }
    foreach (var (_, markers) in _edgeMarkers)
    {
      foreach (var marker in markers)
      {
        canvas.Children.Remove(marker);
      }
    }
    foreach (var (_, label) in _edgeLabels)
    {
      canvas.Children.Remove(label);
    }
    foreach (var (_, handles) in _edgeEndpointHandles)
    {
      canvas.Children.Remove(handles.source);
      canvas.Children.Remove(handles.target);
    }

    // Clear edge visuals dictionaries after removing from canvas
    _edgeVisuals.Clear();
    _edgeVisiblePaths.Clear();
    _edgeMarkers.Clear();
    _edgeLabels.Clear();
    _edgeEndpointHandles.Clear();

    // Render new edges - only if both endpoints are visible (by group collapse)
    // and at least one endpoint is in the visible viewport (virtualization)
    foreach (var edge in graph.Elements.Edges)
    {
      var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
      var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

      // Skip edges where either endpoint is hidden by a collapsed group
      if (sourceNode == null || targetNode == null ||
          !NodeVisualManager.IsNodeVisible(graph, sourceNode) ||
          !NodeVisualManager.IsNodeVisible(graph, targetNode))
      {
        continue;
      }

      // Virtualization: Skip edges where both endpoints are outside visible bounds
      if (!IsEdgeInVisibleBounds(sourceNode, targetNode))
      {
        continue;
      }

      RenderEdge(canvas, edge, graph, theme);
    }
  }

  /// <summary>
  /// Renders a single edge with its markers and optional label.
  /// </summary>
  /// <param name="canvas">The canvas to render to.</param>
  /// <param name="edge">The edge to render.</param>
  /// <param name="graph">The graph containing the edge.</param>
  /// <param name="theme">Theme resources for styling.</param>
  /// <returns>The created hit area path, or null if rendering failed.</returns>
  public AvaloniaPath? RenderEdge(Canvas canvas, Edge edge, Graph graph, ThemeResources theme)
  {
    var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
    var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

    if (sourceNode == null || targetNode == null)
      return null;

    // Find the actual port objects to get their positions
    var sourcePort = sourceNode.Outputs.FirstOrDefault(p => p.Id == edge.SourcePort);
    var targetPort = targetNode.Inputs.FirstOrDefault(p => p.Id == edge.TargetPort);

    // Get node dimensions for proper port positioning
    var (sourceWidth, sourceHeight) = _nodeVisualManager.GetNodeDimensions(sourceNode);
    var (targetWidth, targetHeight) = _nodeVisualManager.GetNodeDimensions(targetNode);

    // Calculate port positions based on Port.Position property
    var (sourceX, sourceY) = GetPortCanvasPosition(
        sourceNode, sourcePort, sourceWidth, sourceHeight, isOutput: true);
    var (targetX, targetY) = GetPortCanvasPosition(
        targetNode, targetPort, targetWidth, targetHeight, isOutput: false);

    // Use canvas coordinates directly (MainCanvas transform handles viewport mapping)
    var startPoint = new AvaloniaPoint(sourceX, sourceY);
    var endPoint = new AvaloniaPoint(targetX, targetY);

    // In transform-based rendering, Scale=1.0 always. MatrixTransform handles zoom.
    // We still get the viewport zoom for calculations that need it (like inverse scale for constant-size elements)
    var viewportZoom = _renderContext.ViewportZoom;

    // Check for custom edge renderer
    var customRenderer = _edgeRendererRegistry?.GetRenderer(edge);
    if (customRenderer != null)
    {
      return RenderCustomEdge(canvas, edge, graph, theme, customRenderer, sourceNode, targetNode, startPoint, endPoint, viewportZoom);
    }

    // Create path based on edge type - use waypoints if available
    PathGeometry pathGeometry;
    var waypoints = edge.Waypoints;  // Get once to avoid multiple ToList() calls
    IReadOnlyList<Core.Point>? transformedWaypoints = null;

    if (waypoints != null && waypoints.Count > 0)
    {
      // Waypoints are already in canvas coordinates
      transformedWaypoints = waypoints;

      pathGeometry = EdgePathHelper.CreatePathWithWaypoints(
          startPoint,
          endPoint,
          transformedWaypoints,
          edge.Type);
    }
    else
    {
      pathGeometry = EdgePathHelper.CreatePath(startPoint, endPoint, edge.Type);
    }

    var strokeBrush = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;

    // Create visible edge path - use logical (unscaled) dimensions
    // MatrixTransform handles all zoom scaling
    var visiblePath = new AvaloniaPath
    {
      Data = pathGeometry,
      Stroke = strokeBrush,
      StrokeThickness = edge.IsSelected ? 3 : 2,
      StrokeDashArray = null,
      IsHitTestVisible = false
    };

    // Create invisible hit area path (wider, transparent stroke for easier clicking)
    // Use logical dimensions - MatrixTransform scales the hit area appropriately
    var hitAreaPath = new AvaloniaPath
    {
      Data = pathGeometry,
      Stroke = Brushes.Transparent,
      StrokeThickness = _renderContext.Settings.EdgeHitAreaWidth,
      Tag = edge,
      Cursor = new Cursor(StandardCursorType.Hand)
    };

    // Add paths to canvas
    canvas.Children.Add(visiblePath);
    canvas.Children.Add(hitAreaPath);

    // Track both paths
    _edgeVisuals[edge.Id] = hitAreaPath;
    _edgeVisiblePaths[edge.Id] = visiblePath;

    // Track markers for this edge
    var markers = new List<AvaloniaPath>();

    // Render end marker (arrow) - use logical dimensions
    if (edge.MarkerEnd != EdgeMarker.None)
    {
      var lastFromPoint = GetLastFromPoint(startPoint, endPoint, edge.Waypoints, edge.Type);
      // Use port position if available, otherwise default to Left for input ports
      var targetPortPosition = targetPort?.Position ?? PortPosition.Left;
      var markerPath = RenderEdgeMarker(canvas, endPoint, lastFromPoint, edge.MarkerEnd, strokeBrush, targetPortPosition);
      if (markerPath != null)
      {
        markers.Add(markerPath);
      }
    }

    // Render start marker - use logical dimensions
    if (edge.MarkerStart != EdgeMarker.None)
    {
      var firstToPoint = GetFirstToPoint(startPoint, endPoint, edge.Waypoints, edge.Type);
      // Use port position if available, otherwise default to Right for output ports
      var sourcePortPosition = sourcePort?.Position ?? PortPosition.Right;
      var markerPath = RenderEdgeMarker(canvas, startPoint, firstToPoint, edge.MarkerStart, strokeBrush, sourcePortPosition);
      if (markerPath != null)
      {
        markers.Add(markerPath);
      }
    }

    // Store markers for animation
    if (markers.Count > 0)
    {
      _edgeMarkers[edge.Id] = markers;
    }

    // Render label if present (use LabelInfo if available, else fall back to Label)
    var effectiveLabel = edge.Definition.EffectiveLabel;
    if (!string.IsNullOrEmpty(effectiveLabel))
    {
      // Pass transformed waypoints for accurate label positioning along the routed path
      var labelVisual = RenderEdgeLabel(canvas, startPoint, endPoint, transformedWaypoints, edge, theme);
      if (labelVisual != null)
      {
        _edgeLabels[edge.Id] = labelVisual;
      }
    }

    return hitAreaPath;
  }

  /// <summary>
  /// Updates the selection visual state of an edge.
  /// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
  /// </summary>
  /// <param name="edge">The edge to update.</param>
  /// <param name="theme">Theme resources for styling.</param>
  public void UpdateEdgeSelection(Edge edge, ThemeResources theme)
  {
    // Check if this edge has a custom render result
    if (_customRenderResults.TryGetValue(edge.Id, out var customResult))
    {
      var customRenderer = _edgeRendererRegistry?.GetRenderer(edge);
      if (customRenderer != null)
      {
        var context = new EdgeRenderers.EdgeRenderContext
        {
          Theme = theme,
          Settings = _renderContext.Settings,
          Scale = _renderContext.Scale, // Scale is 1.0 in transform-based rendering
          SourceNode = null!, // Not needed for selection update
          TargetNode = null!,
          StartPoint = default,
          EndPoint = default,
          Graph = null!
        };
        customRenderer.UpdateSelection(customResult, edge, context);
        return;
      }
    }

    if (_edgeVisiblePaths.TryGetValue(edge.Id, out var visiblePath))
    {
      // Use logical (unscaled) dimensions - MatrixTransform handles zoom
      visiblePath.Stroke = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;
      visiblePath.StrokeThickness = edge.IsSelected ? 3 : 2;
    }
  }

  /// <summary>
  /// Renders an edge using a custom renderer.
  /// </summary>
  private AvaloniaPath? RenderCustomEdge(
      Canvas canvas,
      Edge edge,
      Graph graph,
      ThemeResources theme,
      EdgeRenderers.IEdgeRenderer renderer,
      Node sourceNode,
      Node targetNode,
      AvaloniaPoint startPoint,
      AvaloniaPoint endPoint,
      double viewportZoom)
  {
    IReadOnlyList<AvaloniaPoint>? transformedWaypoints = null;
    var waypoints = edge.Waypoints; // cloned list via Edge.State
    if (waypoints != null && waypoints.Count > 0)
    {
      // Convert Core.Point waypoints to AvaloniaPoint (already in canvas coords)
      transformedWaypoints = waypoints
          .Select(wp => new AvaloniaPoint(wp.X, wp.Y))
          .ToList();
    }

    // Scale is 1.0 in transform-based rendering - MatrixTransform handles zoom
    var context = new EdgeRenderers.EdgeRenderContext
    {
      Theme = theme,
      Settings = _renderContext.Settings,
      Scale = 1.0, // Transform-based rendering
      SourceNode = sourceNode,
      TargetNode = targetNode,
      StartPoint = startPoint,
      EndPoint = endPoint,
      Waypoints = transformedWaypoints,
      Graph = graph
    };

    var result = renderer.Render(edge, context);

    // Add visuals to canvas
    canvas.Children.Add(result.VisiblePath);
    canvas.Children.Add(result.HitAreaPath);

    // Track the paths
    _edgeVisuals[edge.Id] = result.HitAreaPath;
    _edgeVisiblePaths[edge.Id] = result.VisiblePath;
    _customRenderResults[edge.Id] = result;

    // Add markers if present
    if (result.Markers is { Count: > 0 })
    {
      var markerList = new List<AvaloniaPath>();
      foreach (var marker in result.Markers)
      {
        canvas.Children.Add(marker);
        markerList.Add(marker);
      }
      _edgeMarkers[edge.Id] = markerList;
    }

    // Add label if present
    if (result.Label != null)
    {
      canvas.Children.Add(result.Label);
      if (result.Label is TextBlock tb)
      {
        _edgeLabels[edge.Id] = tb;
      }
    }

    // Add additional visuals
    if (result.AdditionalVisuals != null)
    {
      foreach (var visual in result.AdditionalVisuals)
      {
        canvas.Children.Add(visual);
      }
    }

    return result.HitAreaPath;
  }
}
