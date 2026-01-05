using FlowGraph.Core;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates a set of nodes from their current positions to target positions.
/// Intended for layout transitions (auto-layout, arrange, etc.).
/// </summary>
public sealed class LayoutAnimation : IAnimation
{
    private readonly IReadOnlyDictionary<string, Core.Point> _startPositions;
    private readonly IReadOnlyDictionary<string, Core.Point> _targetPositions;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action<IReadOnlyDictionary<string, Core.Point>> _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    public LayoutAnimation(
        IReadOnlyDictionary<string, Core.Point> startPositions,
        IReadOnlyDictionary<string, Core.Point> targetPositions,
        double duration,
        Action<IReadOnlyDictionary<string, Core.Point>> onUpdate,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        _startPositions = startPositions;
        _targetPositions = targetPositions;
        _duration = Math.Max(0.0001, duration);
        _easing = easing ?? Easing.EaseInOutCubic;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        var current = new Dictionary<string, Core.Point>(_targetPositions.Count);
        foreach (var (nodeId, target) in _targetPositions)
        {
            if (!_startPositions.TryGetValue(nodeId, out var start))
            {
                start = target;
            }

            current[nodeId] = new Core.Point(
                start.X + (target.X - start.X) * easedT,
                start.Y + (target.Y - start.Y) * easedT);
        }

        _onUpdate(current);

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
