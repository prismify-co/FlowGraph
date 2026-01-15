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
    private const double DefaultWidth = 100;
    private const double DefaultHeight = 50;
    private const double CornerRadius = 4;
    private const double BorderThickness = 2;

    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var width = (node.Width ?? DefaultWidth) * scale;
        var height = (node.Height ?? DefaultHeight) * scale;

        // Use a simple Border with a TextBlock - minimal visual tree
        var border = new Border
        {
            Width = width,
            Height = height,
            Background = GetBackgroundBrush(node, context),
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.NodeBorder,
            BorderThickness = new Thickness(BorderThickness * scale),
            CornerRadius = new CornerRadius(CornerRadius * scale),
            Child = new TextBlock
            {
                Text = node.Label ?? node.Type ?? node.Id,
                Foreground = context.Theme.NodeText,
                FontSize = 10 * scale,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4 * scale)
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
                (node.IsSelected ? BorderThickness + 1 : BorderThickness) * context.Scale);
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

    public double? GetWidth(Node node, FlowCanvasSettings settings) => node.Width ?? DefaultWidth;

    public double? GetHeight(Node node, FlowCanvasSettings settings) => node.Height ?? DefaultHeight;

    public double? GetMinWidth(Node node, FlowCanvasSettings settings) => 60;

    public double? GetMinHeight(Node node, FlowCanvasSettings settings) => 30;

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
