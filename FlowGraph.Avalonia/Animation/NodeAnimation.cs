using FlowGraph.Core;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates a node's position.
/// </summary>
public class NodeAnimation : IAnimation
{
    private readonly Node _node;
    private readonly CorePoint _startPosition;
    private readonly CorePoint _endPosition;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new node position animation.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="targetPosition">Target position.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function (defaults to EaseOutCubic).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public NodeAnimation(
        Node node,
        CorePoint targetPosition,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        _node = node;
        _startPosition = node.Position;
        _endPosition = targetPosition;
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onComplete = onComplete;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        // Interpolate position
        var currentX = Lerp(_startPosition.X, _endPosition.X, easedT);
        var currentY = Lerp(_startPosition.Y, _endPosition.Y, easedT);

        _node.Position = new CorePoint(currentX, currentY);

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
/// Animates multiple nodes' positions simultaneously.
/// </summary>
public class MultiNodeAnimation : IAnimation
{
    private readonly List<(Node Node, CorePoint StartPosition, CorePoint EndPosition)> _nodeData;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new multi-node position animation.
    /// </summary>
    /// <param name="nodePositions">Dictionary mapping nodes to their target positions.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="easing">Easing function (defaults to EaseOutCubic).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public MultiNodeAnimation(
        IReadOnlyDictionary<Node, CorePoint> nodePositions,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        _nodeData = nodePositions
            .Select(kvp => (kvp.Key, kvp.Key.Position, kvp.Value))
            .ToList();
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onComplete = onComplete;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        // Interpolate all node positions
        foreach (var (node, startPos, endPos) in _nodeData)
        {
            var currentX = Lerp(startPos.X, endPos.X, easedT);
            var currentY = Lerp(startPos.Y, endPos.Y, easedT);
            node.Position = new CorePoint(currentX, currentY);
        }

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
