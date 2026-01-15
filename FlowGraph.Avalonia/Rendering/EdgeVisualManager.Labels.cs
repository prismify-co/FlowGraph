// EdgeVisualManager.Labels.cs
// Partial class containing edge label rendering and positioning logic

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

public partial class EdgeVisualManager
{
    /// <summary>
    /// Renders a label on an edge with support for anchor positioning and offsets.
    /// Automatically positions labels based on edge direction to avoid overlap with the edge line.
    /// Supports perpendicular offsets that rotate with the edge direction (like GoJS segmentOffset).
    /// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
    /// </summary>
    private TextBlock? RenderEdgeLabel(Canvas canvas, AvaloniaPoint start, AvaloniaPoint end, IReadOnlyList<Core.Point>? waypoints, Edge edge, ThemeResources theme)
    {
        var labelInfo = edge.Definition.LabelInfo;
        var labelText = labelInfo?.Text ?? edge.Label;

        if (string.IsNullOrEmpty(labelText))
            return null;

        // Calculate position based on anchor
        double t = 0.5; // Default to center
        if (labelInfo != null)
        {
            t = labelInfo.Anchor switch
            {
                LabelAnchor.Start => 0.25,
                LabelAnchor.End => 0.75,
                _ => 0.5
            };
        }

        // Calculate position along the actual path (including waypoints) and get edge angle
        // Use 1.0 for scale since we're in transform-based rendering
        var (posX, posY, edgeDirection, edgeAngle) = CalculateLabelPositionOnPathWithDirectionAndAngle(start, end, waypoints, t, 1.0);

        // Create the text block first - use logical (unscaled) dimensions
        var textBlock = new TextBlock
        {
            Text = labelText,
            FontSize = 12,
            Foreground = theme.NodeText,
            Background = theme.NodeBackground,
            Padding = new Thickness(4, 2, 4, 2),
            Tag = edge,  // Store edge reference for event handling
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // Get user-specified offsets if any
        var userOffsetX = labelInfo?.OffsetX ?? 0;
        var userOffsetY = labelInfo?.OffsetY ?? 0;
        var perpOffset = labelInfo?.PerpendicularOffset ?? 0;
        var hasUserOffset = labelInfo != null && (labelInfo.OffsetX != 0 || labelInfo.OffsetY != 0);
        var hasPerpOffset = labelInfo != null && labelInfo.PerpendicularOffset != 0;

        // Calculate smart offset based on edge direction (only if no user override and no perpendicular offset)
        double autoOffsetX = 0;
        double autoOffsetY = 0;

        if (!hasUserOffset && !hasPerpOffset)
        {
            // Position label based on edge direction to avoid overlap
            switch (edgeDirection)
            {
                case EdgeDirection.Horizontal:
                    // Horizontal edge: place label above
                    autoOffsetY = -16;
                    break;

                case EdgeDirection.Vertical:
                    // Vertical edge: place label to the right
                    autoOffsetX = 8;
                    autoOffsetY = -8;  // Slight vertical offset for centering
                    break;

                case EdgeDirection.DiagonalDownRight:
                case EdgeDirection.DiagonalUpRight:
                    // Diagonal going right: place label above-right
                    autoOffsetX = 4;
                    autoOffsetY = -16;
                    break;

                case EdgeDirection.DiagonalDownLeft:
                case EdgeDirection.DiagonalUpLeft:
                    // Diagonal going left: place label above-left
                    autoOffsetX = -4;
                    autoOffsetY = -16;
                    break;
            }
        }

        // Calculate perpendicular offset (rotates with edge direction)
        // Perpendicular to edge means: if edge angle is θ, perpendicular is θ + 90°
        // Positive perpOffset moves to the "right" of the edge direction
        double perpOffsetX = 0;
        double perpOffsetY = 0;
        if (hasPerpOffset)
        {
            // Calculate perpendicular direction (90° clockwise from edge direction)
            // Edge direction vector: (cos(θ), sin(θ))
            // Perpendicular (right side): (sin(θ), -cos(θ)) but in screen coords Y is inverted
            // So right side is: (-sin(θ), cos(θ)) in math coords = (sin(θ), cos(θ)) in screen
            perpOffsetX = Math.Sin(edgeAngle) * perpOffset;
            perpOffsetY = -Math.Cos(edgeAngle) * perpOffset;  // Negative because screen Y is inverted
        }

        // Use logical dimensions - MatrixTransform handles zoom
        var finalOffsetX = userOffsetX + autoOffsetX + perpOffsetX;
        var finalOffsetY = userOffsetY + autoOffsetY + perpOffsetY;

        Canvas.SetLeft(textBlock, posX + finalOffsetX);
        Canvas.SetTop(textBlock, posY + finalOffsetY);

        canvas.Children.Add(textBlock);
        return textBlock;
    }

    /// <summary>
    /// Edge direction categories for smart label placement.
    /// </summary>
    private enum EdgeDirection
    {
        Horizontal,          // Mostly left-right
        Vertical,            // Mostly up-down
        DiagonalDownRight,   // Going down and right
        DiagonalDownLeft,    // Going down and left
        DiagonalUpRight,     // Going up and right
        DiagonalUpLeft       // Going up and left
    }

    /// <summary>
    /// Calculates the label position along the actual edge path, including waypoints,
    /// and returns the direction of the edge segment at that point along with the angle.
    /// </summary>
    private static (double X, double Y, EdgeDirection Direction, double Angle) CalculateLabelPositionOnPathWithDirectionAndAngle(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<Core.Point>? waypoints,
        double t,
        double scale)
    {
        // If no waypoints, use simple interpolation between start and end
        if (waypoints == null || waypoints.Count == 0)
        {
            var direction = DetermineEdgeDirection(start.X, start.Y, end.X, end.Y);
            var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            return (
                start.X + (end.X - start.X) * t,
                start.Y + (end.Y - start.Y) * t,
                direction,
                angle
            );
        }

        // Build the complete path: start -> waypoints (already in screen coords) -> end
        var allPoints = new List<AvaloniaPoint> { start };
        foreach (var wp in waypoints)
        {
            allPoints.Add(new AvaloniaPoint(wp.X, wp.Y));
        }
        allPoints.Add(end);

        // Calculate total path length
        double totalLength = 0;
        var segmentLengths = new List<double>();
        for (int i = 0; i < allPoints.Count - 1; i++)
        {
            var dx = allPoints[i + 1].X - allPoints[i].X;
            var dy = allPoints[i + 1].Y - allPoints[i].Y;
            var segmentLength = Math.Sqrt(dx * dx + dy * dy);
            segmentLengths.Add(segmentLength);
            totalLength += segmentLength;
        }

        // Find the position at t along the total path
        var targetDistance = totalLength * t;
        double accumulatedDistance = 0;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            if (accumulatedDistance + segmentLengths[i] >= targetDistance)
            {
                // The point is on this segment
                var segmentT = (targetDistance - accumulatedDistance) / segmentLengths[i];
                var p1 = allPoints[i];
                var p2 = allPoints[i + 1];
                var direction = DetermineEdgeDirection(p1.X, p1.Y, p2.X, p2.Y);
                var angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                return (
                    p1.X + (p2.X - p1.X) * segmentT,
                    p1.Y + (p2.Y - p1.Y) * segmentT,
                    direction,
                    angle
                );
            }
            accumulatedDistance += segmentLengths[i];
        }

        // Fallback to end point
        var fallbackDirection = DetermineEdgeDirection(start.X, start.Y, end.X, end.Y);
        var fallbackAngle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        return (end.X, end.Y, fallbackDirection, fallbackAngle);
    }

    /// <summary>
    /// Calculates the label position along the actual edge path, including waypoints,
    /// and returns the direction of the edge segment at that point.
    /// </summary>
    private static (double X, double Y, EdgeDirection Direction) CalculateLabelPositionOnPathWithDirection(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<Core.Point>? waypoints,
        double t,
        double scale)
    {
        var (x, y, direction, _) = CalculateLabelPositionOnPathWithDirectionAndAngle(start, end, waypoints, t, scale);
        return (x, y, direction);
    }

    /// <summary>
    /// Determines the direction category of an edge segment.
    /// </summary>
    private static EdgeDirection DetermineEdgeDirection(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var absDx = Math.Abs(dx);
        var absDy = Math.Abs(dy);

        // If mostly horizontal (|dx| > 2 * |dy|)
        if (absDx > absDy * 2)
            return EdgeDirection.Horizontal;

        // If mostly vertical (|dy| > 2 * |dx|)
        if (absDy > absDx * 2)
            return EdgeDirection.Vertical;

        // Diagonal - determine quadrant
        if (dx > 0 && dy > 0)
            return EdgeDirection.DiagonalDownRight;
        if (dx > 0 && dy < 0)
            return EdgeDirection.DiagonalUpRight;
        if (dx < 0 && dy > 0)
            return EdgeDirection.DiagonalDownLeft;

        return EdgeDirection.DiagonalUpLeft;
    }

    /// <summary>
    /// Calculates the label position along the actual edge path, including waypoints.
    /// </summary>
    private static (double X, double Y) CalculateLabelPositionOnPath(
        AvaloniaPoint start,
        AvaloniaPoint end,
        IReadOnlyList<Core.Point>? waypoints,
        double t,
        double scale)
    {
        var (x, y, _) = CalculateLabelPositionOnPathWithDirection(start, end, waypoints, t, scale);
        return (x, y);
    }
}
