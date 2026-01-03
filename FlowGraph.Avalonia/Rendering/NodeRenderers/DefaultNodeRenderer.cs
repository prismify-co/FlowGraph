using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Default node renderer that creates a standard bordered box with text.
/// </summary>
public class DefaultNodeRenderer : INodeRenderer
{
    public virtual Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var theme = context.Theme;
        var scale = context.Scale;
        var settings = context.Settings;

        var width = node.Width ?? GetWidth(node, settings) ?? settings.NodeWidth;
        var height = node.Height ?? GetHeight(node, settings) ?? settings.NodeHeight;

        var scaledWidth = width * scale;
        var scaledHeight = height * scale;

        var nodeBackground = theme.NodeBackground;
        var nodeBorder = node.IsSelected ? theme.NodeSelectedBorder : theme.NodeBorder;
        var nodeText = theme.NodeText;

        var border = new Border
        {
            Width = scaledWidth,
            Height = scaledHeight,
            Background = nodeBackground,
            BorderBrush = nodeBorder,
            BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2),
            CornerRadius = new CornerRadius(8 * scale),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2 * scale,
                OffsetY = 2 * scale,
                Blur = 8 * scale,
                Color = Color.FromArgb(60, 0, 0, 0)
            }),
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = node,
            Child = CreateNodeContent(node, context)
        };

        return border;
    }

    /// <summary>
    /// Creates the content inside the node. Override to customize.
    /// </summary>
    protected virtual Control CreateNodeContent(Node node, NodeRenderContext context)
    {
        return new TextBlock
        {
            Text = GetDisplayText(node),
            Foreground = context.Theme.NodeText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeight.Medium,
            FontSize = 14 * context.Scale,
            IsHitTestVisible = false
        };
    }

    /// <summary>
    /// Gets the display text for the node. Override to customize.
    /// </summary>
    protected virtual string GetDisplayText(Node node)
    {
        return $"{node.Type}\n{node.Id[..Math.Min(8, node.Id.Length)]}";
    }

    public virtual void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        if (visual is Border border)
        {
            border.BorderBrush = node.IsSelected 
                ? context.Theme.NodeSelectedBorder 
                : context.Theme.NodeBorder;
            border.BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2);
        }
    }

    public virtual void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
    {
        if (visual is Border border)
        {
            border.Width = width * context.Scale;
            border.Height = height * context.Scale;
        }
    }

    public virtual double? GetWidth(Node node, FlowCanvasSettings settings) => null;
    
    public virtual double? GetHeight(Node node, FlowCanvasSettings settings) => null;

    public virtual double? GetMinWidth(Node node, FlowCanvasSettings settings) => 60;

    public virtual double? GetMinHeight(Node node, FlowCanvasSettings settings) => 40;
}
