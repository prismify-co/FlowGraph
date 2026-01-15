using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// A simplified node renderer optimized for large graphs.
/// Uses minimal visual elements for better performance with 500+ nodes.
/// </summary>
public class SimplifiedNodeRenderer : INodeRenderer
{
    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var width = (node.Width ?? DesignTokens.NodeWidthNarrow) * scale;
        var height = (node.Height ?? DesignTokens.NodeHeightCompact) * scale;

        // Use a simple Border with a TextBlock - minimal visual tree
        var border = new Border
        {
            Width = width,
            Height = height,
            Background = GetBackgroundBrush(node, context),
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.NodeBorder,
            BorderThickness = new Thickness(DesignTokens.BorderBase * scale),
            CornerRadius = new CornerRadius(DesignTokens.RadiusBase * scale),
            Child = new TextBlock
            {
                Text = node.Label ?? node.Type ?? node.Id,
                Foreground = context.Theme.NodeText,
                FontSize = DesignTokens.FontSizeXs * scale,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(DesignTokens.SpacingSm * scale)
            }
        };

        return border;
    }

    public void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        if (visual is Border border)
        {
            border.BorderBrush = node.IsSelected
                ? context.Theme.NodeSelectedBorder
                : context.Theme.NodeBorder;
            border.BorderThickness = new Thickness(
                (node.IsSelected ? DesignTokens.BorderThick : DesignTokens.BorderBase) * context.Scale);
        }
    }

    public void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
    {
        if (visual is Border border)
        {
            border.Width = width * context.Scale;
            border.Height = height * context.Scale;
        }
    }

    public double? GetWidth(Node node, FlowCanvasSettings settings) => node.Width ?? DesignTokens.NodeWidthNarrow;

    public double? GetHeight(Node node, FlowCanvasSettings settings) => node.Height ?? DesignTokens.NodeHeightCompact;

    public double? GetMinWidth(Node node, FlowCanvasSettings settings) => DesignTokens.NodeMinWidth;

    public double? GetMinHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeMinHeight;

    private static IBrush GetBackgroundBrush(Node node, NodeRenderContext context)
    {
        // Use type-based colors for quick visual differentiation
        return node.Type?.ToLowerInvariant() switch
        {
            "input" => context.Theme.InputNodeBackground,
            "output" => context.Theme.OutputNodeBackground,
            _ => context.Theme.NodeBackground
        };
    }
}
