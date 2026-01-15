using Avalonia;
using FlowGraph.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// DirectGraphRenderer partial - Helper methods and structs for hit testing and rendering.
/// </summary>
public partial class DirectGraphRenderer
{
    /// <summary>
    /// Captures viewport state for hit testing operations.
    /// Using a readonly struct avoids allocations and allows stack-only usage.
    /// </summary>
    private readonly struct HitTestContext
    {
        public readonly Rect ViewBounds;
        public readonly double Zoom;
        public readonly double OffsetX;
        public readonly double OffsetY;
        public readonly AvaloniaPoint CanvasPoint;

        public HitTestContext(Rect viewBounds, ViewportState viewport, AvaloniaPoint canvasPoint)
        {
            ViewBounds = viewBounds;
            Zoom = viewport.Zoom;
            OffsetX = viewport.OffsetX;
            OffsetY = viewport.OffsetY;
            CanvasPoint = canvasPoint;
        }

        /// <summary>
        /// Converts canvas bounds to screen bounds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rect CanvasToScreen(Rect canvasBounds)
        {
            return new Rect(
                canvasBounds.X * Zoom + OffsetX,
                canvasBounds.Y * Zoom + OffsetY,
                canvasBounds.Width * Zoom,
                canvasBounds.Height * Zoom);
        }

        /// <summary>
        /// Converts canvas point to screen point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AvaloniaPoint CanvasToScreen(AvaloniaPoint canvasPoint)
        {
            return new AvaloniaPoint(
                canvasPoint.X * Zoom + OffsetX,
                canvasPoint.Y * Zoom + OffsetY);
        }

        /// <summary>
        /// Checks if screen bounds are within visible viewport (with optional buffer).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsScreenBoundsVisible(Rect screenBounds, double buffer = 0)
        {
            return screenBounds.X + screenBounds.Width + buffer >= 0 &&
                   screenBounds.X - buffer <= ViewBounds.Width &&
                   screenBounds.Y + screenBounds.Height + buffer >= 0 &&
                   screenBounds.Y - buffer <= ViewBounds.Height;
        }

        /// <summary>
        /// Checks if a node from the spatial index is visible in the viewport.
        /// Uses pre-cached node bounds to avoid repeated calculations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNodeIndexEntryVisible(double nx, double ny, double nw, double nh, double buffer = 0)
        {
            var screenX = nx * Zoom + OffsetX;
            var screenY = ny * Zoom + OffsetY;
            var screenW = nw * Zoom;
            var screenH = nh * Zoom;

            return screenX + screenW + buffer >= 0 &&
                   screenX - buffer <= ViewBounds.Width &&
                   screenY + screenH + buffer >= 0 &&
                   screenY - buffer <= ViewBounds.Height;
        }

        /// <summary>
        /// Gets screen bounds for a node from the spatial index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (double screenX, double screenY, double screenW, double screenH) GetScreenBounds(
            double nx, double ny, double nw, double nh)
        {
            return (
                nx * Zoom + OffsetX,
                ny * Zoom + OffsetY,
                nw * Zoom,
                nh * Zoom);
        }
    }

    /// <summary>
    /// Creates a hit test context from current state.
    /// Returns null if viewport is not available.
    /// </summary>
    private HitTestContext? CreateHitTestContext(double screenX, double screenY)
    {
        if (_viewport == null) return null;

        var viewBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var canvasPoint = ScreenToCanvas(screenX, screenY);
        return new HitTestContext(viewBounds, _viewport, canvasPoint);
    }

    /// <summary>
    /// Checks if a point is within a circular radius (using squared distance to avoid sqrt).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPointInCircle(AvaloniaPoint point, AvaloniaPoint center, double radius)
    {
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        return dx * dx + dy * dy <= radius * radius;
    }

    /// <summary>
    /// Checks if a point is within a circular radius (using squared distance to avoid sqrt).
    /// Overload for double coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPointInCircle(double pointX, double pointY, double centerX, double centerY, double radius)
    {
        var dx = pointX - centerX;
        var dy = pointY - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }

    /// <summary>
    /// Helper for timed debug logging during hit testing.
    /// Only logs in DEBUG builds.
    /// </summary>
    [Conditional("DEBUG")]
    private static void LogHitTestTiming(string methodName, Stopwatch sw, string details)
    {
        sw.Stop();
        Debug.WriteLine($"[{methodName}] {details} in {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Helper for timed debug logging with no-hit result.
    /// </summary>
    [Conditional("DEBUG")]
    private static void LogHitTestNoHit(string methodName, Stopwatch sw, int checked_count, int skipped_count)
    {
        sw.Stop();
        Debug.WriteLine($"[{methodName}] No hit in {sw.ElapsedMilliseconds}ms | Checked:{checked_count}, Skipped:{skipped_count}");
    }
}
