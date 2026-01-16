// EdgeFlowAnimationManager.cs
// Manages automatic edge flow animations based on EdgeStyle.AnimatedFlow

using FlowGraph.Core;
using FlowGraph.Core.Models;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Manages automatic edge flow animations based on EdgeStyle settings.
/// </summary>
/// <remarks>
/// <para>
/// This manager monitors edges in the graph and automatically starts/stops
/// flow animations based on the <see cref="EdgeStyle.AnimatedFlow"/> property.
/// </para>
/// <para>
/// Animation parameters are taken from the EdgeStyle:
/// - <see cref="EdgeStyle.FlowSpeed"/>: Controls animation speed (1.0 = normal, 2.0 = double)
/// - <see cref="EdgeStyle.FlowDirection"/>: Forward, Reverse, or Bidirectional
/// - <see cref="EdgeStyle.DashPattern"/>: Used if set, otherwise defaults to marching ants
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Edges with AnimatedFlow style are automatically animated:
/// edge.Definition = edge.Definition with {
///     Style = EdgeStyle.ActiveFlow  // Has AnimatedFlow = true
/// };
/// 
/// // Or create a custom animated style:
/// edge.Definition = edge.Definition with {
///     Style = new EdgeStyle {
///         AnimatedFlow = true,
///         FlowSpeed = 2.0,
///         FlowDirection = EdgeFlowDirection.Reverse,
///         StrokeColor = "#00FF00"
///     }
/// };
/// </code>
/// </example>
public class EdgeFlowAnimationManager : IDisposable
{
  private readonly AnimationManager _animationManager;
  private readonly Action<Edge, double> _updateDashOffset;
  private readonly Dictionary<string, EdgeFlowAnimation> _activeAnimations = new();
  private readonly Dictionary<string, BidirectionalFlowState> _bidirectionalStates = new();

  /// <summary>
  /// Base speed multiplier for flow animations (pixels per second at FlowSpeed = 1.0).
  /// </summary>
  public double BaseFlowSpeed { get; set; } = 40.0;

  /// <summary>
  /// Gets the number of currently active flow animations.
  /// </summary>
  public int ActiveAnimationCount => _activeAnimations.Count;

  /// <summary>
  /// Creates a new EdgeFlowAnimationManager.
  /// </summary>
  /// <param name="animationManager">The animation manager to use for running animations.</param>
  /// <param name="updateDashOffset">Callback to update edge dash offset during animation.</param>
  public EdgeFlowAnimationManager(
      AnimationManager animationManager,
      Action<Edge, double> updateDashOffset)
  {
    _animationManager = animationManager ?? throw new ArgumentNullException(nameof(animationManager));
    _updateDashOffset = updateDashOffset ?? throw new ArgumentNullException(nameof(updateDashOffset));
  }

  /// <summary>
  /// Synchronizes flow animations with the current graph state.
  /// Starts animations for edges with AnimatedFlow=true, stops others.
  /// </summary>
  /// <param name="edges">All edges in the graph.</param>
  public void SyncAnimations(IEnumerable<Edge> edges)
  {
    var edgeList = edges.ToList();
    var edgeIds = new HashSet<string>(edgeList.Select(e => e.Id));

    // Stop animations for removed edges
    var toRemove = _activeAnimations.Keys.Where(id => !edgeIds.Contains(id)).ToList();
    foreach (var edgeId in toRemove)
    {
      StopAnimation(edgeId);
    }

    // Update or start animations for edges with AnimatedFlow
    foreach (var edge in edgeList)
    {
      var style = edge.Style;
      var shouldAnimate = style?.AnimatedFlow == true;

      if (shouldAnimate)
      {
        // Check if we need to restart (style changed)
        if (_activeAnimations.TryGetValue(edge.Id, out var existing))
        {
          // Animation already running - check if parameters changed
          var currentSpeed = GetEffectiveSpeed(style);
          var currentReverse = style?.FlowDirection == EdgeFlowDirection.Reverse;
          var isBidirectional = style?.FlowDirection == EdgeFlowDirection.Bidirectional;

          // For bidirectional, we handle direction changes ourselves
          if (!isBidirectional)
          {
            // Simple flow - let it continue unless direction changed significantly
            // The animation will naturally use its configured direction
          }
        }
        else
        {
          // Start new animation
          StartAnimation(edge);
        }
      }
      else if (_activeAnimations.ContainsKey(edge.Id))
      {
        // AnimatedFlow turned off - stop animation
        StopAnimation(edge.Id);
      }
    }
  }

  /// <summary>
  /// Starts a flow animation for a specific edge.
  /// </summary>
  /// <param name="edge">The edge to animate.</param>
  public void StartAnimation(Edge edge)
  {
    if (_activeAnimations.ContainsKey(edge.Id))
    {
      StopAnimation(edge.Id);
    }

    var style = edge.Style ?? EdgeStyle.Default;
    var speed = GetEffectiveSpeed(style);
    var isBidirectional = style.FlowDirection == EdgeFlowDirection.Bidirectional;
    var isReverse = style.FlowDirection == EdgeFlowDirection.Reverse;

    if (isBidirectional)
    {
      // Create bidirectional flow state
      var state = new BidirectionalFlowState(speed);
      _bidirectionalStates[edge.Id] = state;

      // Start forward animation initially
      var animation = new EdgeFlowAnimation(
          edge,
          speed,
          reverse: false,
          onUpdate: (e, offset) => UpdateBidirectionalOffset(e, offset, state));
      _activeAnimations[edge.Id] = animation;
      _animationManager.Start(animation);
    }
    else
    {
      // Create standard directional flow
      var animation = new EdgeFlowAnimation(
          edge,
          speed,
          reverse: isReverse,
          onUpdate: _updateDashOffset);
      _activeAnimations[edge.Id] = animation;
      _animationManager.Start(animation);
    }
  }

  /// <summary>
  /// Stops a flow animation for a specific edge.
  /// </summary>
  /// <param name="edgeId">The ID of the edge to stop animating.</param>
  public void StopAnimation(string edgeId)
  {
    if (_activeAnimations.TryGetValue(edgeId, out var animation))
    {
      _animationManager.Stop(animation);
      _activeAnimations.Remove(edgeId);
      _bidirectionalStates.Remove(edgeId);
    }
  }

  /// <summary>
  /// Stops all flow animations.
  /// </summary>
  public void StopAllAnimations()
  {
    foreach (var animation in _activeAnimations.Values.ToList())
    {
      _animationManager.Stop(animation);
    }
    _activeAnimations.Clear();
    _bidirectionalStates.Clear();
  }

  /// <summary>
  /// Checks if a specific edge has an active flow animation.
  /// </summary>
  /// <param name="edgeId">The ID of the edge to check.</param>
  /// <returns>True if the edge has an active flow animation.</returns>
  public bool IsAnimating(string edgeId) => _activeAnimations.ContainsKey(edgeId);

  /// <summary>
  /// Gets the effective animation speed in pixels per second.
  /// </summary>
  private double GetEffectiveSpeed(EdgeStyle? style)
  {
    var speedMultiplier = style?.FlowSpeed ?? 1.0;
    return BaseFlowSpeed * Math.Max(0.1, speedMultiplier);
  }

  /// <summary>
  /// Handles bidirectional animation offset updates with direction reversal.
  /// </summary>
  private void UpdateBidirectionalOffset(Edge edge, double rawOffset, BidirectionalFlowState state)
  {
    // Update accumulated distance
    state.AccumulatedDistance += Math.Abs(rawOffset - state.LastRawOffset);
    state.LastRawOffset = rawOffset;

    // Check if we should reverse direction
    const double reversalThreshold = 200.0; // Reverse after traveling this distance
    if (state.AccumulatedDistance >= reversalThreshold)
    {
      state.AccumulatedDistance = 0;
      state.IsReversed = !state.IsReversed;
    }

    // Calculate actual offset with direction
    var actualOffset = state.IsReversed ? -rawOffset : rawOffset;
    _updateDashOffset(edge, actualOffset);
  }

  /// <summary>
  /// Releases all resources.
  /// </summary>
  public void Dispose()
  {
    StopAllAnimations();
  }

  /// <summary>
  /// Tracks state for bidirectional flow animations.
  /// </summary>
  private class BidirectionalFlowState
  {
    public double BaseSpeed { get; }
    public bool IsReversed { get; set; }
    public double AccumulatedDistance { get; set; }
    public double LastRawOffset { get; set; }

    public BidirectionalFlowState(double baseSpeed)
    {
      BaseSpeed = baseSpeed;
    }
  }
}
