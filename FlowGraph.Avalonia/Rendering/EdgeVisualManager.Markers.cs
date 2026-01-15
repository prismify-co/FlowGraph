// EdgeVisualManager.Markers.cs
// Partial class containing edge marker (arrow) rendering and angle calculation logic

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

public partial class EdgeVisualManager
{
  /// <summary>
  /// Gets the "from" point for calculating the end marker angle.
  /// For bezier curves, calculates the approach direction based on control points.
  /// </summary>
  private AvaloniaPoint GetLastFromPoint(AvaloniaPoint start, AvaloniaPoint end, List<Core.Point>? waypoints, EdgeType edgeType = EdgeType.Bezier)
  {
    if (waypoints != null && waypoints.Count > 0)
    {
      var lastWaypoint = waypoints[^1];
      // Waypoints are already in canvas coordinates
      return new AvaloniaPoint(lastWaypoint.X, lastWaypoint.Y);
    }

    // For bezier curves, calculate the approach direction from the second control point
    if (edgeType == EdgeType.Bezier)
    {
      return CalculateBezierApproachPoint(start, end, isEndPoint: true);
    }

    return start;
  }

  /// <summary>
  /// Gets the "to" point for calculating the start marker angle.
  /// For bezier curves, calculates the departure direction based on control points.
  /// </summary>
  private AvaloniaPoint GetFirstToPoint(AvaloniaPoint start, AvaloniaPoint end, List<Core.Point>? waypoints, EdgeType edgeType = EdgeType.Bezier)
  {
    if (waypoints != null && waypoints.Count > 0)
    {
      var firstWaypoint = waypoints[0];
      // Waypoints are already in canvas coordinates
      return new AvaloniaPoint(firstWaypoint.X, firstWaypoint.Y);
    }

    // For bezier curves, calculate the departure direction from the first control point
    if (edgeType == EdgeType.Bezier)
    {
      return CalculateBezierApproachPoint(start, end, isEndPoint: false);
    }

    return end;
  }

  /// <summary>
  /// Calculates a point representing the bezier curve's approach/departure direction.
  /// This mimics the control point calculation from EdgePathHelper.CreateBezierPath.
  /// </summary>
  private static AvaloniaPoint CalculateBezierApproachPoint(AvaloniaPoint start, AvaloniaPoint end, bool isEndPoint)
  {
    var dx = end.X - start.X;
    var dy = end.Y - start.Y;
    var controlPointOffset = Math.Abs(dx) / 2;
    controlPointOffset = Math.Max(controlPointOffset, 50);

    // Mirror the control point logic from EdgePathHelper.CalculateDirectionAwareControlPoints
    if (dx > Math.Abs(dy) * 0.5)
    {
      // Standard left-to-right bezier - horizontal approach
      return isEndPoint
          ? new AvaloniaPoint(end.X - controlPointOffset, end.Y)
          : new AvaloniaPoint(start.X + controlPointOffset, start.Y);
    }

    if (dx < -Math.Abs(dy) * 0.5)
    {
      // Right-to-left (backwards) - horizontal approach
      return isEndPoint
          ? new AvaloniaPoint(end.X - controlPointOffset, end.Y)
          : new AvaloniaPoint(start.X + controlPointOffset, start.Y);
    }

    // Mostly vertical edges
    if (Math.Abs(dx) < 10)
    {
      // Pure vertical
      var verticalDist = Math.Abs(dy) / 3;
      return isEndPoint
          ? new AvaloniaPoint(end.X, end.Y - (dy > 0 ? verticalDist : -verticalDist))
          : new AvaloniaPoint(start.X, start.Y + (dy > 0 ? verticalDist : -verticalDist));
    }

    if (dx >= 0)
    {
      // Going down-right - slight horizontal offset
      var hOffset = Math.Min(controlPointOffset * 0.3, Math.Abs(dx) * 0.5);
      return isEndPoint
          ? new AvaloniaPoint(end.X - hOffset, end.Y - controlPointOffset * 0.4)
          : new AvaloniaPoint(start.X + hOffset, start.Y + controlPointOffset * 0.4);
    }
    else
    {
      // Going down-left
      return isEndPoint
          ? new AvaloniaPoint(end.X - controlPointOffset * 0.3, end.Y - Math.Abs(dy) * 0.3)
          : new AvaloniaPoint(start.X + controlPointOffset * 0.3, start.Y + Math.Abs(dy) * 0.3);
    }
  }

  /// <summary>
  /// Renders a marker (arrow) at an edge endpoint.
  /// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
  /// </summary>
  /// <param name="canvas">The canvas to render on.</param>
  /// <param name="point">The marker tip position.</param>
  /// <param name="fromPoint">The point the edge comes from (for angle calculation).</param>
  /// <param name="marker">The marker type.</param>
  /// <param name="stroke">The stroke brush.</param>
  /// <param name="portPosition">Optional port position for accurate arrow direction.</param>
  /// <returns>The rendered marker path.</returns>
  private AvaloniaPath RenderEdgeMarker(
      Canvas canvas,
      AvaloniaPoint point,
      AvaloniaPoint fromPoint,
      EdgeMarker marker,
      IBrush stroke,
      PortPosition? portPosition = null)
  {
    // Use the hybrid angle calculation for consistent arrow direction
    var angle = EdgePathHelper.CalculateArrowAngle(point, fromPoint, portPosition);
    // Use logical (unscaled) marker size - MatrixTransform handles zoom
    var markerSize = 10;
    var isClosed = marker == EdgeMarker.ArrowClosed;

    var markerGeometry = EdgePathHelper.CreateArrowMarker(point, angle, markerSize, isClosed);

    var markerPath = new AvaloniaPath
    {
      Data = markerGeometry,
      Stroke = stroke,
      StrokeThickness = 2,
      Fill = isClosed ? stroke : null,
      Tag = "marker"
    };

    canvas.Children.Add(markerPath);
    return markerPath;
  }
}
