using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Helper methods for updating node visuals consistently.
/// Use these methods in INodeRenderer.UpdateSize implementations to ensure
/// all child elements are properly updated.
/// </summary>
public static class NodeVisualHelper
{
    /// <summary>
    /// Recursively updates the size of a visual and all its children.
    /// This is a best-effort update that handles common Avalonia controls.
    /// </summary>
    /// <param name="visual">The root visual to update.</param>
    /// <param name="width">The new width in screen units (already scaled).</param>
    /// <param name="height">The new height in screen units (already scaled).</param>
    /// <param name="recursive">If true, recursively updates child containers.</param>
    /// <remarks>
    /// This helper updates Width/Height on common container types and shapes.
    /// For complex visuals with custom layout logic, you may need to implement
    /// custom update logic in addition to using this helper.
    /// </remarks>
    public static void UpdateVisualSize(Control visual, double width, double height, bool recursive = false)
    {
        // Update the visual itself
        SetSize(visual, width, height);

        if (!recursive) return;

        // Recursively update children for container types
        switch (visual)
        {
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    if (child is Control childControl)
                    {
                        UpdateVisualSize(childControl, width, height, recursive: false);
                    }
                }
                break;

            case Border border when border.Child is Control borderChild:
                UpdateVisualSize(borderChild, width, height, recursive: false);
                break;

            case Decorator decorator when decorator.Child is Control decoratorChild:
                UpdateVisualSize(decoratorChild, width, height, recursive: false);
                break;
        }
    }

    /// <summary>
    /// Updates only direct Shape children within a container to the specified size.
    /// Useful for updating background/border rectangles without affecting content.
    /// </summary>
    /// <param name="container">The container to search for shapes.</param>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public static void UpdateShapeChildren(Control container, double width, double height)
    {
        if (container is not Panel panel) return;

        foreach (var child in panel.Children)
        {
            if (child is Shape shape)
            {
                SetSize(shape, width, height);

                // Update corner radius for rectangles
                if (shape is Rectangle rect && rect.RadiusX > 0)
                {
                    // Preserve aspect ratio of corner radius relative to size
                    // This is a heuristic - may need adjustment for specific cases
                }
            }
        }
    }

    /// <summary>
    /// Sets Width and Height on a control if it's a supported type.
    /// </summary>
    private static void SetSize(Control control, double width, double height)
    {
        switch (control)
        {
            case Shape shape:
                shape.Width = width;
                shape.Height = height;
                break;

            case Border border:
                border.Width = width;
                border.Height = height;
                break;

            case Panel panel:
                panel.Width = width;
                panel.Height = height;
                break;

            case ContentControl cc:
                cc.Width = width;
                cc.Height = height;
                break;
        }
    }

    /// <summary>
    /// Finds a child control by its Tag value.
    /// </summary>
    /// <typeparam name="T">The expected control type.</typeparam>
    /// <param name="parent">The parent container to search.</param>
    /// <param name="tag">The tag value to match.</param>
    /// <returns>The matching control, or null if not found.</returns>
    public static T? FindByTag<T>(Control parent, object tag) where T : Control
    {
        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is T typed && Equals(typed.Tag, tag))
                    return typed;
            }
        }
        else if (parent is Border { Child: Control borderChild })
        {
            if (borderChild is T typed && Equals(typed.Tag, tag))
                return typed;
            return FindByTag<T>(borderChild, tag);
        }
        else if (parent is Decorator { Child: Control decoratorChild })
        {
            if (decoratorChild is T typed && Equals(typed.Tag, tag))
                return typed;
            return FindByTag<T>(decoratorChild, tag);
        }

        return null;
    }
}
