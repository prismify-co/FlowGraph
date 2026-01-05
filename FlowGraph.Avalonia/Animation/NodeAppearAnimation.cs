using FlowGraph.Core;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates a node's opacity for fade in/out effects.
/// </summary>
public class NodeFadeAnimation : IAnimation
{
    private readonly Node _node;
    private readonly double _startOpacity;
    private readonly double _endOpacity;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action<Node, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current opacity value.
    /// </summary>
    public double CurrentOpacity { get; private set; }

    /// <summary>
    /// Creates a new node fade animation.
    /// </summary>
    public NodeFadeAnimation(
        Node node,
        double startOpacity,
        double endOpacity,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _node = node;
        _startOpacity = Math.Clamp(startOpacity, 0, 1);
        _endOpacity = Math.Clamp(endOpacity, 0, 1);
        _duration = duration;
        _easing = easing ?? Easing.EaseOutCubic;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentOpacity = _startOpacity;
    }

    /// <summary>
    /// Creates a fade-in animation for a node.
    /// </summary>
    public static NodeFadeAnimation FadeIn(
        Node node,
        double duration = 0.3,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new NodeFadeAnimation(node, 0, 1, duration, Easing.EaseOutCubic, onUpdate, onComplete);
    }

    /// <summary>
    /// Creates a fade-out animation for a node.
    /// </summary>
    public static NodeFadeAnimation FadeOut(
        Node node,
        double duration = 0.3,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new NodeFadeAnimation(node, 1, 0, duration, Easing.EaseOutCubic, onUpdate, onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        CurrentOpacity = _startOpacity + (_endOpacity - _startOpacity) * easedT;
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
/// Animates a node's scale for pop/shrink effects.
/// </summary>
public class NodeScaleAnimation : IAnimation
{
    private readonly Node _node;
    private readonly double _startScale;
    private readonly double _endScale;
    private readonly double _duration;
    private readonly Func<double, double> _easing;
    private readonly Action<Node, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current scale value.
    /// </summary>
    public double CurrentScale { get; private set; }

    /// <summary>
    /// Creates a new node scale animation.
    /// </summary>
    public NodeScaleAnimation(
        Node node,
        double startScale,
        double endScale,
        double duration = 0.3,
        Func<double, double>? easing = null,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _node = node;
        _startScale = startScale;
        _endScale = endScale;
        _duration = duration;
        _easing = easing ?? Easing.EaseOutBack;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentScale = _startScale;
    }

    /// <summary>
    /// Creates a "pop in" animation (scale from small to normal with overshoot).
    /// </summary>
    public static NodeScaleAnimation PopIn(
        Node node,
        double duration = 0.3,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new NodeScaleAnimation(node, 0.5, 1.0, duration, Easing.EaseOutBack, onUpdate, onComplete);
    }

    /// <summary>
    /// Creates a "shrink out" animation (scale from normal to small).
    /// </summary>
    public static NodeScaleAnimation ShrinkOut(
        Node node,
        double duration = 0.2,
        Action<Node, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new NodeScaleAnimation(node, 1.0, 0.0, duration, Easing.EaseInCubic, onUpdate, onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _easing(t);

        CurrentScale = _startScale + (_endScale - _startScale) * easedT;
        _onUpdate?.Invoke(_node, CurrentScale);

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
/// Combines fade and scale for a complete appear/disappear effect.
/// </summary>
public class NodeAppearAnimation : IAnimation
{
    private readonly Node _node;
    private readonly bool _appearing;
    private readonly double _duration;
    private readonly Action<Node, double, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current opacity value.
    /// </summary>
    public double CurrentOpacity { get; private set; }

    /// <summary>
    /// Gets the current scale value.
    /// </summary>
    public double CurrentScale { get; private set; }

    /// <summary>
    /// Creates a new node appear/disappear animation.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="appearing">True for appear animation, false for disappear.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="onUpdate">Callback with (node, opacity, scale).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public NodeAppearAnimation(
        Node node,
        bool appearing,
        double duration = 0.3,
        Action<Node, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _node = node;
        _appearing = appearing;
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        
        CurrentOpacity = appearing ? 0 : 1;
        CurrentScale = appearing ? 0.8 : 1;
    }

    /// <summary>
    /// Creates an appear animation for a node.
    /// </summary>
    public static NodeAppearAnimation Appear(
        Node node,
        double duration = 0.3,
        Action<Node, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new NodeAppearAnimation(node, true, duration, onUpdate, onComplete);
    }

    /// <summary>
    /// Creates a disappear animation for a node.
    /// </summary>
    public static NodeAppearAnimation Disappear(
        Node node,
        double duration = 0.2,
        Action<Node, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new NodeAppearAnimation(node, false, duration, onUpdate, onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        
        if (_appearing)
        {
            // Appear: fade in with ease-out, scale with overshoot
            CurrentOpacity = Easing.EaseOutCubic(t);
            CurrentScale = 0.8 + 0.2 * Easing.EaseOutBack(t);
        }
        else
        {
            // Disappear: fade out and shrink
            CurrentOpacity = 1 - Easing.EaseInCubic(t);
            CurrentScale = 1 - 0.3 * Easing.EaseInCubic(t);
        }

        _onUpdate?.Invoke(_node, CurrentOpacity, CurrentScale);

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
/// Animates multiple nodes appearing/disappearing simultaneously.
/// </summary>
public class MultiNodeAppearAnimation : IAnimation
{
    private readonly List<Node> _nodes;
    private readonly bool _appearing;
    private readonly double _duration;
    private readonly double _stagger;
    private readonly Action<Node, double, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;
    private readonly Dictionary<Node, double> _nodeStartTimes;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new multi-node appear/disappear animation.
    /// </summary>
    /// <param name="nodes">The nodes to animate.</param>
    /// <param name="appearing">True for appear animation, false for disappear.</param>
    /// <param name="duration">Animation duration per node in seconds.</param>
    /// <param name="stagger">Delay between each node's animation start (0 = all at once).</param>
    /// <param name="onUpdate">Callback with (node, opacity, scale) for each node.</param>
    /// <param name="onComplete">Callback when all animations complete.</param>
    public MultiNodeAppearAnimation(
        IEnumerable<Node> nodes,
        bool appearing,
        double duration = 0.3,
        double stagger = 0.05,
        Action<Node, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _nodes = nodes.ToList();
        _appearing = appearing;
        _duration = duration;
        _stagger = stagger;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        
        _nodeStartTimes = new Dictionary<Node, double>();
        for (int i = 0; i < _nodes.Count; i++)
        {
            _nodeStartTimes[_nodes[i]] = i * stagger;
        }
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var totalDuration = (_nodes.Count - 1) * _stagger + _duration;
        var allComplete = true;

        foreach (var node in _nodes)
        {
            var startTime = _nodeStartTimes[node];
            var nodeElapsed = _elapsed - startTime;
            
            if (nodeElapsed < 0)
            {
                // Not started yet
                _onUpdate?.Invoke(node, _appearing ? 0 : 1, _appearing ? 0.8 : 1);
                allComplete = false;
            }
            else if (nodeElapsed >= _duration)
            {
                // Complete
                _onUpdate?.Invoke(node, _appearing ? 1 : 0, _appearing ? 1 : 0.7);
            }
            else
            {
                // Animating
                allComplete = false;
                var t = nodeElapsed / _duration;
                
                double opacity, scale;
                if (_appearing)
                {
                    opacity = Easing.EaseOutCubic(t);
                    scale = 0.8 + 0.2 * Easing.EaseOutBack(t);
                }
                else
                {
                    opacity = 1 - Easing.EaseInCubic(t);
                    scale = 1 - 0.3 * Easing.EaseInCubic(t);
                }
                
                _onUpdate?.Invoke(node, opacity, scale);
            }
        }

        if (allComplete || _elapsed >= totalDuration)
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
