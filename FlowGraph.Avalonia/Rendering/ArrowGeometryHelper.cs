using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Helper class for calculating arrow geometry points.
/// Provides shared calculations for both retained-mode (PathGeometry) and immediate-mode (DrawingContext) rendering.
/// </summary>
public static class ArrowGeometryHelper
{
  /// <summary>
  /// Calculates the two back points of an arrow given the tip, angle, and size.
  /// </summary>
  /// <param name="tip">The tip (point) of the arrow.</param>
  /// <param name="angle">The angle the arrow is pointing in radians (0 = pointing right).</param>
  /// <param name="size">The size of the arrow (default uses GraphDefaults.EdgeArrowSize).</param>
  /// <returns>Two points that form the back of the arrow.</returns>
  /// <remarks>
  /// The arrow spreads at 30 degrees (π/6) on each side from the direction.
  /// This is used by both EdgePathHelper.CreateArrowMarker and DirectCanvasRenderer.DrawArrow.
  /// </remarks>
  public static (AvaloniaPoint p1, AvaloniaPoint p2) CalculateArrowPoints(
      AvaloniaPoint tip,
      double angle,
      double size = 0)
  {
    // Use default size if not specified
    if (size <= 0)
      size = GraphDefaults.EdgeArrowSize;

    // Arrow points spread at 30 degrees (π/6) on each side
    var arrowAngle = GraphDefaults.EdgeArrowAngle;

    var p1 = new AvaloniaPoint(
        tip.X - size * Math.Cos(angle - arrowAngle),
        tip.Y - size * Math.Sin(angle - arrowAngle));

    var p2 = new AvaloniaPoint(
        tip.X - size * Math.Cos(angle + arrowAngle),
        tip.Y - size * Math.Sin(angle + arrowAngle));

    return (p1, p2);
  }
}
