using Avalonia;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Helper class for creating edge path geometries between points.
/// </summary>
public static class EdgePathHelper
{
    /// <summary>
    /// Default corner radius for smooth step edges.
    /// </summary>
    private const double DefaultCornerRadius = 10;
    
    /// <summary>
    /// Creates a path geometry between two points based on the edge type.
    /// </summary>
    public static PathGeometry CreatePath(AvaloniaPoint start, AvaloniaPoint end, EdgeType edgeType)
    {
        return edgeType switch
        {
            EdgeType.Straight => CreateStraightPath(start, end),
            EdgeType.Step => CreateStepPath(start, end),
            EdgeType.SmoothStep => CreateSmoothStepPath(start, end),
            _ => CreateBezierPath(start, end)
        };
    }

    /// <summary>
    /// Creates a bezier path geometry between two points.
    /// </summary>
    /// <param name="start">Start point of the curve.</param>
    /// <param name="end">End point of the curve.</param>
    /// <param name="horizontalBias">If true, curves horizontally first; if false, curves based on direction.</param>
    /// <returns>A PathGeometry representing the bezier curve.</returns>
    public static PathGeometry CreateBezierPath(AvaloniaPoint start, AvaloniaPoint end, bool horizontalBias = true)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments()
        };

        var controlPointOffset = Math.Abs(end.X - start.X) / 2;
        
        // Ensure minimum offset for visual appeal
        controlPointOffset = Math.Max(controlPointOffset, 50);

        AvaloniaPoint control1, control2;
        
        if (horizontalBias)
        {
            control1 = new AvaloniaPoint(start.X + controlPointOffset, start.Y);
            control2 = new AvaloniaPoint(end.X - controlPointOffset, end.Y);
        }
        else
        {
            // Adjust control points based on relative position
            var goingRight = end.X > start.X;
            control1 = new AvaloniaPoint(
                goingRight ? start.X + controlPointOffset : start.X - controlPointOffset,
                start.Y);
            control2 = new AvaloniaPoint(
                goingRight ? end.X - controlPointOffset : end.X + controlPointOffset,
                end.Y);
        }

        var bezierSegment = new BezierSegment
        {
            Point1 = control1,
            Point2 = control2,
            Point3 = end
        };

        pathFigure.Segments.Add(bezierSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Creates a straight line path between two points.
    /// </summary>
    public static PathGeometry CreateStraightPath(AvaloniaPoint start, AvaloniaPoint end)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments
            {
                new LineSegment { Point = end }
            }
        };

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Creates a step (right-angle) path between two points.
    /// The path goes horizontally from start, then vertically, then horizontally to end.
    /// </summary>
    public static PathGeometry CreateStepPath(AvaloniaPoint start, AvaloniaPoint end)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments()
        };

        // Calculate midpoint X
        var midX = (start.X + end.X) / 2;

        // Create step path: start -> mid horizontal -> vertical -> end
        pathFigure.Segments.Add(new LineSegment { Point = new AvaloniaPoint(midX, start.Y) });
        pathFigure.Segments.Add(new LineSegment { Point = new AvaloniaPoint(midX, end.Y) });
        pathFigure.Segments.Add(new LineSegment { Point = end });

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Creates a smooth step path with rounded corners between two points.
    /// </summary>
    public static PathGeometry CreateSmoothStepPath(AvaloniaPoint start, AvaloniaPoint end, double cornerRadius = DefaultCornerRadius)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments()
        };

        var midX = (start.X + end.X) / 2;
        var deltaY = end.Y - start.Y;
        
        // Clamp corner radius to not exceed half the available space
        var maxRadiusX = Math.Abs(midX - start.X);
        var maxRadiusY = Math.Abs(deltaY) / 2;
        var radius = Math.Min(cornerRadius, Math.Min(maxRadiusX, maxRadiusY));

        if (radius < 1 || Math.Abs(deltaY) < 1)
        {
            // Fall back to step path if not enough room for curves
            return CreateStepPath(start, end);
        }

        var goingDown = deltaY > 0;
        var sweepDir = goingDown ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
        var oppositeSweep = goingDown ? SweepDirection.CounterClockwise : SweepDirection.Clockwise;

        // First horizontal segment (leaving start, before first corner)
        var corner1Start = new AvaloniaPoint(midX - radius, start.Y);
        pathFigure.Segments.Add(new LineSegment { Point = corner1Start });

        // First corner (top corner)
        var corner1End = new AvaloniaPoint(midX, start.Y + (goingDown ? radius : -radius));
        pathFigure.Segments.Add(new ArcSegment
        {
            Point = corner1End,
            Size = new Size(radius, radius),
            SweepDirection = sweepDir,
            IsLargeArc = false
        });

        // Vertical segment
        var corner2Start = new AvaloniaPoint(midX, end.Y - (goingDown ? radius : -radius));
        pathFigure.Segments.Add(new LineSegment { Point = corner2Start });

        // Second corner (bottom corner)
        var corner2End = new AvaloniaPoint(midX + radius, end.Y);
        pathFigure.Segments.Add(new ArcSegment
        {
            Point = corner2End,
            Size = new Size(radius, radius),
            SweepDirection = oppositeSweep,
            IsLargeArc = false
        });

        // Final horizontal segment to end
        pathFigure.Segments.Add(new LineSegment { Point = end });

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Creates an arrow marker geometry at the specified point.
    /// </summary>
    /// <param name="point">The tip of the arrow.</param>
    /// <param name="angle">The angle of the arrow in radians (0 = pointing right).</param>
    /// <param name="size">The size of the arrow.</param>
    /// <param name="closed">If true, creates a filled triangle; if false, creates open arrow lines.</param>
    public static PathGeometry CreateArrowMarker(AvaloniaPoint point, double angle, double size = 10, bool closed = false)
    {
        // Arrow points back from the tip
        var arrowAngle = Math.PI / 6; // 30 degrees spread
        
        var p1 = new AvaloniaPoint(
            point.X - size * Math.Cos(angle - arrowAngle),
            point.Y - size * Math.Sin(angle - arrowAngle)
        );
        
        var p2 = new AvaloniaPoint(
            point.X - size * Math.Cos(angle + arrowAngle),
            point.Y - size * Math.Sin(angle + arrowAngle)
        );

        var pathFigure = new PathFigure
        {
            StartPoint = p1,
            IsClosed = closed,
            IsFilled = closed,
            Segments = new PathSegments
            {
                new LineSegment { Point = point },
                new LineSegment { Point = p2 }
            }
        };

        if (closed)
        {
            pathFigure.Segments.Add(new LineSegment { Point = p1 });
        }

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Calculates the angle of a line at a given point (for arrow direction).
    /// </summary>
    public static double CalculateAngle(AvaloniaPoint from, AvaloniaPoint to)
    {
        return Math.Atan2(to.Y - from.Y, to.X - from.X);
    }
}

// Keep the old name as an alias for backward compatibility
public static class BezierHelper
{
    public static PathGeometry CreateBezierPath(AvaloniaPoint start, AvaloniaPoint end, bool horizontalBias = true)
        => EdgePathHelper.CreateBezierPath(start, end, horizontalBias);
}
