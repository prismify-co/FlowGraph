using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for output nodes with a distinctive red/orange color scheme.
/// </summary>
public class OutputNodeRenderer : DefaultNodeRenderer
{
    private static readonly IBrush OutputBackground = new SolidColorBrush(Color.Parse("#FBE9E7"));
    private static readonly IBrush OutputBackgroundDark = new SolidColorBrush(Color.Parse("#BF360C"));
    private static readonly IBrush OutputBorder = new SolidColorBrush(Color.Parse("#FF5722"));

    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var control = base.CreateNodeVisual(node, context);
        
        if (control is Border border)
        {
            // Use orange/red color scheme for output nodes
            var isDark = context.Theme.NodeBackground is SolidColorBrush brush && 
                         brush.Color.R < 128;
            border.Background = isDark ? OutputBackgroundDark : OutputBackground;
            if (!node.IsSelected)
            {
                border.BorderBrush = OutputBorder;
            }
        }

        return control;
    }

    protected override Control CreateNodeContent(Node node, NodeRenderContext context)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = "??",
            FontSize = 20 * context.Scale,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsHitTestVisible = false
        });

        panel.Children.Add(new TextBlock
        {
            Text = GetDisplayText(node),
            Foreground = context.Theme.NodeText,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeight.Medium,
            FontSize = 12 * context.Scale,
            IsHitTestVisible = false
        });

        return panel;
    }

    protected override string GetDisplayText(Node node)
    {
        // Show custom label if available in Data, otherwise show type
        if (node.Data is string label && !string.IsNullOrEmpty(label))
            return label;
        return "Output";
    }

    public override void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        base.UpdateSelection(visual, node, context);
        
        if (visual is Border border && !node.IsSelected)
        {
            border.BorderBrush = OutputBorder;
        }
    }
}
