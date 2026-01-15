using FlowGraph.Core;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Interface for providing snap offsets during node drag operations.
/// External systems (like helper lines, guides, magnetic snapping) can implement
/// this interface to influence node positions during drag without directly
/// modifying node positions themselves.
/// </summary>
/// <remarks>
/// This follows the "single authority" pattern where the drag system remains
/// the sole authority for setting node positions. Snap providers suggest offsets,
/// and the drag system decides whether and how to apply them.
/// </remarks>
public interface ISnapProvider
{
  /// <summary>
  /// Calculate a snap offset for nodes being dragged.
  /// </summary>
  /// <param name="nodes">The nodes currently being dragged.</param>
  /// <param name="proposedPosition">The position the drag system is proposing for the primary node
  /// (after applying delta from drag start and any grid snapping).</param>
  /// <returns>
  /// A snap offset to apply to all dragged nodes, or null if no snapping should occur.
  /// The offset is relative to the proposed position (e.g., return (5, 0) to shift 5 pixels right).
  /// </returns>
  Point? GetSnapOffset(IReadOnlyList<Node> nodes, Point proposedPosition);

  /// <summary>
  /// Called when a drag operation starts.
  /// </summary>
  /// <param name="nodes">The nodes being dragged.</param>
  /// <param name="startPosition">The starting canvas position of the drag.</param>
  void OnDragStart(IReadOnlyList<Node> nodes, Point startPosition);

  /// <summary>
  /// Called when a drag operation ends.
  /// </summary>
  /// <param name="nodes">The nodes that were dragged.</param>
  /// <param name="cancelled">Whether the drag was cancelled.</param>
  void OnDragEnd(IReadOnlyList<Node> nodes, bool cancelled);
}
