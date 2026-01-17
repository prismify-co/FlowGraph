using Avalonia.Controls;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Inline label editing methods for nodes.
/// </summary>
public partial class NodeVisualManager
{
    /// <summary>
    /// Begins inline editing for a node's label.
    /// </summary>
    /// <param name="node">The node to edit.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onCommit">Callback when editing is committed with the new label value.</param>
    /// <param name="onCancel">Callback when editing is cancelled.</param>
    /// <returns>True if edit mode was started successfully.</returns>
    public bool BeginEditLabel(Node node, ThemeResources theme, Action<string> onCommit, Action onCancel)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var control))
            return false;

        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        if (renderer is not IEditableNodeRenderer editableRenderer)
            return false;

        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = _renderContext.Scale
        };

        editableRenderer.BeginEdit(control, node, context, onCommit, onCancel);
        return true;
    }

    /// <summary>
    /// Ends inline editing for a node's label.
    /// </summary>
    /// <param name="node">The node being edited.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void EndEditLabel(Node node, ThemeResources theme)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var control))
            return;

        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        if (renderer is not IEditableNodeRenderer editableRenderer)
            return;

        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = _renderContext.Scale
        };

        editableRenderer.EndEdit(control, node, context);
    }

    /// <summary>
    /// Gets whether a node is currently in edit mode.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is being edited.</returns>
    public bool IsEditingLabel(Node node)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var control))
            return false;

        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        if (renderer is not IEditableNodeRenderer editableRenderer)
            return false;

        return editableRenderer.IsEditing(control);
    }

    /// <summary>
    /// Checks if a node's renderer supports inline editing.
    /// </summary>
    /// <param name="nodeType">The node type to check.</param>
    /// <returns>True if the renderer supports editing.</returns>
    public bool SupportsEditing(string? nodeType)
    {
        var renderer = _nodeRendererRegistry.GetRenderer(nodeType);
        return renderer is IEditableNodeRenderer;
    }
}
