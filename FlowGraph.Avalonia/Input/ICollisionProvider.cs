using FlowGraph.Core;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Interface for providing collision detection and prevention during node drag operations.
/// External systems (like collision managers) can implement this interface to prevent
/// nodes from overlapping during drag without directly modifying node positions themselves.
/// </summary>
/// <remarks>
/// This follows the "single authority" pattern where the drag system remains
/// the sole authority for setting node positions. Collision providers suggest offsets
/// to prevent overlap, and the drag system applies them after snap offsets.
/// 
/// Semantic difference from <see cref="ISnapProvider"/>:
/// - Snap = attraction (guide lines pull nodes toward alignment)
/// - Collision = repulsion (prevents nodes from overlapping)
/// </remarks>
public interface ICollisionProvider
{
    /// <summary>
    /// Calculate an offset to prevent collision for nodes being dragged.
    /// </summary>
    /// <param name="nodes">The nodes currently being dragged.</param>
    /// <param name="proposedPosition">The position the drag system is proposing for the primary node
    /// (after applying delta, grid snapping, and snap provider offset).</param>
    /// <returns>
    /// An offset to apply to prevent collision, or null if no collision would occur.
    /// The offset is relative to the proposed position.
    /// </returns>
    Point? GetCollisionOffset(IReadOnlyList<Node> nodes, Point proposedPosition);

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
