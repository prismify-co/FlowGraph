using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Unified graph render service that abstracts the difference between
/// retained mode (CanvasElementManager) and direct rendering mode (DirectCanvasRenderer).
/// 
/// This service ensures that all rendering operations work correctly regardless
/// of which rendering mode is active, solving the recurring issue of handlers
/// only implementing one rendering path.
/// </summary>
public class CanvasRenderService : ICanvasRenderService
{
  private readonly CanvasElementManager _retainedRenderer;
  private readonly Func<DirectCanvasRenderer?> _getDirectRenderer;
  private readonly Func<bool> _getIsDirectRenderingMode;
  private readonly Action _renderEdgesAction;
  private readonly Action _refreshAction;
  private readonly Func<ThemeResources> _getTheme;

  /// <summary>
  /// Creates a new GraphRenderService.
  /// </summary>
  /// <param name="retainedRenderer">The retained mode (visual tree) renderer.</param>
  /// <param name="getDirectRenderer">Function to get the direct renderer (may be null if not initialized).</param>
  /// <param name="getIsDirectRenderingMode">Function to check if direct rendering mode is active.</param>
  /// <param name="renderEdgesAction">Action to re-render edges.</param>
  /// <param name="refreshAction">Action to force a full refresh.</param>
  /// <param name="getTheme">Function to get the current theme resources.</param>
  public CanvasRenderService(
    CanvasElementManager retainedRenderer,
    Func<DirectCanvasRenderer?> getDirectRenderer,
    Func<bool> getIsDirectRenderingMode,
    Action renderEdgesAction,
    Action refreshAction,
    Func<ThemeResources> getTheme)
  {
    _retainedRenderer = retainedRenderer ?? throw new ArgumentNullException(nameof(retainedRenderer));
    _getDirectRenderer = getDirectRenderer ?? throw new ArgumentNullException(nameof(getDirectRenderer));
    _getIsDirectRenderingMode = getIsDirectRenderingMode ?? throw new ArgumentNullException(nameof(getIsDirectRenderingMode));
    _renderEdgesAction = renderEdgesAction ?? throw new ArgumentNullException(nameof(renderEdgesAction));
    _refreshAction = refreshAction ?? throw new ArgumentNullException(nameof(refreshAction));
    _getTheme = getTheme ?? throw new ArgumentNullException(nameof(getTheme));
  }

  /// <inheritdoc />
  public bool IsDirectRenderingMode => _getIsDirectRenderingMode();

  /// <inheritdoc />
  public void UpdateNodeSize(Node node)
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: trigger a full re-render
      // InvalidateVisual() alone doesn't work - we need RenderAll() to actually redraw
      _refreshAction();
    }
    else
    {
      // Retained mode: update the visual control
      _retainedRenderer.UpdateNodeSize(node, _getTheme());
    }
  }

  /// <inheritdoc />
  public void UpdateNodePosition(Node node)
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: trigger a full re-render
      _refreshAction();
    }
    else
    {
      // Retained mode: update the visual position
      _retainedRenderer.UpdateNodePosition(node);
    }
  }

  /// <inheritdoc />
  public void UpdateNodeSelection(Node node)
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: trigger a full re-render
      _refreshAction();
    }
    else
    {
      // Retained mode: update selection visual
      _retainedRenderer.UpdateNodeSelection(node, _getTheme());
    }
  }

  /// <inheritdoc />
  public void UpdateNodeStyle(Node node)
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: trigger a full re-render
      _refreshAction();
    }
    else
    {
      // Retained mode: update custom styling
      _retainedRenderer.UpdateNodeStyle(node, _getTheme());
    }
  }

  /// <inheritdoc />
  public void UpdateResizeHandlePositions(Node node)
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: handles are drawn each frame, trigger re-render
      _refreshAction();
    }
    else
    {
      // Retained mode: reposition handle controls
      _retainedRenderer.UpdateResizeHandlePositions(node);
    }
  }

  /// <inheritdoc />
  public void UpdateNodeAfterResize(Node node)
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: single refresh is more efficient than multiple calls
      _refreshAction();
    }
    else
    {
      // Retained mode: update all visual aspects individually
      _retainedRenderer.UpdateNodeSize(node, _getTheme());
      _retainedRenderer.UpdateNodePosition(node);
      _retainedRenderer.UpdateResizeHandlePositions(node);
      _renderEdgesAction();
    }
  }

  /// <inheritdoc />
  public void Refresh()
  {
    _refreshAction();
  }

  /// <inheritdoc />
  public void Invalidate()
  {
    if (IsDirectRenderingMode)
    {
      // Direct rendering: just mark as needing repaint
      // Avalonia will batch multiple invalidations into one Render() call
      _getDirectRenderer()?.InvalidateVisual();
    }
    // Retained mode: no-op, the visual tree automatically handles invalidation
    // when control properties change
  }

  /// <inheritdoc />
  public void RenderEdges()
  {
    _renderEdgesAction();
  }
}
