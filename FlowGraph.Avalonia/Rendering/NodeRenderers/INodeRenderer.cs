using Avalonia.Controls;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Interface for custom node renderers.
/// Implement this interface to create custom visual representations for node types.
/// </summary>
public interface INodeRenderer
{
    /// <summary>
    /// Creates the visual representation for a node.
    /// </summary>
    /// <param name="node">The node data to render.</param>
    /// <param name="context">The rendering context with theme, scale, and settings.</param>
    /// <returns>A Control representing the node visual.</returns>
    Control CreateNodeVisual(Node node, NodeRenderContext context);

    /// <summary>
    /// Updates the visual state when selection changes.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="node">The node data.</param>
    /// <param name="context">The rendering context.</param>
    void UpdateSelection(Control visual, Node node, NodeRenderContext context);

    /// <summary>
    /// Updates the size of an existing node visual.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="node">The node data.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="width">The new width in canvas units.</param>
    /// <param name="height">The new height in canvas units.</param>
    void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height);

    /// <summary>
    /// Gets the default width of the node in canvas units (before scaling).
    /// Return null to use the default width from settings.
    /// </summary>
    double? GetWidth(Node node, FlowCanvasSettings settings);

    /// <summary>
    /// Gets the default height of the node in canvas units (before scaling).
    /// Return null to use the default height from settings.
    /// </summary>
    double? GetHeight(Node node, FlowCanvasSettings settings);

    /// <summary>
    /// Gets the minimum width for resizing. Return null for no minimum.
    /// </summary>
    double? GetMinWidth(Node node, FlowCanvasSettings settings) => 60;

    /// <summary>
    /// Gets the minimum height for resizing. Return null for no minimum.
    /// </summary>
    double? GetMinHeight(Node node, FlowCanvasSettings settings) => 40;
}

/// <summary>
/// Context information passed to node renderers.
/// </summary>
public class NodeRenderContext
{
    public required ThemeResources Theme { get; init; }
    public required FlowCanvasSettings Settings { get; init; }
    public required double Scale { get; init; }
}
