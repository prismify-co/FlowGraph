using Avalonia.Controls;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Extended node renderer that supports data binding to port values.
/// Implement this for nodes with interactive controls (sliders, color pickers, etc.).
/// </summary>
public interface IDataNodeRenderer : INodeRenderer
{
    /// <summary>
    /// Creates the visual with data binding context.
    /// </summary>
    /// <param name="node">The node to render.</param>
    /// <param name="processor">The node's data processor (may be null for display-only nodes).</param>
    /// <param name="context">The render context.</param>
    /// <returns>A control bound to the node's data.</returns>
    Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context);

    /// <summary>
    /// Updates the visual when port values change.
    /// Called when connected input values change.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="processor">The node's processor.</param>
    void UpdateFromPortValues(Control visual, INodeProcessor processor);

    /// <summary>
    /// Called when the processor is attached to this renderer.
    /// Use this to set up event subscriptions.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="processor">The node's processor.</param>
    void OnProcessorAttached(Control visual, INodeProcessor processor);

    /// <summary>
    /// Called when the processor is detached from this renderer.
    /// Use this to clean up event subscriptions.
    /// </summary>
    /// <param name="visual">The node's visual control.</param>
    /// <param name="processor">The node's processor.</param>
    void OnProcessorDetached(Control visual, INodeProcessor processor);
}

/// <summary>
/// Base class for data-bound node renderers.
/// Provides common functionality for interactive nodes.
/// </summary>
public abstract class DataNodeRendererBase : DefaultNodeRenderer, IDataNodeRenderer
{
    /// <inheritdoc />
    public virtual Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        // Default implementation creates the base visual
        // Override in derived classes to add data-bound controls
        return base.CreateNodeVisual(node, context);
    }

    /// <inheritdoc />
    public virtual void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        // Default: no-op. Override to update visuals when values change.
    }

    /// <inheritdoc />
    public virtual void OnProcessorAttached(Control visual, INodeProcessor processor)
    {
        // Subscribe to all input value changes
        foreach (var input in processor.InputValues.Values)
        {
            input.ValueChanged += (_, _) => UpdateFromPortValues(visual, processor);
        }
    }

    /// <inheritdoc />
    public virtual void OnProcessorDetached(Control visual, INodeProcessor processor)
    {
        // Note: Event handlers will be garbage collected with the processor
        // For explicit cleanup, derived classes should track subscriptions
    }

    /// <summary>
    /// Gets the Node associated with a visual control.
    /// Handles both direct Tag assignment and ResizableVisual dictionary storage.
    /// Use this method instead of directly accessing visual.Tag for Node lookups.
    /// </summary>
    /// <param name="visual">The visual control to get the node from.</param>
    /// <returns>The associated Node, or null if not found.</returns>
    protected static Node? GetNodeFromVisual(Control visual)
    {
        return ResizableVisual.GetNodeFromTag(visual.Tag);
    }

    /// <summary>
    /// Finds a control by its Tag property.
    /// </summary>
    /// <typeparam name="T">The control type to find.</typeparam>
    /// <param name="root">The root control to search from.</param>
    /// <param name="tag">The tag value to match.</param>
    /// <returns>The found control, or null.</returns>
    protected static T? FindByTag<T>(Control root, string tag) where T : Control
    {
        if (root is T typed && typed.Tag as string == tag)
            return typed;

        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control control)
                {
                    var result = FindByTag<T>(control, tag);
                    if (result != null) return result;
                }
            }
        }
        else if (root is Decorator decorator && decorator.Child is Control decoratorChild)
        {
            return FindByTag<T>(decoratorChild, tag);
        }
        else if (root is ContentControl content && content.Content is Control contentChild)
        {
            return FindByTag<T>(contentChild, tag);
        }

        return null;
    }

    /// <summary>
    /// Finds all controls of a type within a visual tree.
    /// </summary>
    /// <typeparam name="T">The control type to find.</typeparam>
    /// <param name="root">The root control to search from.</param>
    /// <returns>All matching controls.</returns>
    protected static IEnumerable<T> FindAll<T>(Control root) where T : Control
    {
        if (root is T typed)
            yield return typed;

        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control control)
                {
                    foreach (var result in FindAll<T>(control))
                        yield return result;
                }
            }
        }
        else if (root is Decorator decorator && decorator.Child is Control decoratorChild)
        {
            foreach (var result in FindAll<T>(decoratorChild))
                yield return result;
        }
        else if (root is ContentControl content && content.Content is Control contentChild)
        {
            foreach (var result in FindAll<T>(contentChild))
                yield return result;
        }
    }
}
