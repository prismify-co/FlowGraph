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
    /// Starts an animation.
    /// </summary>
    public void Start(IAnimation animation)
    {
        lock (_lock)
        {
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
    }

    /// <summary>
    /// Stops and removes an animation.
    /// </summary>
    public void Stop(IAnimation animation)
    {
        lock (_lock)
        {
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
    }

    /// <summary>
    /// Stops all running animations.
    /// </summary>
    public void StopAll()
    {
        lock (_lock)
        {
            foreach (var animation in _animations)
            {
                animation.Cancel();
            }
            _animations.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }

        StopTimer();
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

        lock (_lock)
        {
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
    }

    public void Dispose()
    {
        StopAll();
    }
}
