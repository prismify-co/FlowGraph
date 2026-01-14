using Avalonia.Controls;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.BackgroundRenderers;

/// <summary>
/// Manages registration and rendering of custom background renderers.
/// </summary>
/// <remarks>
/// <para>
/// Background renderers are rendered in registration order, allowing layered
/// backgrounds. The registry supports both multiple concurrent renderers and
/// a single-renderer mode for simpler use cases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var registry = new BackgroundRendererRegistry();
/// 
/// // Add multiple renderers (rendered in order)
/// registry.Add(new GradientBackgroundRenderer());
/// registry.Add(new LifelineRenderer());
/// 
/// // Or use single mode
/// registry.SetSingle(new SwimlaneBackgroundRenderer());
/// </code>
/// </example>
public class BackgroundRendererRegistry
{
  private readonly List<IBackgroundRenderer> _renderers = new();
  private IBackgroundRenderer? _singleRenderer;
  private bool _useSingleMode;
  private Canvas? _lastCanvas;

  /// <summary>
  /// Gets whether there are any background renderers registered.
  /// </summary>
  public bool HasRenderers => _useSingleMode ? _singleRenderer != null : _renderers.Count > 0;

  /// <summary>
  /// Adds a background renderer to the registry.
  /// Renderers are called in the order they were added.
  /// </summary>
  /// <param name="renderer">The renderer to add.</param>
  public void Add(IBackgroundRenderer renderer)
  {
    ArgumentNullException.ThrowIfNull(renderer);
    _useSingleMode = false;
    _renderers.Add(renderer);
  }

  /// <summary>
  /// Removes a background renderer from the registry.
  /// </summary>
  /// <param name="renderer">The renderer to remove.</param>
  /// <returns>True if the renderer was found and removed.</returns>
  public bool Remove(IBackgroundRenderer renderer)
  {
    var removed = _renderers.Remove(renderer);
    if (removed && _lastCanvas != null)
    {
      renderer.Cleanup(_lastCanvas);
    }
    return removed;
  }

  /// <summary>
  /// Sets a single background renderer, replacing any existing renderers.
  /// </summary>
  /// <param name="renderer">The renderer to use, or null to clear.</param>
  public void SetSingle(IBackgroundRenderer? renderer)
  {
    // Cleanup old renderers before replacing
    CleanupAllRenderers();

    _useSingleMode = true;
    _singleRenderer = renderer;
    _renderers.Clear();
  }

  /// <summary>
  /// Clears all registered background renderers.
  /// </summary>
  public void Clear()
  {
    // Cleanup all renderers before clearing
    CleanupAllRenderers();

    _renderers.Clear();
    _singleRenderer = null;
    _useSingleMode = false;
  }

  /// <summary>
  /// Cleans up all currently registered renderers.
  /// </summary>
  private void CleanupAllRenderers()
  {
    if (_lastCanvas == null) return;

    if (_useSingleMode)
    {
      _singleRenderer?.Cleanup(_lastCanvas);
    }
    else
    {
      foreach (var renderer in _renderers)
      {
        renderer.Cleanup(_lastCanvas);
      }
    }
  }

  /// <summary>
  /// Renders all registered backgrounds.
  /// </summary>
  /// <param name="canvas">The canvas to render to.</param>
  /// <param name="context">The rendering context.</param>
  public void Render(Canvas canvas, BackgroundRenderContext context)
  {
    _lastCanvas = canvas;

    if (_useSingleMode)
    {
      _singleRenderer?.Render(canvas, context);
    }
    else
    {
      foreach (var renderer in _renderers)
      {
        renderer.Render(canvas, context);
      }
    }
  }

  /// <summary>
  /// Notifies all renderers of a graph change.
  /// </summary>
  /// <param name="graph">The updated graph.</param>
  public void OnGraphChanged(Graph? graph)
  {
    if (_useSingleMode)
    {
      _singleRenderer?.OnGraphChanged(graph);
    }
    else
    {
      foreach (var renderer in _renderers)
      {
        renderer.OnGraphChanged(graph);
      }
    }
  }

  /// <summary>
  /// Notifies all renderers of a viewport change.
  /// </summary>
  /// <param name="context">The updated context.</param>
  public void OnViewportChanged(BackgroundRenderContext context)
  {
    if (_useSingleMode)
    {
      _singleRenderer?.OnViewportChanged(context);
    }
    else
    {
      foreach (var renderer in _renderers)
      {
        renderer.OnViewportChanged(context);
      }
    }
  }
}
