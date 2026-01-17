using Avalonia;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// DirectCanvasRenderer partial - Edge rendering methods.
/// </summary>
public partial class DirectCanvasRenderer
{
  private void DrawEdge(DrawingContext context, Edge edge, double zoom, double offsetX, double offsetY, Rect viewBounds, bool useSimplified)
  {
    // O(1) dictionary lookup instead of O(n) FirstOrDefault - massive speedup for large graphs
    if (_nodeById == null || _graph == null) return;
    if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) return;
    if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) return;
    if (!CanvasRenderModel.IsNodeVisible(_graph, sourceNode) || !CanvasRenderModel.IsNodeVisible(_graph, targetNode)) return;

    // Use optimized overload with pre-looked-up nodes
    var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);
    var startScreen = CanvasToScreen(startCanvas, zoom, offsetX, offsetY);
    var endScreen = CanvasToScreen(endCanvas, zoom, offsetX, offsetY);

    // Cull edges outside visible bounds
    var margin = 100 * zoom;
    var edgeMinX = Math.Min(startScreen.X, endScreen.X) - margin;
    var edgeMaxX = Math.Max(startScreen.X, endScreen.X) + margin;
    var edgeMinY = Math.Min(startScreen.Y, endScreen.Y) - margin;
    var edgeMaxY = Math.Max(startScreen.Y, endScreen.Y) + margin;

    if (edgeMaxX < 0 || edgeMinX > viewBounds.Width || edgeMaxY < 0 || edgeMinY > viewBounds.Height)
      return;

    var pen = edge.IsSelected ? _edgeSelectedPen : _edgePen;

    // Simplified rendering: straight line instead of bezier when zoomed out
    if (useSimplified)
    {
      context.DrawLine(pen!, startScreen, endScreen);
    }
    else
    {
      // Get control points (scaled to screen)
      var (cp1Canvas, cp2Canvas) = _model.GetBezierControlPoints(startCanvas, endCanvas);
      var cp1Screen = CanvasToScreen(cp1Canvas, zoom, offsetX, offsetY);
      var cp2Screen = CanvasToScreen(cp2Canvas, zoom, offsetX, offsetY);

      // Draw bezier curve
      var geometry = new StreamGeometry();
      using (var ctx = geometry.Open())
      {
        ctx.BeginFigure(startScreen, false);
        ctx.CubicBezierTo(cp1Screen, cp2Screen, endScreen);
        ctx.EndFigure(false);
      }

      context.DrawGeometry(null, pen, geometry);
    }

    // Draw arrow at end (skip in simplified mode for performance)
    if (!useSimplified && edge.MarkerEnd != EdgeMarker.None)
    {
      var cp2Screen = useSimplified ? startScreen : CanvasToScreen(_model.GetBezierControlPoints(startCanvas, endCanvas).Item2, zoom, offsetX, offsetY);
      var angle = Math.Atan2(endScreen.Y - cp2Screen.Y, endScreen.X - cp2Screen.X);
      DrawArrow(context, endScreen, angle, pen!.Brush, zoom, edge.MarkerEnd == EdgeMarker.ArrowClosed);
    }

    // Draw edge label (skip if being edited or in simplified mode)
    if (!useSimplified && !string.IsNullOrEmpty(edge.Label) && _editingEdgeId != edge.Id)
    {
      var midCanvas = _model.GetEdgeMidpoint(startCanvas, endCanvas);
      var midScreen = CanvasToScreen(midCanvas, zoom, offsetX, offsetY);
      DrawEdgeLabel(context, edge.Label, midScreen, zoom);
    }
  }

  private void DrawEdgeEndpointHandles(DrawingContext context, Edge edge, double zoom, double offsetX, double offsetY)
  {
    // Use O(1) dictionary lookup instead of O(n) FirstOrDefault
    if (_nodeById == null) return;
    if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) return;
    if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) return;

    // Use optimized overload with pre-looked-up nodes
    var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);
    var startScreen = CanvasToScreen(startCanvas, zoom, offsetX, offsetY);
    var endScreen = CanvasToScreen(endCanvas, zoom, offsetX, offsetY);

    var handleSize = _settings.EdgeEndpointHandleSize * zoom;
    var halfSize = handleSize / 2;

    // Source handle
    var isSourceHovered = _hoveredEndpointHandle.HasValue &&
                          _hoveredEndpointHandle.Value.edgeId == edge.Id &&
                          _hoveredEndpointHandle.Value.isSource;
    var sourceFill = isSourceHovered ? _theme!.PortHover : _endpointHandleFill;
    context.DrawEllipse(sourceFill, _endpointHandlePen, startScreen, halfSize, halfSize);

    // Target handle
    var isTargetHovered = _hoveredEndpointHandle.HasValue &&
                          _hoveredEndpointHandle.Value.edgeId == edge.Id &&
                          !_hoveredEndpointHandle.Value.isSource;
    var targetFill = isTargetHovered ? _theme!.PortHover : _endpointHandleFill;
    context.DrawEllipse(targetFill, _endpointHandlePen, endScreen, halfSize, halfSize);
  }

  private void DrawEdgeLabel(DrawingContext context, string label, AvaloniaPoint midScreen, double zoom)
  {
    var fontSize = 12 * zoom;
    var formattedText = new FormattedText(
        label,
        System.Globalization.CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        _typeface,
        fontSize,
        _theme!.NodeText);

    var padding = 4 * zoom;
    var bgRect = new Rect(
        midScreen.X - padding,
        midScreen.Y - 10 * zoom - padding,
        formattedText.Width + padding * 2,
        formattedText.Height + padding * 2);

    context.DrawRectangle(_theme.NodeBackground, null, bgRect, 3 * zoom, 3 * zoom);
    context.DrawText(formattedText, new AvaloniaPoint(midScreen.X, midScreen.Y - 10 * zoom));
  }

  private void DrawArrow(DrawingContext context, AvaloniaPoint tip, double angle, IBrush? brush, double zoom, bool filled)
  {
    var arrowSize = GraphDefaults.EdgeArrowSize * zoom;

    // Use shared helper for arrow point calculation
    var (p1, p2) = ArrowGeometryHelper.CalculateArrowPoints(tip, angle, arrowSize);

    var geometry = new StreamGeometry();
    using (var ctx = geometry.Open())
    {
      ctx.BeginFigure(tip, filled);
      ctx.LineTo(p1);
      ctx.LineTo(p2);
      ctx.EndFigure(true);
    }

    var pen = filled ? null : new Pen(brush, 2 * zoom);
    context.DrawGeometry(filled ? brush : null, pen, geometry);
  }
}
