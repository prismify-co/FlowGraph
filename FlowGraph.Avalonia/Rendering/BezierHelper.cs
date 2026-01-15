using Avalonia;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;
using CorePoint = FlowGraph.Core.Point;

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
    /// Creates a path geometry through waypoints based on the edge type.
    /// </summary>
    public static PathGeometry CreatePathWithWaypoints(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<CorePoint>? waypoints,
        EdgeType edgeType)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return CreatePath(start, end, edgeType);
        }

        // Convert waypoints to Avalonia points
        var allPoints = new List<AvaloniaPoint> { start };
        allPoints.AddRange(waypoints.Select(p => new AvaloniaPoint(p.X, p.Y)));
        allPoints.Add(end);

        return edgeType switch
        {
            EdgeType.Straight => CreateMultiSegmentStraightPath(allPoints),
            EdgeType.Step => CreateMultiSegmentStepPath(allPoints),
            EdgeType.SmoothStep => CreateMultiSegmentSmoothPath(allPoints),
            _ => CreateMultiSegmentBezierPath(allPoints)
        };
    }

    /// <summary>
    /// Creates a multi-segment straight line path through waypoints.
    /// </summary>
    public static PathGeometry CreateMultiSegmentStraightPath(IReadOnlyList<AvaloniaPoint> points)
    {
        if (points.Count < 2)
            return new PathGeometry();

        var pathFigure = new PathFigure
        {
            StartPoint = points[0],
            IsClosed = false,
            Segments = new PathSegments()
        };

        for (int i = 1; i < points.Count; i++)
        {
            pathFigure.Segments.Add(new LineSegment { Point = points[i] });
        }

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures!.Add(pathFigure);
        return pathGeometry;
    }

    /// <summary>
    /// Creates a multi-segment bezier path through waypoints.
    /// Uses smooth curves between consecutive waypoints.
    /// </summary>
    public static PathGeometry CreateMultiSegmentBezierPath(IReadOnlyList<AvaloniaPoint> points)
    {
        if (points.Count < 2)
            return new PathGeometry();

        if (points.Count == 2)
            return CreateBezierPath(points[0], points[1]);

        var pathFigure = new PathFigure
        {
            StartPoint = points[0],
            IsClosed = false,
            Segments = new PathSegments()
        };

        // Create smooth bezier segments through all points
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p3 = points[i + 1];

            // Calculate control points for smooth curve
            var controlOffset = Math.Abs(p3.X - p0.X) / 3;
            controlOffset = Math.Max(controlOffset, 20);

            AvaloniaPoint p1, p2;

            // First segment: exit horizontally from port
            if (i == 0)
            {
                p1 = new AvaloniaPoint(p0.X + controlOffset, p0.Y);
                p2 = new AvaloniaPoint(p3.X - controlOffset / 2, p3.Y);
            }
            // Last segment: enter horizontally to port
            else if (i == points.Count - 2)
            {
                p1 = new AvaloniaPoint(p0.X + controlOffset / 2, p0.Y);
                p2 = new AvaloniaPoint(p3.X - controlOffset, p3.Y);
            }
            // Middle segments: smooth transitions
            else
            {
                var dx = p3.X - p0.X;
                var dy = p3.Y - p0.Y;
                p1 = new AvaloniaPoint(p0.X + dx / 3, p0.Y + dy / 3);
                p2 = new AvaloniaPoint(p0.X + dx * 2 / 3, p0.Y + dy * 2 / 3);
            }

            pathFigure.Segments.Add(new BezierSegment
            {
                Point1 = p1,
                Point2 = p2,
                Point3 = p3
            });
        }

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures!.Add(pathFigure);
        return pathGeometry;
    }

    /// <summary>
    /// Creates a multi-segment step path through waypoints.
    /// </summary>
    public static PathGeometry CreateMultiSegmentStepPath(IReadOnlyList<AvaloniaPoint> points)
    {
        if (points.Count < 2)
            return new PathGeometry();

        var pathFigure = new PathFigure
        {
            StartPoint = points[0],
            IsClosed = false,
            Segments = new PathSegments()
        };

        for (int i = 0; i < points.Count - 1; i++)
        {
            var start = points[i];
            var end = points[i + 1];
            var midX = (start.X + end.X) / 2;

            // First point - exit horizontally
            if (i == 0)
            {
                pathFigure.Segments.Add(new LineSegment { Point = new AvaloniaPoint(midX, start.Y) });
            }

            pathFigure.Segments.Add(new LineSegment { Point = new AvaloniaPoint(midX, end.Y) });
            pathFigure.Segments.Add(new LineSegment { Point = end });
        }

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures!.Add(pathFigure);
        return pathGeometry;
    }

    /// <summary>
    /// Creates a multi-segment smooth step path with rounded corners through waypoints.
    /// </summary>
    public static PathGeometry CreateMultiSegmentSmoothPath(IReadOnlyList<AvaloniaPoint> points, double cornerRadius = DefaultCornerRadius)
    {
        if (points.Count < 2)
            return new PathGeometry();

        // For simple paths, use the original implementation
        if (points.Count == 2)
            return CreateSmoothStepPath(points[0], points[1], cornerRadius);

        var pathFigure = new PathFigure
        {
            StartPoint = points[0],
            IsClosed = false,
            Segments = new PathSegments()
        };

        // Build path with rounded corners at each waypoint
        var current = points[0];

        for (int i = 1; i < points.Count; i++)
        {
            var next = points[i];
            var isLast = i == points.Count - 1;

            if (isLast)
            {
                // Simple line to final point
                pathFigure.Segments.Add(new LineSegment { Point = next });
            }
            else
            {
                var afterNext = points[i + 1];

                // Calculate corner based on direction change
                var radius = Math.Min(cornerRadius,
                    Math.Min(Distance(current, next) / 2, Distance(next, afterNext) / 2));

                if (radius > 1)
                {
                    // Line to just before the corner
                    var beforeCorner = MoveTowards(next, current, radius);
                    pathFigure.Segments.Add(new LineSegment { Point = beforeCorner });

                    // Arc around the corner
                    var afterCorner = MoveTowards(next, afterNext, radius);
                    var sweepDir = GetSweepDirection(current, next, afterNext);

                    pathFigure.Segments.Add(new ArcSegment
                    {
                        Point = afterCorner,
                        Size = new Size(radius, radius),
                        SweepDirection = sweepDir,
                        IsLargeArc = false
                    });

                    current = afterCorner;
                }
                else
                {
                    pathFigure.Segments.Add(new LineSegment { Point = next });
                    current = next;
                }
            }
        }

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures!.Add(pathFigure);
        return pathGeometry;
    }

    private static double Distance(AvaloniaPoint a, AvaloniaPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static AvaloniaPoint MoveTowards(AvaloniaPoint from, AvaloniaPoint to, double distance)
    {
        var totalDist = Distance(from, to);
        if (totalDist < 0.0001) return from;

        var ratio = distance / totalDist;
        return new AvaloniaPoint(
            from.X + (to.X - from.X) * ratio,
            from.Y + (to.Y - from.Y) * ratio);
    }

    private static SweepDirection GetSweepDirection(AvaloniaPoint prev, AvaloniaPoint current, AvaloniaPoint next)
    {
        // Cross product to determine turn direction
        var cross = (current.X - prev.X) * (next.Y - current.Y) -
                   (current.Y - prev.Y) * (next.X - current.X);
        return cross > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
    }

    /// <summary>
    /// Creates a bezier path geometry between two points.
    /// </summary>
    public static PathGeometry CreateBezierPath(AvaloniaPoint start, AvaloniaPoint end, bool horizontalBias = true)
    {
        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments()
        };

        var controlPointOffset = Math.Abs(end.X - start.X) / 2;
        controlPointOffset = Math.Max(controlPointOffset, GraphDefaults.MinBezierControlOffset);

        // Calculate direction-aware control points
        var (control1, control2) = CalculateDirectionAwareControlPoints(start, end, controlPointOffset);

        var bezierSegment = new BezierSegment
        {
            Point1 = control1,
            Point2 = control2,
            Point3 = end
        };

        pathFigure.Segments.Add(bezierSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures!.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Calculates bezier control points based on edge direction.
    /// For left-to-right edges, uses horizontal control points.
    /// For vertical/backward edges, uses more appropriate control point placement.
    /// </summary>
    private static (AvaloniaPoint c1, AvaloniaPoint c2) CalculateDirectionAwareControlPoints(
        AvaloniaPoint start, AvaloniaPoint end, double offset)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        // If edge goes mostly left-to-right, use horizontal control points
        if (dx > Math.Abs(dy) * 0.5)
        {
            // Standard left-to-right bezier
            return (
                new AvaloniaPoint(start.X + offset, start.Y),
                new AvaloniaPoint(end.X - offset, end.Y)
            );
        }

        // If edge goes mostly right-to-left (backwards), curve around
        if (dx < -Math.Abs(dy) * 0.5)
        {
            // Both control points go outward to create a looping curve
            return (
                new AvaloniaPoint(start.X + offset, start.Y),
                new AvaloniaPoint(end.X - offset, end.Y)
            );
        }

        // For mostly vertical edges
        // Check if it's a pure vertical edge (same X coordinate)
        if (Math.Abs(dx) < 10)
        {
            // Pure vertical - keep control points on the same X line
            var verticalDist = Math.Abs(dy) / 3;
            return (
                new AvaloniaPoint(start.X, start.Y + (dy > 0 ? verticalDist : -verticalDist)),
                new AvaloniaPoint(end.X, end.Y - (dy > 0 ? verticalDist : -verticalDist))
            );
        }

        if (dx >= 0)
        {
            // Going down-right - slight horizontal offset
            var hOffset = Math.Min(offset * 0.3, Math.Abs(dx) * 0.5);
            return (
                new AvaloniaPoint(start.X + hOffset, start.Y + offset * 0.4),
                new AvaloniaPoint(end.X - hOffset, end.Y - offset * 0.4)
            );
        }
        else
        {
            // Going down-left - create a smooth arc that doesn't swing too wide
            return (
                new AvaloniaPoint(start.X + offset * 0.3, start.Y + Math.Abs(dy) * 0.3),
                new AvaloniaPoint(end.X - offset * 0.3, end.Y - Math.Abs(dy) * 0.3)
            );
        }
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
        pathGeometry.Figures!.Add(pathFigure);

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

        var midX = (start.X + end.X) / 2;

        // Create step path: start -> mid horizontal -> vertical -> end
        pathFigure.Segments.Add(new LineSegment { Point = new AvaloniaPoint(midX, start.Y) });
        pathFigure.Segments.Add(new LineSegment { Point = new AvaloniaPoint(midX, end.Y) });
        pathFigure.Segments.Add(new LineSegment { Point = end });

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures!.Add(pathFigure);

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
        pathGeometry.Figures!.Add(pathFigure);

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
        // Use shared helper for arrow point calculation
        var (p1, p2) = ArrowGeometryHelper.CalculateArrowPoints(point, angle, size);

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
        pathGeometry.Figures!.Add(pathFigure);

        return pathGeometry;
    }

    /// <summary>
    /// Calculates the angle of a line at a given point (for arrow direction).
    /// </summary>
    public static double CalculateAngle(AvaloniaPoint from, AvaloniaPoint to)
    {
        return Math.Atan2(to.Y - from.Y, to.X - from.X);
    }

    /// <summary>
    /// Calculates the arrow angle using a hybrid approach that combines port position (logical direction)
    /// with path geometry (visual direction) for consistent, accurate arrow rendering.
    /// </summary>
    /// <param name="tip">The arrow tip point (target position).</param>
    /// <param name="fromPoint">The point the edge comes from (preceding the tip).</param>
    /// <param name="targetPortPosition">Optional port position for logical direction hint.</param>
    /// <param name="angleThreshold">Angle tolerance in radians for preferring port direction (default: π/4 = 45°).</param>
    /// <returns>The angle in radians for the arrow direction.</returns>
    /// <remarks>
    /// <para>
    /// This method implements a hybrid approach:
    /// 1. If port position is provided, get the expected angle from port side
    /// 2. Calculate the actual segment angle from path geometry
    /// 3. If segment angle is within threshold of expected, use port direction (cleaner)
    /// 4. Otherwise, use actual segment direction (handles unusual routing)
    /// </para>
    /// <para>
    /// This ensures consistency between custom renderers and default rendering while
    /// handling edge cases like curved paths or unusual node arrangements.
    /// </para>
    /// </remarks>
    public static double CalculateArrowAngle(
        AvaloniaPoint tip,
        AvaloniaPoint fromPoint,
        PortPosition? targetPortPosition = null,
        double angleThreshold = Math.PI / 4)
    {
        // Calculate the actual segment angle from path geometry
        var segmentAngle = CalculateAngle(fromPoint, tip);

        // If no port position hint, just use segment angle
        if (targetPortPosition == null)
            return segmentAngle;

        // Get the expected angle from port position
        var expectedAngle = targetPortPosition.Value.ToIncomingArrowAngle();

        // Compare angles: if segment is close to expected, prefer the cleaner port direction
        var angleDiff = NormalizeAngle(segmentAngle - expectedAngle);
        return Math.Abs(angleDiff) <= angleThreshold ? expectedAngle : segmentAngle;
    }

    /// <summary>
    /// Normalizes an angle to the range [-π, π].
    /// </summary>
    private static double NormalizeAngle(double angle)
    {
        while (angle > Math.PI) angle -= 2 * Math.PI;
        while (angle < -Math.PI) angle += 2 * Math.PI;
        return angle;
    }
}

// Keep the old name as an alias for backward compatibility
public static class BezierHelper
{
    public static PathGeometry CreateBezierPath(AvaloniaPoint start, AvaloniaPoint end, bool horizontalBias = true)
        => EdgePathHelper.CreateBezierPath(start, end, horizontalBias);
}
