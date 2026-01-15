using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// A wrapper that tracks resizable child elements within a node visual.
/// When UpdateSize is called, all registered children are automatically updated.
/// 
/// Use this in CreateNodeVisual to ensure UpdateSize properly propagates to all children.
/// </summary>
/// <example>
/// <code>
/// public Control CreateNodeVisual(Node node, NodeRenderContext context)
/// {
///     var grid = new Grid { Width = 200, Height = 100 };
///     var background = new Rectangle { Width = 200, Height = 100 };
///     var border = new Rectangle { Width = 200, Height = 100 };
///     
///     grid.Children.Add(background);
///     grid.Children.Add(border);
///     
///     // Register children that need resizing - this is the key!
///     return ResizableVisual.Create(grid)
///         .WithFullSizeChild(background)
///         .WithFullSizeChild(border)
///         .Build();
/// }
/// 
/// public void UpdateSize(Control visual, Node node, NodeRenderContext context, double w, double h)
/// {
///     // One line - all registered children are automatically updated!
///     ResizableVisual.UpdateSize(visual, w * context.Scale, h * context.Scale);
/// }
/// </code>
/// </example>
public class ResizableVisual
{
    /// <summary>
    /// Tag key used to store resize metadata on the visual.
    /// </summary>
    public const string MetadataKey = "__ResizableVisual";

    /// <summary>
    /// Metadata attached to the root visual that tracks child resize requirements.
    /// </summary>
    public class ResizeMetadata
    {
        /// <summary>
        /// Children that should be resized to match the full parent size.
        /// </summary>
        public List<Control> FullSizeChildren { get; } = new();

        /// <summary>
        /// Children with custom resize logic (receives width, height, scale).
        /// </summary>
        public List<(Control Control, Action<Control, double, double> Updater)> CustomChildren { get; } = new();

        /// <summary>
        /// Children that should be resized with insets (padding/margins).
        /// </summary>
        public List<(Control Control, Thickness Insets)> InsetChildren { get; } = new();
    }

    private readonly Control _root;
    private readonly ResizeMetadata _metadata;

    private ResizableVisual(Control root)
    {
        _root = root;
        _metadata = new ResizeMetadata();
    }

    /// <summary>
    /// Creates a new ResizableVisual builder for the given root control.
    /// </summary>
    public static ResizableVisual Create(Control root) => new(root);

    /// <summary>
    /// Registers a child that should be resized to match the full parent size.
    /// </summary>
    public ResizableVisual WithFullSizeChild(Control child)
    {
        _metadata.FullSizeChildren.Add(child);
        return this;
    }

    /// <summary>
    /// Registers multiple children that should be resized to match the full parent size.
    /// </summary>
    public ResizableVisual WithFullSizeChildren(params Control[] children)
    {
        _metadata.FullSizeChildren.AddRange(children);
        return this;
    }

    /// <summary>
    /// Registers a child with inset margins (content area sizing).
    /// </summary>
    /// <param name="child">The child control.</param>
    /// <param name="insets">Thickness representing padding/margins to subtract from full size.</param>
    public ResizableVisual WithInsetChild(Control child, Thickness insets)
    {
        _metadata.InsetChildren.Add((child, insets));
        return this;
    }

    /// <summary>
    /// Registers a child with custom resize logic.
    /// </summary>
    /// <param name="child">The child control.</param>
    /// <param name="updater">Action that receives (control, scaledWidth, scaledHeight).</param>
    public ResizableVisual WithCustomChild(Control child, Action<Control, double, double> updater)
    {
        _metadata.CustomChildren.Add((child, updater));
        return this;
    }

    /// <summary>
    /// Associates a Node with this visual for event handling.
    /// The node will be stored in the tag dictionary and can be retrieved via GetNodeFromTag.
    /// </summary>
    /// <param name="node">The node to associate with this visual.</param>
    public ResizableVisual WithNode(FlowGraph.Core.Node node)
    {
        _node = node;
        return this;
    }

    private FlowGraph.Core.Node? _node;

    /// <summary>
    /// Builds and returns the root control with resize metadata attached.
    /// </summary>
    public Control Build()
    {
        // Store metadata using attached property pattern via Tag
        // We use a dictionary to allow multiple tags
        if (_root.Tag is Dictionary<string, object> existingTags)
        {
            existingTags[MetadataKey] = _metadata;
            if (_node != null)
                existingTags["Node"] = _node;
        }
        else if (_root.Tag != null)
        {
            // Preserve existing tag
            var dict = new Dictionary<string, object>
            {
                ["OriginalTag"] = _root.Tag,
                [MetadataKey] = _metadata
            };
            if (_node != null)
                dict["Node"] = _node;
            _root.Tag = dict;
        }
        else
        {
            var dict = new Dictionary<string, object>
            {
                [MetadataKey] = _metadata
            };
            if (_node != null)
                dict["Node"] = _node;
            _root.Tag = dict;
        }

        return _root;
    }

    /// <summary>
    /// Updates the size of a visual and all its registered children.
    /// Call this from INodeRenderer.UpdateSize.
    /// </summary>
    /// <param name="visual">The visual returned from CreateNodeVisual.</param>
    /// <param name="scaledWidth">Width in screen units (already multiplied by scale).</param>
    /// <param name="scaledHeight">Height in screen units (already multiplied by scale).</param>
    /// <returns>True if resize metadata was found and applied, false if fallback behavior was used.</returns>
    public static bool UpdateSize(Control visual, double scaledWidth, double scaledHeight)
    {
        // Update root control
        SetControlSize(visual, scaledWidth, scaledHeight);

        // Look for metadata
        var metadata = GetMetadata(visual);
        if (metadata == null)
        {
            // No metadata - caller should handle manually
            return false;
        }

        // Update full-size children
        foreach (var child in metadata.FullSizeChildren)
        {
            SetControlSize(child, scaledWidth, scaledHeight);
        }

        // Update inset children
        foreach (var (child, insets) in metadata.InsetChildren)
        {
            var insetWidth = scaledWidth - insets.Left - insets.Right;
            var insetHeight = scaledHeight - insets.Top - insets.Bottom;
            SetControlSize(child, Math.Max(0, insetWidth), Math.Max(0, insetHeight));
        }

        // Update custom children
        foreach (var (child, updater) in metadata.CustomChildren)
        {
            updater(child, scaledWidth, scaledHeight);
        }

        return true;
    }

    /// <summary>
    /// Gets the resize metadata from a visual, if present.
    /// </summary>
    public static ResizeMetadata? GetMetadata(Control visual)
    {
        if (visual.Tag is Dictionary<string, object> tags &&
            tags.TryGetValue(MetadataKey, out var value) &&
            value is ResizeMetadata metadata)
        {
            return metadata;
        }
        return null;
    }

    /// <summary>
    /// Checks if a visual has resize metadata attached.
    /// Useful for validation/debugging.
    /// </summary>
    public static bool HasResizeMetadata(Control visual) => GetMetadata(visual) != null;

    /// <summary>
    /// Extracts the Node from a control's Tag, handling both direct assignment
    /// and dictionary storage (used when ResizableVisual metadata is present).
    /// </summary>
    /// <param name="tag">The Tag value from a control.</param>
    /// <returns>The Node if found, null otherwise.</returns>
    public static FlowGraph.Core.Node? GetNodeFromTag(object? tag)
    {
        if (tag is FlowGraph.Core.Node node)
        {
            return node;
        }

        if (tag is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("Node", out var nodeObj) && nodeObj is FlowGraph.Core.Node dictNode)
            {
                return dictNode;
            }
            // Also check OriginalTag for backwards compatibility
            if (dict.TryGetValue("OriginalTag", out var origObj) && origObj is FlowGraph.Core.Node origNode)
            {
                return origNode;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a control's Tag contains a Node (either directly or in dictionary).
    /// </summary>
    public static bool TagContainsNode(object? tag) => GetNodeFromTag(tag) != null;

    /// <summary>
    /// Sets the size on a control based on its type.
    /// </summary>
    private static void SetControlSize(Control control, double width, double height)
    {
        switch (control)
        {
            case Shape shape:
                shape.Width = width;
                shape.Height = height;
                break;
            case Panel panel:
                panel.Width = width;
                panel.Height = height;
                break;
            case Border border:
                border.Width = width;
                border.Height = height;
                break;
            case ContentControl cc:
                cc.Width = width;
                cc.Height = height;
                break;
            default:
                // Fallback: try setting via reflection or just Width/Height properties
                control.Width = width;
                control.Height = height;
                break;
        }
    }
}
