using FlowGraph.Core;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates a selection highlight pulse effect.
/// </summary>
public class SelectionPulseAnimation : IAnimation
{
    private readonly double _duration;
    private readonly Action<double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current pulse intensity (0-1).
    /// </summary>
    public double CurrentIntensity { get; private set; }

    /// <summary>
    /// Creates a new selection pulse animation.
    /// </summary>
    /// <param name="duration">Total duration in seconds.</param>
    /// <param name="onUpdate">Callback with current intensity (0-1).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public SelectionPulseAnimation(
        double duration = 0.4,
        Action<double>? onUpdate = null,
        Action? onComplete = null)
    {
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);

        // Quick rise, slow fall
        if (t < 0.3)
        {
            // Rise to peak
            CurrentIntensity = Easing.EaseOutCubic(t / 0.3);
        }
        else
        {
            // Fall from peak
            CurrentIntensity = 1 - Easing.EaseOutCubic((t - 0.3) / 0.7);
        }

        _onUpdate?.Invoke(CurrentIntensity);

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

/// <summary>
/// Animates a node's selection border fading in.
/// </summary>
public class SelectionBorderAnimation : IAnimation
{
    private readonly Node _node;
    private readonly bool _selecting;
    private readonly double _duration;
    private readonly Action<Node, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current border opacity.
    /// </summary>
    public double CurrentOpacity { get; private set; }

    /// <summary>
    /// Creates a new selection border animation.
    /// </summary>
    public SelectionBorderAnimation(
        Node node,
        bool selecting,
        double duration = 0.15,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _node = node;
        _selecting = selecting;
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentOpacity = selecting ? 0 : 1;
    }

    /// <summary>
    /// Creates an animation for when a node becomes selected.
    /// </summary>
    public static SelectionBorderAnimation Select(
        Node node,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new SelectionBorderAnimation(node, true, 0.15, onUpdate, onComplete);
    }

    /// <summary>
    /// Creates an animation for when a node becomes deselected.
    /// </summary>
    public static SelectionBorderAnimation Deselect(
        Node node,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new SelectionBorderAnimation(node, false, 0.1, onUpdate, onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _selecting ? Easing.EaseOutCubic(t) : Easing.EaseInCubic(t);

        CurrentOpacity = _selecting ? easedT : (1 - easedT);
        _onUpdate?.Invoke(_node, CurrentOpacity);

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

/// <summary>
/// Animates a box selection rectangle.
/// </summary>
public class BoxSelectionAnimation : IAnimation
{
    private readonly double _dashSpeed;
    private readonly Action<double>? _onUpdate;

    private double _currentOffset;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current dash offset for animated border.
    /// </summary>
    public double CurrentDashOffset => _currentOffset;

    /// <summary>
    /// Creates a new box selection animation.
    /// </summary>
    /// <param name="dashSpeed">Speed of dash animation in pixels per second.</param>
    /// <param name="onUpdate">Callback with current dash offset.</param>
    public BoxSelectionAnimation(
        double dashSpeed = 30,
        Action<double>? onUpdate = null)
    {
        _dashSpeed = dashSpeed;
        _onUpdate = onUpdate;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _currentOffset += _dashSpeed * deltaTime;
        
        // Wrap around
        if (_currentOffset > 100)
            _currentOffset -= 100;

        _onUpdate?.Invoke(_currentOffset);
    }

    public void Cancel()
    {
        IsComplete = true;
    }
}
