using Avalonia;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates viewport transitions (pan and zoom).
/// </summary>
public class ViewportAnimation : IAnimation, ICategorizedAnimation
{
    private readonly ViewportState _viewport;
    private readonly double _startZoom;
    private readonly double _endZoom;
    private readonly double _startOffsetX;
    private readonly double _startOffsetY;
    private readonly double _endOffsetX;
    private readonly double _endOffsetY;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }
    
    public AnimationCategory Category => AnimationCategory.Viewport;
    
    public IReadOnlyCollection<string> AffectedNodeIds => Array.Empty<string>();

    /// <summary>
    /// Creates a new viewport animation.
    /// </summary>
    /// <param name="viewport">The viewport to animate.</param>
    /// <param name="targetZoom">Target zoom level.</param>
    /// <param name="targetOffsetX">Target X offset.</param>
    /// <param name="targetOffsetY">Target Y offset.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function (defaults to EaseOutCubic).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public ViewportAnimation(
        ViewportState viewport,
        double targetZoom,
        double targetOffsetX,
        double targetOffsetY,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        _viewport = viewport;
        _startZoom = viewport.Zoom;
        _endZoom = targetZoom;
        _startOffsetX = viewport.OffsetX;
        _startOffsetY = viewport.OffsetY;
        _endOffsetX = targetOffsetX;
        _endOffsetY = targetOffsetY;
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onComplete = onComplete;
    }

    /// <summary>
    /// Creates a viewport animation to center on a canvas point.
    /// </summary>
    public static ViewportAnimation CenterOn(
        ViewportState viewport,
        Point canvasPoint,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        var targetOffsetX = viewport.ViewSize.Width / 2 - canvasPoint.X * viewport.Zoom;
        var targetOffsetY = viewport.ViewSize.Height / 2 - canvasPoint.Y * viewport.Zoom;

        return new ViewportAnimation(
            viewport,
            viewport.Zoom,
            targetOffsetX,
            targetOffsetY,
            duration,
            easing,
            onComplete);
    }

    /// <summary>
    /// Creates a viewport animation to fit bounds in view.
    /// </summary>
    /// <param name="viewport">The viewport to animate.</param>
    /// <param name="bounds">The bounds to fit.</param>
    /// <param name="padding">Padding around the bounds.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function.</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    /// <param name="minZoom">Minimum zoom level (default: 0.1).</param>
    /// <param name="maxZoom">Maximum zoom level (default: 2.0).</param>
    public static ViewportAnimation FitToBounds(
        ViewportState viewport,
        Rect bounds,
        double padding = 50,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action? onComplete = null,
        double minZoom = 0.1,
        double maxZoom = 2.0)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 ||
            viewport.ViewSize.Width <= 0 || viewport.ViewSize.Height <= 0)
        {
            return new ViewportAnimation(viewport, viewport.Zoom, viewport.OffsetX, viewport.OffsetY, 0);
        }

        var zoomX = (viewport.ViewSize.Width - padding * 2) / bounds.Width;
        var zoomY = (viewport.ViewSize.Height - padding * 2) / bounds.Height;
        var targetZoom = Math.Clamp(Math.Min(zoomX, zoomY), minZoom, maxZoom);

        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var targetOffsetX = viewport.ViewSize.Width / 2 - centerX * targetZoom;
        var targetOffsetY = viewport.ViewSize.Height / 2 - centerY * targetZoom;

        return new ViewportAnimation(
            viewport,
            targetZoom,
            targetOffsetX,
            targetOffsetY,
            duration,
            easing,
            onComplete);
    }

    /// <summary>
    /// Creates a viewport animation to zoom to a level at a specific screen point.
    /// </summary>
    /// <param name="viewport">The viewport to animate.</param>
    /// <param name="targetZoom">Target zoom level (will be clamped by viewport's SetZoomDirect).</param>
    /// <param name="zoomCenter">Screen point to zoom towards.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function.</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public static ViewportAnimation ZoomTo(
        ViewportState viewport,
        double targetZoom,
        Point? zoomCenter = null,
        double duration = 0.2,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        // Note: targetZoom will be clamped by SetZoomDirect during animation
        var center = zoomCenter ?? new Point(viewport.ViewSize.Width / 2, viewport.ViewSize.Height / 2);
        var zoomFactor = targetZoom / viewport.Zoom;
        var targetOffsetX = center.X - (center.X - viewport.OffsetX) * zoomFactor;
        var targetOffsetY = center.Y - (center.Y - viewport.OffsetY) * zoomFactor;

        return new ViewportAnimation(
            viewport,
            targetZoom,
            targetOffsetX,
            targetOffsetY,
            duration,
            easing,
            onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        // Interpolate values
        var currentZoom = Lerp(_startZoom, _endZoom, easedT);
        var currentOffsetX = Lerp(_startOffsetX, _endOffsetX, easedT);
        var currentOffsetY = Lerp(_startOffsetY, _endOffsetY, easedT);

        // Apply to viewport without triggering recursive events
        // SetZoomDirect clamps to min/max zoom from settings
        _viewport.SetZoomDirect(currentZoom);
        _viewport.SetOffsetDirect(currentOffsetX, currentOffsetY);
        _viewport.NotifyViewportChanged();

        if (t >= 1)
        {
            IsComplete = true;
            _onComplete?.Invoke();
        }
    }

    public void Cancel()
    {
        IsComplete = true;
    }

    private static double Lerp(double start, double end, double t)
    {
        return start + (end - start) * t;
    }
}
