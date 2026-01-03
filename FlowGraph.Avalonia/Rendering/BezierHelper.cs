using Avalonia;
using Avalonia.Media;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Helper class for creating bezier curve paths between points.
/// </summary>
public static class BezierHelper
{
    /// <summary>
    /// Creates a bezier path geometry between two points.
    /// </summary>
    /// <param name="start">Start point of the curve.</param>
    /// <param name="end">End point of the curve.</param>
    /// <param name="horizontalBias">If true, curves horizontally first; if false, curves based on direction.</param>
    /// <returns>A PathGeometry representing the bezier curve.</returns>
    public static PathGeometry CreateBezierPath(Point start, Point end, bool horizontalBias = true)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false
        };

        var controlPointOffset = Math.Abs(end.X - start.X) / 2;
        
        // Ensure minimum offset for visual appeal
        controlPointOffset = Math.Max(controlPointOffset, 50);

        Point control1, control2;
        
        if (horizontalBias)
        {
            control1 = new Point(start.X + controlPointOffset, start.Y);
            control2 = new Point(end.X - controlPointOffset, end.Y);
        }
        else
        {
            // Adjust control points based on relative position
            var goingRight = end.X > start.X;
            control1 = new Point(
                goingRight ? start.X + controlPointOffset : start.X - controlPointOffset,
                start.Y);
            control2 = new Point(
                goingRight ? end.X - controlPointOffset : end.X + controlPointOffset,
                end.Y);
        }

        var bezierSegment = new BezierSegment
        {
            Point1 = control1,
            Point2 = control2,
            Point3 = end
        };

        pathFigure.Segments!.Add(bezierSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }
}
