using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates an edge's opacity for fade in/out effects.
/// </summary>
public class EdgeFadeAnimation : IAnimation
{
    private readonly Edge _edge;
    private readonly double _startOpacity;
    private readonly double _endOpacity;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action<Edge, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current opacity value.
    /// </summary>
    public double CurrentOpacity { get; private set; }

    /// <summary>
    /// Creates a new edge fade animation.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="startOpacity">Starting opacity (0-1).</param>
    /// <param name="endOpacity">Ending opacity (0-1).</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function (defaults to EaseOutCubic).</param>
    /// <param name="onUpdate">Callback called each frame with the edge and current opacity.</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public EdgeFadeAnimation(
        Edge edge,
        double startOpacity,
        double endOpacity,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action<Edge, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _edge = edge;
        _startOpacity = Math.Clamp(startOpacity, 0, 1);
        _endOpacity = Math.Clamp(endOpacity, 0, 1);
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentOpacity = _startOpacity;
    }

    /// <summary>
    /// Creates a fade-in animation for an edge.
    /// </summary>
    public static EdgeFadeAnimation FadeIn(
        Edge edge,
        double duration = 0.3,
        Action<Edge, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new EdgeFadeAnimation(edge, 0, 1, duration, Easing.EaseOutCubic, onUpdate, onComplete);
    }

    /// <summary>
    /// Creates a fade-out animation for an edge.
    /// </summary>
    public static EdgeFadeAnimation FadeOut(
        Edge edge,
        double duration = 0.3,
        Action<Edge, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new EdgeFadeAnimation(edge, 1, 0, duration, Easing.EaseOutCubic, onUpdate, onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        CurrentOpacity = Lerp(_startOpacity, _endOpacity, easedT);
        _onUpdate?.Invoke(_edge, CurrentOpacity);

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

/// <summary>
/// Animates an edge's stroke dash offset for a "flow" effect.
/// </summary>
public class EdgeFlowAnimation : IAnimation
{
    private readonly Edge _edge;
    private readonly double _speed;
    private readonly bool _reverse;
    private readonly Action<Edge, double>? _onUpdate;
    private readonly double? _maxDuration;

    private double _elapsed;
    private double _currentOffset;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current dash offset value.
    /// </summary>
    public double CurrentDashOffset => _currentOffset;

    /// <summary>
    /// Creates a new edge flow animation.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="speed">Speed of the flow animation (pixels per second). Positive = forward (source to target), negative = reverse.</param>
    /// <param name="onUpdate">Callback called each frame with the edge and current dash offset.</param>
    /// <param name="maxDuration">Maximum duration in seconds, or null for infinite.</param>
    public EdgeFlowAnimation(
        Edge edge,
        double speed = 50,
        Action<Edge, double>? onUpdate = null,
        double? maxDuration = null)
        : this(edge, Math.Abs(speed), speed < 0, onUpdate, maxDuration)
    {
    }

    /// <summary>
    /// Creates a new edge flow animation with explicit direction control.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="speed">Speed of the flow animation (pixels per second, always positive).</param>
    /// <param name="reverse">If true, flow goes from target to source. If false, flow goes from source to target.</param>
    /// <param name="onUpdate">Callback called each frame with the edge and current dash offset.</param>
    /// <param name="maxDuration">Maximum duration in seconds, or null for infinite.</param>
    public EdgeFlowAnimation(
        Edge edge,
        double speed,
        bool reverse,
        Action<Edge, double>? onUpdate = null,
        double? maxDuration = null)
    {
        _edge = edge;
        _speed = Math.Abs(speed);
        _reverse = reverse;
        _onUpdate = onUpdate;
        _maxDuration = maxDuration;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        
        // Update offset based on direction
        if (_reverse)
        {
            _currentOffset -= _speed * deltaTime;
            // Wrap around to prevent underflow
            if (_currentOffset < -1000)
                _currentOffset += 1000;
        }
        else
        {
            _currentOffset += _speed * deltaTime;
            // Wrap around to prevent overflow
            if (_currentOffset > 1000)
                _currentOffset -= 1000;
        }

        _onUpdate?.Invoke(_edge, _currentOffset);

        if (_maxDuration.HasValue && _elapsed >= _maxDuration.Value)
        {
            IsComplete = true;
        }
    }

    public void Cancel()
    {
        IsComplete = true;
    }
}

/// <summary>
/// Animates an edge's stroke color.
/// </summary>
public class EdgeColorAnimation : IAnimation
{
    private readonly Edge _edge;
    private readonly Color _startColor;
    private readonly Color _endColor;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action<Edge, Color>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current color value.
    /// </summary>
    public Color CurrentColor { get; private set; }

    /// <summary>
    /// Creates a new edge color animation.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="startColor">Starting color.</param>
    /// <param name="endColor">Ending color.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function (defaults to EaseOutCubic).</param>
    /// <param name="onUpdate">Callback called each frame with the edge and current color.</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public EdgeColorAnimation(
        Edge edge,
        Color startColor,
        Color endColor,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action<Edge, Color>? onUpdate = null,
        Action? onComplete = null)
    {
        _edge = edge;
        _startColor = startColor;
        _endColor = endColor;
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentColor = _startColor;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        CurrentColor = LerpColor(_startColor, _endColor, easedT);
        _onUpdate?.Invoke(_edge, CurrentColor);

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

    private static Color LerpColor(Color start, Color end, double t)
    {
        return Color.FromArgb(
            (byte)(start.A + (end.A - start.A) * t),
            (byte)(start.R + (end.R - start.R) * t),
            (byte)(start.G + (end.G - start.G) * t),
            (byte)(start.B + (end.B - start.B) * t));
    }
}

/// <summary>
/// Animates an edge's stroke thickness for pulse/highlight effects.
/// </summary>
public class EdgePulseAnimation : IAnimation
{
    private readonly Edge _edge;
    private readonly double _baseThickness;
    private readonly double _pulseAmount;
    private readonly double _frequency;
    private readonly double _duration;
    private readonly Action<Edge, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current thickness value.
    /// </summary>
    public double CurrentThickness { get; private set; }

    /// <summary>
    /// Creates a new edge pulse animation.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="baseThickness">Base stroke thickness.</param>
    /// <param name="pulseAmount">Amount to add during pulse (thickness oscillates between base and base+pulseAmount).</param>
    /// <param name="frequency">Pulse frequency in Hz.</param>
    /// <param name="duration">Total animation duration in seconds.</param>
    /// <param name="onUpdate">Callback called each frame with the edge and current thickness.</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public EdgePulseAnimation(
        Edge edge,
        double baseThickness = 2,
        double pulseAmount = 2,
        double frequency = 2,
        double duration = 1,
        Action<Edge, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _edge = edge;
        _baseThickness = baseThickness;
        _pulseAmount = pulseAmount;
        _frequency = frequency;
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentThickness = baseThickness;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        
        // Sine wave oscillation
        var sine = Math.Sin(_elapsed * _frequency * 2 * Math.PI);
        var normalized = (sine + 1) / 2; // Convert from -1..1 to 0..1
        
        CurrentThickness = _baseThickness + _pulseAmount * normalized;
        _onUpdate?.Invoke(_edge, CurrentThickness);

        if (_elapsed >= _duration)
        {
            IsComplete = true;
            _onComplete?.Invoke();
        }
    }

    public void Cancel()
    {
        IsComplete = true;
    }
}

/// <summary>
/// Animates multiple edges simultaneously with the same animation parameters.
/// </summary>
public class MultiEdgeFadeAnimation : IAnimation
{
    private readonly List<Edge> _edges;
    private readonly double _startOpacity;
    private readonly double _endOpacity;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action<IReadOnlyList<Edge>, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current opacity value.
    /// </summary>
    public double CurrentOpacity { get; private set; }

    /// <summary>
    /// Creates a new multi-edge fade animation.
    /// </summary>
    public MultiEdgeFadeAnimation(
        IEnumerable<Edge> edges,
        double startOpacity,
        double endOpacity,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action<IReadOnlyList<Edge>, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _edges = edges.ToList();
        _startOpacity = Math.Clamp(startOpacity, 0, 1);
        _endOpacity = Math.Clamp(endOpacity, 0, 1);
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentOpacity = _startOpacity;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        CurrentOpacity = _startOpacity + (_endOpacity - _startOpacity) * easedT;
        _onUpdate?.Invoke(_edges, CurrentOpacity);

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
}
