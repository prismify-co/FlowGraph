using Avalonia;
using Avalonia.Threading;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Manages running animations and coordinates frame updates.
/// </summary>
public class AnimationManager : IDisposable
{
    private readonly List<IAnimation> _animations = new();
    private readonly List<IAnimation> _pendingAdd = new();
    private readonly List<IAnimation> _pendingRemove = new();
    private readonly object _lock = new();
    
    private DispatcherTimer? _timer;
    private DateTime _lastUpdate;
    private bool _isRunning;
    private bool _isUpdating;
    
    /// <summary>
    /// Target frames per second for animation updates.
    /// </summary>
    public int TargetFps { get; set; } = 60;

    /// <summary>
    /// Event raised after each animation frame update.
    /// </summary>
    public event EventHandler? FrameUpdated;

    /// <summary>
    /// Event raised when the set of active animation categories changes.
    /// </summary>
    public event EventHandler<AnimationCategoriesChangedEventArgs>? CategoriesChanged;

    /// <summary>
    /// Gets whether any animations are currently running.
    /// </summary>
    public bool HasAnimations
    {
        get
        {
            lock (_lock)
            {
                return _animations.Count > 0 || _pendingAdd.Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets the currently active animation categories.
    /// </summary>
    public IReadOnlySet<AnimationCategory> ActiveCategories
    {
        get
        {
            lock (_lock)
            {
                var categories = new HashSet<AnimationCategory>();
                foreach (var anim in _animations.Concat(_pendingAdd))
                {
                    if (anim is ICategorizedAnimation categorized)
                    {
                        categories.Add(categorized.Category);
                    }
                    else
                    {
                        categories.Add(AnimationCategory.Other);
                    }
                }
                return categories;
            }
        }
    }

    /// <summary>
    /// Gets the IDs of all nodes currently being animated.
    /// </summary>
    public IReadOnlySet<string> AnimatingNodeIds
    {
        get
        {
            lock (_lock)
            {
                var nodeIds = new HashSet<string>();
                foreach (var anim in _animations.Concat(_pendingAdd))
                {
                    if (anim is ICategorizedAnimation categorized)
                    {
                        foreach (var nodeId in categorized.AffectedNodeIds)
                        {
                            nodeIds.Add(nodeId);
                        }
                    }
                }
                return nodeIds;
            }
        }
    }

    /// <summary>
    /// Checks if a specific animation category is currently active.
    /// </summary>
    public bool HasCategory(AnimationCategory category)
    {
        lock (_lock)
        {
            foreach (var anim in _animations.Concat(_pendingAdd))
            {
                if (anim is ICategorizedAnimation categorized && categorized.Category == category)
                    return true;
                if (category == AnimationCategory.Other && anim is not ICategorizedAnimation)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Checks if viewport animations are running.
    /// </summary>
    public bool HasViewportAnimations => HasCategory(AnimationCategory.Viewport);

    /// <summary>
    /// Checks if node position or appearance animations are running.
    /// </summary>
    public bool HasNodeAnimations => 
        HasCategory(AnimationCategory.NodePosition) || 
        HasCategory(AnimationCategory.NodeAppearance);

    /// <summary>
    /// Checks if group animations are running.
    /// </summary>
    public bool HasGroupAnimations => HasCategory(AnimationCategory.Group);

    /// <summary>
    /// Starts an animation.
    /// </summary>
    public void Start(IAnimation animation)
    {
        HashSet<AnimationCategory>? oldCategories = null;
        
        lock (_lock)
        {
            oldCategories = new HashSet<AnimationCategory>(GetCurrentCategories());
            
            if (_isUpdating)
            {
                _pendingAdd.Add(animation);
            }
            else
            {
                _animations.Add(animation);
            }
        }

        EnsureTimerRunning();
        RaiseCategoriesChangedIfNeeded(oldCategories);
    }

    /// <summary>
    /// Starts an animation with category information.
    /// </summary>
    public void Start(IAnimation animation, AnimationCategory category, IEnumerable<string>? affectedNodeIds = null)
    {
        var wrapped = new CategorizedAnimationWrapper(animation, category, affectedNodeIds);
        Start(wrapped);
    }

    /// <summary>
    /// Stops and removes an animation.
    /// </summary>
    public void Stop(IAnimation animation)
    {
        HashSet<AnimationCategory>? oldCategories = null;
        
        lock (_lock)
        {
            oldCategories = new HashSet<AnimationCategory>(GetCurrentCategories());
            
            if (_isUpdating)
            {
                _pendingRemove.Add(animation);
            }
            else
            {
                _animations.Remove(animation);
            }
        }
        
        animation.Cancel();
        RaiseCategoriesChangedIfNeeded(oldCategories);
    }

    /// <summary>
    /// Stops all animations of a specific category.
    /// </summary>
    public void StopCategory(AnimationCategory category)
    {
        HashSet<AnimationCategory>? oldCategories = null;
        List<IAnimation> toStop;
        
        lock (_lock)
        {
            oldCategories = new HashSet<AnimationCategory>(GetCurrentCategories());
            
            toStop = _animations
                .Where(a => GetAnimationCategory(a) == category)
                .ToList();
            
            foreach (var anim in toStop)
            {
                if (_isUpdating)
                {
                    _pendingRemove.Add(anim);
                }
                else
                {
                    _animations.Remove(anim);
                }
                anim.Cancel();
            }
        }
        
        RaiseCategoriesChangedIfNeeded(oldCategories);
    }

    /// <summary>
    /// Stops all viewport animations.
    /// </summary>
    public void StopViewportAnimations() => StopCategory(AnimationCategory.Viewport);

    /// <summary>
    /// Stops all node animations.
    /// </summary>
    public void StopNodeAnimations()
    {
        StopCategory(AnimationCategory.NodePosition);
        StopCategory(AnimationCategory.NodeAppearance);
    }

    /// <summary>
    /// Stops all running animations.
    /// </summary>
    public void StopAll()
    {
        HashSet<AnimationCategory>? oldCategories = null;
        
        lock (_lock)
        {
            oldCategories = new HashSet<AnimationCategory>(GetCurrentCategories());
            
            foreach (var animation in _animations)
            {
                animation.Cancel();
            }
            _animations.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }

        StopTimer();
        RaiseCategoriesChangedIfNeeded(oldCategories);
    }

    private IEnumerable<AnimationCategory> GetCurrentCategories()
    {
        foreach (var anim in _animations.Concat(_pendingAdd))
        {
            yield return GetAnimationCategory(anim);
        }
    }

    private static AnimationCategory GetAnimationCategory(IAnimation animation)
    {
        return animation is ICategorizedAnimation categorized 
            ? categorized.Category 
            : AnimationCategory.Other;
    }

    private void RaiseCategoriesChangedIfNeeded(HashSet<AnimationCategory>? oldCategories)
    {
        if (oldCategories == null) return;
        
        var newCategories = new HashSet<AnimationCategory>(ActiveCategories);
        if (!oldCategories.SetEquals(newCategories))
        {
            CategoriesChanged?.Invoke(this, new AnimationCategoriesChangedEventArgs(oldCategories, newCategories));
        }
    }

    private void EnsureTimerRunning()
    {
        if (_isRunning) return;

        _isRunning = true;
        _lastUpdate = DateTime.Now;
        
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / TargetFps)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void StopTimer()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // Clamp delta time to prevent huge jumps
        deltaTime = Math.Min(deltaTime, 0.1);

        List<IAnimation> completedAnimations;
        HashSet<AnimationCategory>? oldCategories = null;

        lock (_lock)
        {
            oldCategories = new HashSet<AnimationCategory>(GetCurrentCategories());
            _isUpdating = true;
            
            // Update all animations
            foreach (var animation in _animations)
            {
                animation.Update(deltaTime);
            }

            // Find completed animations
            completedAnimations = _animations.Where(a => a.IsComplete).ToList();
            
            _isUpdating = false;

            // Process pending changes
            foreach (var animation in _pendingRemove)
            {
                _animations.Remove(animation);
            }
            _pendingRemove.Clear();

            foreach (var animation in _pendingAdd)
            {
                _animations.Add(animation);
            }
            _pendingAdd.Clear();

            // Remove completed animations
            foreach (var animation in completedAnimations)
            {
                _animations.Remove(animation);
            }

            // Stop timer if no more animations
            if (_animations.Count == 0)
            {
                StopTimer();
            }
        }

        FrameUpdated?.Invoke(this, EventArgs.Empty);
        RaiseCategoriesChangedIfNeeded(oldCategories);
    }

    public void Dispose()
    {
        StopAll();
    }
}

/// <summary>
/// Event args for animation category changes.
/// </summary>
public class AnimationCategoriesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Categories that were active before the change.
    /// </summary>
    public IReadOnlySet<AnimationCategory> OldCategories { get; }

    /// <summary>
    /// Categories that are active after the change.
    /// </summary>
    public IReadOnlySet<AnimationCategory> NewCategories { get; }

    /// <summary>
    /// Categories that were added.
    /// </summary>
    public IEnumerable<AnimationCategory> AddedCategories => NewCategories.Except(OldCategories);

    /// <summary>
    /// Categories that were removed.
    /// </summary>
    public IEnumerable<AnimationCategory> RemovedCategories => OldCategories.Except(NewCategories);

    public AnimationCategoriesChangedEventArgs(
        IReadOnlySet<AnimationCategory> oldCategories,
        IReadOnlySet<AnimationCategory> newCategories)
    {
        OldCategories = oldCategories;
        NewCategories = newCategories;
    }
}
