// EdgeEffectsManager.cs
// Manages automatic edge effects (rainbow, pulse) based on EdgeStyle settings

using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.Models;

namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Manages automatic edge effects based on EdgeStyle settings.
/// </summary>
/// <remarks>
/// <para>
/// This manager handles effects beyond simple flow animation:
/// - Rainbow color cycling
/// - Pulsing opacity/glow effects
/// </para>
/// <para>
/// Works in conjunction with <see cref="EdgeFlowAnimationManager"/> which handles
/// dash offset animations.
/// </para>
/// </remarks>
public class EdgeEffectsManager : IDisposable
{
  private readonly AnimationManager _animationManager;
  private readonly Action<Edge, Color> _updateEdgeColor;
  private readonly Action<Edge, double> _updateEdgeOpacity;

  private readonly Dictionary<string, IAnimation> _rainbowAnimations = new();
  private readonly Dictionary<string, IAnimation> _pulseAnimations = new();

  /// <summary>
  /// Gets the number of active rainbow animations.
  /// </summary>
  public int ActiveRainbowCount => _rainbowAnimations.Count;

  /// <summary>
  /// Gets the number of active pulse animations.
  /// </summary>
  public int ActivePulseCount => _pulseAnimations.Count;

  /// <summary>
  /// Gets the total number of active effect animations.
  /// </summary>
  public int TotalActiveEffects => _rainbowAnimations.Count + _pulseAnimations.Count;

  /// <summary>
  /// Creates a new EdgeEffectsManager.
  /// </summary>
  /// <param name="animationManager">The animation manager to use for running animations.</param>
  /// <param name="updateEdgeColor">Callback to update edge color during rainbow animation.</param>
  /// <param name="updateEdgeOpacity">Callback to update edge opacity during pulse animation.</param>
  public EdgeEffectsManager(
      AnimationManager animationManager,
      Action<Edge, Color> updateEdgeColor,
      Action<Edge, double> updateEdgeOpacity)
  {
    _animationManager = animationManager ?? throw new ArgumentNullException(nameof(animationManager));
    _updateEdgeColor = updateEdgeColor ?? throw new ArgumentNullException(nameof(updateEdgeColor));
    _updateEdgeOpacity = updateEdgeOpacity ?? throw new ArgumentNullException(nameof(updateEdgeOpacity));
  }

  /// <summary>
  /// Synchronizes effects with the current graph state.
  /// </summary>
  /// <param name="edges">All edges in the graph.</param>
  public void SyncEffects(IEnumerable<Edge> edges)
  {
    var edgeList = edges.ToList();
    var edgeIds = new HashSet<string>(edgeList.Select(e => e.Id));

    // Stop effects for removed edges
    StopRemovedEdgeEffects(edgeIds);

    // Update or start effects for edges with effects enabled
    foreach (var edge in edgeList)
    {
      var style = edge.Style;
      SyncRainbowEffect(edge, style);
      SyncPulseEffect(edge, style);
    }
  }

  private void StopRemovedEdgeEffects(HashSet<string> currentEdgeIds)
  {
    // Rainbow
    var rainbowToRemove = _rainbowAnimations.Keys.Where(id => !currentEdgeIds.Contains(id)).ToList();
    foreach (var edgeId in rainbowToRemove)
    {
      StopRainbowEffect(edgeId);
    }

    // Pulse
    var pulseToRemove = _pulseAnimations.Keys.Where(id => !currentEdgeIds.Contains(id)).ToList();
    foreach (var edgeId in pulseToRemove)
    {
      StopPulseEffect(edgeId);
    }
  }

  private void SyncRainbowEffect(Edge edge, EdgeStyle? style)
  {
    var shouldHaveRainbow = style?.RainbowEffect == true;

    if (shouldHaveRainbow && !_rainbowAnimations.ContainsKey(edge.Id))
    {
      StartRainbowEffect(edge, style!);
    }
    else if (!shouldHaveRainbow && _rainbowAnimations.ContainsKey(edge.Id))
    {
      StopRainbowEffect(edge.Id);
    }
  }

  private void SyncPulseEffect(Edge edge, EdgeStyle? style)
  {
    var shouldHavePulse = style?.PulseEffect == true;

    if (shouldHavePulse && !_pulseAnimations.ContainsKey(edge.Id))
    {
      StartPulseEffect(edge, style!);
    }
    else if (!shouldHavePulse && _pulseAnimations.ContainsKey(edge.Id))
    {
      StopPulseEffect(edge.Id);
    }
  }

  /// <summary>
  /// Starts a rainbow color cycling effect for an edge.
  /// </summary>
  public void StartRainbowEffect(Edge edge, EdgeStyle style)
  {
    if (_rainbowAnimations.ContainsKey(edge.Id))
    {
      StopRainbowEffect(edge.Id);
    }

    var speed = style.RainbowSpeed;
    var animation = new EdgeRainbowAnimation(
        edge,
        cycleSpeed: speed,
        onUpdate: _updateEdgeColor);

    _rainbowAnimations[edge.Id] = animation;
    _animationManager.Start(animation);
  }

  /// <summary>
  /// Stops a rainbow effect for an edge.
  /// </summary>
  public void StopRainbowEffect(string edgeId)
  {
    if (_rainbowAnimations.TryGetValue(edgeId, out var animation))
    {
      _animationManager.Stop(animation);
      _rainbowAnimations.Remove(edgeId);
    }
  }

  /// <summary>
  /// Starts a pulsing opacity effect for an edge.
  /// </summary>
  public void StartPulseEffect(Edge edge, EdgeStyle style)
  {
    if (_pulseAnimations.ContainsKey(edge.Id))
    {
      StopPulseEffect(edge.Id);
    }

    var animation = new EdgePulseGlowAnimation(
        edge,
        minOpacity: style.PulseMinOpacity,
        maxOpacity: style.Opacity,
        frequency: style.PulseFrequency,
        onUpdate: _updateEdgeOpacity);

    _pulseAnimations[edge.Id] = animation;
    _animationManager.Start(animation);
  }

  /// <summary>
  /// Stops a pulse effect for an edge.
  /// </summary>
  public void StopPulseEffect(string edgeId)
  {
    if (_pulseAnimations.TryGetValue(edgeId, out var animation))
    {
      _animationManager.Stop(animation);
      _pulseAnimations.Remove(edgeId);
    }
  }

  /// <summary>
  /// Stops all effects.
  /// </summary>
  public void StopAllEffects()
  {
    foreach (var animation in _rainbowAnimations.Values.ToList())
    {
      _animationManager.Stop(animation);
    }
    _rainbowAnimations.Clear();

    foreach (var animation in _pulseAnimations.Values.ToList())
    {
      _animationManager.Stop(animation);
    }
    _pulseAnimations.Clear();
  }

  /// <summary>
  /// Checks if an edge has an active rainbow effect.
  /// </summary>
  public bool HasRainbowEffect(string edgeId) => _rainbowAnimations.ContainsKey(edgeId);

  /// <summary>
  /// Checks if an edge has an active pulse effect.
  /// </summary>
  public bool HasPulseEffect(string edgeId) => _pulseAnimations.ContainsKey(edgeId);

  /// <summary>
  /// Releases all resources.
  /// </summary>
  public void Dispose()
  {
    StopAllEffects();
  }
}
