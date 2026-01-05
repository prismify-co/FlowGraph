using FlowGraph.Core;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Animates a group's collapse or expand transition.
/// </summary>
public class GroupCollapseAnimation : IAnimation
{
    private readonly Node _group;
    private readonly bool _collapsing;
    private readonly double _startWidth;
    private readonly double _startHeight;
    private readonly double _endWidth;
    private readonly double _endHeight;
    private readonly double _duration;
    private readonly Action<Node, double, double, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current width.
    /// </summary>
    public double CurrentWidth { get; private set; }

    /// <summary>
    /// Gets the current height.
    /// </summary>
    public double CurrentHeight { get; private set; }

    /// <summary>
    /// Gets the current children opacity (0 when collapsed, 1 when expanded).
    /// </summary>
    public double ChildrenOpacity { get; private set; }

    /// <summary>
    /// Creates a new group collapse/expand animation.
    /// </summary>
    /// <param name="group">The group node to animate.</param>
    /// <param name="collapsing">True if collapsing, false if expanding.</param>
    /// <param name="expandedWidth">The width when expanded.</param>
    /// <param name="expandedHeight">The height when expanded.</param>
    /// <param name="collapsedWidth">The width when collapsed.</param>
    /// <param name="collapsedHeight">The height when collapsed.</param>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="onUpdate">Callback with (group, width, height, childrenOpacity).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    public GroupCollapseAnimation(
        Node group,
        bool collapsing,
        double expandedWidth,
        double expandedHeight,
        double collapsedWidth,
        double collapsedHeight,
        double duration = 0.3,
        Action<Node, double, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _group = group;
        _collapsing = collapsing;
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;

        if (collapsing)
        {
            _startWidth = expandedWidth;
            _startHeight = expandedHeight;
            _endWidth = collapsedWidth;
            _endHeight = collapsedHeight;
            ChildrenOpacity = 1;
        }
        else
        {
            _startWidth = collapsedWidth;
            _startHeight = collapsedHeight;
            _endWidth = expandedWidth;
            _endHeight = expandedHeight;
            ChildrenOpacity = 0;
        }

        CurrentWidth = _startWidth;
        CurrentHeight = _startHeight;
    }

    /// <summary>
    /// Creates a collapse animation for a group.
    /// </summary>
    public static GroupCollapseAnimation Collapse(
        Node group,
        double expandedWidth,
        double expandedHeight,
        double collapsedWidth = 150,
        double collapsedHeight = 50,
        double duration = 0.3,
        Action<Node, double, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new GroupCollapseAnimation(
            group, true, expandedWidth, expandedHeight, 
            collapsedWidth, collapsedHeight, duration, onUpdate, onComplete);
    }

    /// <summary>
    /// Creates an expand animation for a group.
    /// </summary>
    public static GroupCollapseAnimation Expand(
        Node group,
        double expandedWidth,
        double expandedHeight,
        double collapsedWidth = 150,
        double collapsedHeight = 50,
        double duration = 0.3,
        Action<Node, double, double, double>? onUpdate = null,
        Action? onComplete = null)
    {
        return new GroupCollapseAnimation(
            group, false, expandedWidth, expandedHeight,
            collapsedWidth, collapsedHeight, duration, onUpdate, onComplete);
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = Easing.EaseInOutCubic(t);

        // Interpolate dimensions
        CurrentWidth = _startWidth + (_endWidth - _startWidth) * easedT;
        CurrentHeight = _startHeight + (_endHeight - _startHeight) * easedT;

        // Children fade out faster when collapsing, fade in later when expanding
        if (_collapsing)
        {
            // Fade out children in first half of animation
            ChildrenOpacity = Math.Max(0, 1 - t * 2);
        }
        else
        {
            // Fade in children in second half of animation
            ChildrenOpacity = Math.Max(0, (t - 0.5) * 2);
        }

        _onUpdate?.Invoke(_group, CurrentWidth, CurrentHeight, ChildrenOpacity);

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
/// Animates children of a group fading in or out.
/// </summary>
public class GroupChildrenFadeAnimation : IAnimation
{
    private readonly IReadOnlyList<Node> _children;
    private readonly IReadOnlyList<Edge> _edges;
    private readonly bool _fadingIn;
    private readonly double _duration;
    private readonly Action<IReadOnlyList<Node>, IReadOnlyList<Edge>, double>? _onUpdate;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the current opacity.
    /// </summary>
    public double CurrentOpacity { get; private set; }

    /// <summary>
    /// Creates a new group children fade animation.
    /// </summary>
    public GroupChildrenFadeAnimation(
        IEnumerable<Node> children,
        IEnumerable<Edge> connectedEdges,
        bool fadingIn,
        double duration = 0.2,
        Action<IReadOnlyList<Node>, IReadOnlyList<Edge>, double>? onUpdate = null,
        Action? onComplete = null)
    {
        _children = children.ToList();
        _edges = connectedEdges.ToList();
        _fadingIn = fadingIn;
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        CurrentOpacity = fadingIn ? 0 : 1;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        var easedT = _fadingIn ? Easing.EaseOutCubic(t) : Easing.EaseInCubic(t);

        CurrentOpacity = _fadingIn ? easedT : (1 - easedT);
        _onUpdate?.Invoke(_children, _edges, CurrentOpacity);

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
