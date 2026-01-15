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
    /// <remarks>
    /// <para>
    /// <b>IMPORTANT:</b> Implementations MUST update ALL child elements within the visual,
    /// not just the root container. Failing to update child elements will cause the visual
    /// to appear unchanged during resize operations until a full re-render occurs.
    /// </para>
    /// <para>
    /// For composite visuals (e.g., a Grid containing Rectangles, Borders, or Canvases),
    /// iterate through children and update their Width/Height properties accordingly.
    /// </para>
    /// <example>
    /// <code>
    /// public void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
    /// {
    ///     var scaledWidth = width * context.Scale;
    ///     var scaledHeight = height * context.Scale;
    ///     
    ///     if (visual is Grid grid)
    ///     {
    ///         grid.Width = scaledWidth;
    ///         grid.Height = scaledHeight;
    ///         
    ///         // CRITICAL: Also update all child elements
    ///         foreach (var child in grid.Children)
    ///         {
    ///             if (child is Rectangle rect)
    ///             {
    ///                 rect.Width = scaledWidth;
    ///                 rect.Height = scaledHeight;
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
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

    /// <summary>
    /// Indicates whether this renderer creates composite visuals with multiple children
    /// that need individual size updates. Used for validation and debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If true, the framework may validate that UpdateSize properly handles child elements.
    /// Renderers should return true if they create visuals with backgrounds, borders,
    /// content areas, or other elements that need size updates beyond the root container.
    /// </para>
    /// <para>
    /// Consider using <see cref="ResizableVisual"/> to automatically track and update children.
    /// </para>
    /// </remarks>
    bool HasCompositeVisual => false;
}

/// <summary>
/// Marker interface for renderers that use <see cref="ResizableVisual"/> for automatic
/// child element updates. Implementing this interface signals that the renderer follows
/// the recommended pattern for composite visuals.
/// </summary>
/// <remarks>
/// When a renderer implements this interface, the framework knows that UpdateSize
/// will properly update all child elements via the ResizableVisual metadata.
/// This enables validation, debugging, and potential optimizations.
/// </remarks>
public interface IResizableNodeRenderer : INodeRenderer
{
    // Marker interface - no additional members required.
    // Implementation implies use of ResizableVisual.Create().Build() in CreateNodeVisual
    // and ResizableVisual.UpdateSize() in UpdateSize.
}

/// <summary>
/// Context information passed to node renderers.
/// 
/// <para><b>TRANSFORM-BASED RENDERING:</b></para>
/// <para>
/// <see cref="Scale"/> is always 1.0 - create visuals at logical size.
/// The MatrixTransform on MainCanvas handles zoom. This enables O(1) zoom.
/// </para>
/// </summary>
public class NodeRenderContext
{
    public required ThemeResources Theme { get; init; }
    public required FlowCanvasSettings Settings { get; init; }
    
    /// <summary>
    /// Logical scale for visual sizing. Always 1.0 in transform-based rendering.
    /// </summary>
    public required double Scale { get; init; }
    
    /// <summary>
    /// Actual viewport zoom level. Use for calculations, not visual sizing.
    /// </summary>
    public double ViewportZoom { get; init; } = 1.0;
    
    /// <summary>
    /// Inverse scale for constant-size elements (1/ViewportZoom).
    /// Apply as ScaleTransform to elements that should stay same screen size.
    /// </summary>
    public double InverseScale => 1.0 / ViewportZoom;
}
