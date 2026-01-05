namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// A generic animation that calls an update action with the normalized progress (0-1).
/// Useful for simple animations or sequencing multiple animation phases.
/// </summary>
public class GenericAnimation : IAnimation
{
    private readonly double _duration;
    private readonly Action<double> _onUpdate;
    private readonly Action? _onComplete;
    private readonly Func<double, double>? _easing;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new generic animation.
    /// </summary>
    /// <param name="duration">Animation duration in seconds.</param>
    /// <param name="onUpdate">Callback called each frame with normalized progress (0-1).</param>
    /// <param name="onComplete">Callback when animation completes.</param>
    /// <param name="easing">Optional easing function (if null, linear progress is passed to onUpdate).</param>
    public GenericAnimation(
        double duration,
        Action<double> onUpdate,
        Action? onComplete = null,
        Func<double, double>? easing = null)
    {
        _duration = duration;
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        _easing = easing;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        var t = Math.Clamp(_elapsed / _duration, 0, 1);
        
        // Apply easing if provided, otherwise pass raw t
        var progress = _easing != null ? _easing(t) : t;
        _onUpdate(progress);

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
/// A delay animation that does nothing for a specified duration, then completes.
/// Useful for sequencing animations with pauses.
/// </summary>
public class DelayAnimation : IAnimation
{
    private readonly double _duration;
    private readonly Action? _onComplete;

    private double _elapsed;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new delay animation.
    /// </summary>
    /// <param name="duration">Delay duration in seconds.</param>
    /// <param name="onComplete">Callback when delay completes.</param>
    public DelayAnimation(double duration, Action? onComplete = null)
    {
        _duration = duration;
        _onComplete = onComplete;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;

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
/// A sequence of animations that run one after another.
/// </summary>
public class SequenceAnimation : IAnimation
{
    private readonly List<IAnimation> _animations;
    private int _currentIndex;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new sequence animation.
    /// </summary>
    /// <param name="animations">The animations to run in sequence.</param>
    public SequenceAnimation(params IAnimation[] animations)
    {
        _animations = animations.ToList();
        _currentIndex = 0;
    }

    /// <summary>
    /// Creates a new sequence animation.
    /// </summary>
    /// <param name="animations">The animations to run in sequence.</param>
    public SequenceAnimation(IEnumerable<IAnimation> animations)
    {
        _animations = animations.ToList();
        _currentIndex = 0;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        while (_currentIndex < _animations.Count)
        {
            var current = _animations[_currentIndex];
            current.Update(deltaTime);

            if (current.IsComplete)
            {
                _currentIndex++;
                // Don't consume remaining delta time - let next animation start fresh
            }
            else
            {
                // Current animation not done yet
                return;
            }
        }

        // All animations complete
        IsComplete = true;
    }

    public void Cancel()
    {
        // Cancel current animation
        if (_currentIndex < _animations.Count)
        {
            _animations[_currentIndex].Cancel();
        }
        IsComplete = true;
    }
}

/// <summary>
/// A group of animations that run in parallel.
/// </summary>
public class ParallelAnimation : IAnimation
{
    private readonly List<IAnimation> _animations;
    private readonly Action? _onComplete;

    public bool IsComplete { get; private set; }

    /// <summary>
    /// Creates a new parallel animation.
    /// </summary>
    /// <param name="animations">The animations to run in parallel.</param>
    public ParallelAnimation(params IAnimation[] animations)
    {
        _animations = animations.ToList();
    }

    /// <summary>
    /// Creates a new parallel animation.
    /// </summary>
    /// <param name="animations">The animations to run in parallel.</param>
    /// <param name="onComplete">Callback when all animations complete.</param>
    public ParallelAnimation(IEnumerable<IAnimation> animations, Action? onComplete = null)
    {
        _animations = animations.ToList();
        _onComplete = onComplete;
    }

    public void Update(double deltaTime)
    {
        if (IsComplete) return;

        var allComplete = true;
        foreach (var animation in _animations)
        {
            if (!animation.IsComplete)
            {
                animation.Update(deltaTime);
                if (!animation.IsComplete)
                {
                    allComplete = false;
                }
            }
        }

        if (allComplete)
        {
            IsComplete = true;
            _onComplete?.Invoke();
        }
    }

    public void Cancel()
    {
        foreach (var animation in _animations)
        {
            animation.Cancel();
        }
        IsComplete = true;
    }
}
