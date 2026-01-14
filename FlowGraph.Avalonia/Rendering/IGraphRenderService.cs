using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Unified interface for graph rendering operations that abstracts away
/// the difference between retained mode (VisualTree) and direct rendering modes.
/// This prevents the recurring issue of handlers only implementing one rendering path.
/// </summary>
public interface IGraphRenderService
{
  /// <summary>
  /// Gets whether the renderer is currently in direct rendering mode.
  /// </summary>
  bool IsDirectRenderingMode { get; }

  /// <summary>
  /// Updates the visual size of a node. Works in both rendering modes.
  /// </summary>
  /// <param name="node">The node to update.</param>
  void UpdateNodeSize(Node node);

  /// <summary>
  /// Updates the visual position of a node. Works in both rendering modes.
  /// </summary>
  /// <param name="node">The node to update.</param>
  void UpdateNodePosition(Node node);

  /// <summary>
  /// Updates the selection state of a node. Works in both rendering modes.
  /// </summary>
  /// <param name="node">The node to update.</param>
  void UpdateNodeSelection(Node node);

  /// <summary>
  /// Updates the resize handle positions for a node. Works in both rendering modes.
  /// </summary>
  /// <param name="node">The node to update handles for.</param>
  void UpdateResizeHandlePositions(Node node);

  /// <summary>
  /// Updates all visual aspects of a node after a resize operation.
  /// This is more efficient than calling individual update methods.
  /// In direct mode, this triggers a single refresh.
  /// In retained mode, this updates size, position, handles, and edges.
  /// </summary>
  /// <param name="node">The node that was resized.</param>
  void UpdateNodeAfterResize(Node node);

  /// <summary>
  /// Forces an immediate refresh/re-render of all graph elements.
  /// Use sparingly as this can be expensive.
  /// </summary>
  void Refresh();

  /// <summary>
  /// Invalidates the render and schedules a repaint.
  /// For retained mode, this updates visuals. For direct mode, this triggers InvalidateVisual().
  /// </summary>
  void Invalidate();

  /// <summary>
  /// Re-renders all edges. Call this after node position/size changes.
  /// </summary>
  void RenderEdges();
}
