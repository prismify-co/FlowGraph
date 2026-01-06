using Avalonia;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Interface for custom node renderers that support direct DrawingContext rendering.
/// Implement this interface in addition to <see cref="INodeRenderer"/> to provide
/// custom drawing logic for the high-performance DirectGraphRenderer mode.
/// </summary>
/// <remarks>
/// <para>
/// When DirectGraphRenderer renders a node, it first checks if the node's renderer
/// implements IDirectNodeRenderer. If so, it delegates to <see cref="DrawNode"/>.
/// Otherwise, it uses the built-in default drawing logic.
/// </para>
/// <para>
/// Implementations should:
/// - Draw node background and border
/// - Draw node label (unless editingNodeId matches)
/// - NOT draw ports (handled by DirectGraphRenderer for consistency)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyCustomRenderer : INodeRenderer, IDirectNodeRenderer
/// {
///     // INodeRenderer implementation for visual tree mode
///     public Control CreateNodeVisual(Node node, NodeRenderContext context) { ... }
///     
///     // IDirectNodeRenderer implementation for direct mode
///     public void DrawNode(DrawingContext context, Node node, DirectNodeRenderContext ctx)
///     {
///         // Draw custom shape
///         var geometry = new EllipseGeometry(ctx.ScreenBounds);
///         context.DrawGeometry(ctx.Background, ctx.BorderPen, geometry);
///         
///         // Draw label
///         if (!ctx.IsEditing)
///         {
///             // Draw text...
///         }
///     }
/// }
/// </code>
/// </example>
public interface IDirectNodeRenderer
{
    /// <summary>
    /// Draws the node directly to a DrawingContext.
    /// </summary>
    /// <param name="context">The Avalonia drawing context.</param>
    /// <param name="node">The node to draw.</param>
    /// <param name="renderContext">Context with pre-calculated screen bounds, brushes, and state.</param>
    void DrawNode(DrawingContext context, Node node, DirectNodeRenderContext renderContext);
}

/// <summary>
/// Context passed to <see cref="IDirectNodeRenderer.DrawNode"/> with pre-calculated values.
/// </summary>
public class DirectNodeRenderContext
{
    /// <summary>
    /// The node bounds in screen coordinates (already transformed by zoom/pan).
    /// </summary>
    public required Rect ScreenBounds { get; init; }

    /// <summary>
    /// The current zoom level.
    /// </summary>
    public required double Zoom { get; init; }

    /// <summary>
    /// Whether the node is currently selected.
    /// </summary>
    public required bool IsSelected { get; init; }

    /// <summary>
    /// Whether this node's label is currently being edited (don't draw the label).
    /// </summary>
    public required bool IsEditing { get; init; }

    /// <summary>
    /// The background brush for the node.
    /// </summary>
    public required IBrush? Background { get; init; }

    /// <summary>
    /// The border pen for the node (changes based on selection state).
    /// </summary>
    public required Pen? BorderPen { get; init; }

    /// <summary>
    /// The brush for drawing text.
    /// </summary>
    public required IBrush TextBrush { get; init; }

    /// <summary>
    /// The theme resources for additional styling.
    /// </summary>
    public required ThemeResources Theme { get; init; }

    /// <summary>
    /// The canvas settings.
    /// </summary>
    public required FlowCanvasSettings Settings { get; init; }

    /// <summary>
    /// The shared render model for geometry calculations.
    /// </summary>
    public required GraphRenderModel Model { get; init; }
}
