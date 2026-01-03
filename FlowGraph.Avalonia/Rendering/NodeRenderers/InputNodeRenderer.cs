using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for input nodes with a distinctive green color scheme.
/// </summary>
public class InputNodeRenderer : DefaultNodeRenderer
{
    private static readonly IBrush InputBackground = new SolidColorBrush(Color.Parse("#E8F5E9"));
    private static readonly IBrush InputBackgroundDark = new SolidColorBrush(Color.Parse("#1B5E20"));
    private static readonly IBrush InputBorder = new SolidColorBrush(Color.Parse("#4CAF50"));

    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var control = base.CreateNodeVisual(node, context);
        
        if (control is Border border)
        {
            // Use green color scheme for input nodes
            var isDark = context.Theme.NodeBackground is SolidColorBrush brush && 
                         brush.Color.R < 128;
            border.Background = isDark ? InputBackgroundDark : InputBackground;
            if (!node.IsSelected)
            {
                border.BorderBrush = InputBorder;
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
        return "Input";
    }

    public override void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        base.UpdateSelection(visual, node, context);
        
        if (visual is Border border && !node.IsSelected)
        {
            border.BorderBrush = InputBorder;
        }
    }
}
