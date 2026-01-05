using Avalonia.Controls;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Interface for node renderers that support inline label editing.
/// </summary>
public interface IEditableNodeRenderer : INodeRenderer
{
    /// <summary>
    /// Enters edit mode for the node's label.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="node">The node being edited.</param>
    /// <param name="context">The render context.</param>
    /// <param name="onCommit">Callback when editing is committed (Enter pressed or focus lost).</param>
    /// <param name="onCancel">Callback when editing is cancelled (Escape pressed).</param>
    void BeginEdit(Control visual, Node node, NodeRenderContext context, Action<string> onCommit, Action onCancel);

    /// <summary>
    /// Exits edit mode and returns to display mode.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="node">The node being edited.</param>
    /// <param name="context">The render context.</param>
    void EndEdit(Control visual, Node node, NodeRenderContext context);

    /// <summary>
    /// Gets whether the node is currently in edit mode.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <returns>True if the node is in edit mode.</returns>
    bool IsEditing(Control visual);
}
