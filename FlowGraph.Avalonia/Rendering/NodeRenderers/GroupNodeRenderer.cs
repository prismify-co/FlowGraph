using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for group nodes that contain other nodes.
/// Uses a translucent background similar to React Flow's grouping style.
/// </summary>
public class GroupNodeRenderer : INodeRenderer
{
    private const double HeaderHeight = 28;
    private const double MinGroupWidth = 200;
    private const double MinGroupHeight = 100;
    private const double CollapseButtonSize = 18;
    private const double BorderRadius = 8;
    private const double DashedStrokeThickness = 2;

    // Use simple ASCII characters that render in all fonts
    private const string ExpandedIcon = "-";   // Minus sign (expanded, can collapse)
    private const string CollapsedIcon = "+";  // Plus sign (collapsed, can expand)

    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var (width, height) = GetScaledDimensions(node, context);

        // Create container grid
        var container = new Grid
        {
            Width = width,
            Height = height
        };

        // Background fill with rounded corners
        var backgroundRect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = context.Theme.GroupBackground,
            RadiusX = BorderRadius * scale,
            RadiusY = BorderRadius * scale
        };

        // Border - dashed when not selected, solid when selected
        var border = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brushes.Transparent,
            Stroke = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.GroupBorder,
            StrokeThickness = DashedStrokeThickness * scale,
            RadiusX = BorderRadius * scale,
            RadiusY = BorderRadius * scale
        };

        // Apply dashed stroke only when not selected
        if (!node.IsSelected)
        {
            border.StrokeDashArray = new AvaloniaList<double> { 4, 2 };
        }

        // Header content
        var headerPanel = CreateHeaderPanel(node, context);

        container.Children.Add(backgroundRect);
        container.Children.Add(border);
        container.Children.Add(headerPanel);

        return container;
    }

    private StackPanel CreateHeaderPanel(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;

        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(8 * scale, 6 * scale, 8 * scale, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        // Collapse/expand button
        var collapseButton = CreateCollapseButton(node, context);
        headerPanel.Children.Add(collapseButton);

        // Label
        var label = new TextBlock
        {
            Text = node.Label ?? "Group",
            Foreground = context.Theme.GroupLabelText,
            FontSize = 11 * scale,
            FontWeight = FontWeight.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4 * scale, 0, 0, 0),
            Opacity = 0.9
        };
        headerPanel.Children.Add(label);

        return headerPanel;
    }

    public void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var selectedBrush = context.Theme.NodeSelectedBorder;
        var normalBrush = context.Theme.GroupBorder;

        if (visual is Grid grid)
        {
            // Find the border rectangle (the one with Stroke set)
            var border = grid.Children.OfType<Rectangle>()
                .FirstOrDefault(r => r.Stroke != null);
            
            if (border != null)
            {
                border.Stroke = node.IsSelected ? selectedBrush : normalBrush;
                
                // Toggle dashed style based on selection
                if (node.IsSelected)
                {
                    border.StrokeDashArray = null;
                }
                else
                {
                    border.StrokeDashArray = new AvaloniaList<double> { 4, 2 };
                }
            }
        }
    }

    public void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
    {
        var scale = context.Scale;
        var scaledWidth = width * scale;
        var scaledHeight = node.IsCollapsed ? HeaderHeight * scale : height * scale;

        if (visual is Grid grid)
        {
            grid.Width = scaledWidth;
            grid.Height = scaledHeight;

            foreach (var child in grid.Children.OfType<Rectangle>())
            {
                child.Width = scaledWidth;
                child.Height = scaledHeight;
            }
        }
    }

    public void UpdateCollapsedState(Control visual, Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        UpdateSize(visual, node, context, node.Width ?? MinGroupWidth, node.Height ?? MinGroupHeight);

        // Update collapse button icon
        if (visual is Grid grid)
        {
            var headerPanel = grid.Children.OfType<StackPanel>().FirstOrDefault();
            if (headerPanel != null)
            {
                var collapseButton = headerPanel.Children.OfType<Border>().FirstOrDefault();
                if (collapseButton?.Child is TextBlock icon)
                {
                    icon.Text = node.IsCollapsed ? CollapsedIcon : ExpandedIcon;
                }
            }
        }
    }

    public double? GetWidth(Node node, FlowCanvasSettings settings)
    {
        return node.Width ?? MinGroupWidth;
    }

    public double? GetHeight(Node node, FlowCanvasSettings settings)
    {
        if (node.IsCollapsed)
            return HeaderHeight;
        return node.Height ?? MinGroupHeight;
    }

    public double? GetMinWidth(Node node, FlowCanvasSettings settings) => MinGroupWidth;

    public double? GetMinHeight(Node node, FlowCanvasSettings settings) =>
        node.IsCollapsed ? HeaderHeight : MinGroupHeight;

    private static (double width, double height) GetScaledDimensions(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var width = (node.Width ?? MinGroupWidth) * scale;
        var height = node.IsCollapsed
            ? HeaderHeight * scale
            : (node.Height ?? MinGroupHeight) * scale;
        return (width, height);
    }

    private Border CreateCollapseButton(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var buttonSize = CollapseButtonSize * scale;

        var icon = new TextBlock
        {
            Text = node.IsCollapsed ? CollapsedIcon : ExpandedIcon,
            FontSize = 12 * scale,
            FontWeight = FontWeight.Bold,
            Foreground = context.Theme.GroupLabelText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var button = new Border
        {
            Width = buttonSize,
            Height = buttonSize,
            Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
            CornerRadius = new CornerRadius(3 * scale),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = icon,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = (node, "collapse") // Tag to identify this as a collapse button
        };

        // Hover effect
        button.PointerEntered += (s, e) =>
        {
            button.Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128));
        };
        button.PointerExited += (s, e) =>
        {
            button.Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
        };

        return button;
    }
}

/// <summary>
/// Event args for group collapse toggle.
/// </summary>
public class GroupCollapseEventArgs : EventArgs
{
    public Node Group { get; }
    public bool IsCollapsed { get; }

    public GroupCollapseEventArgs(Node group, bool isCollapsed)
    {
        Group = group;
        IsCollapsed = isCollapsed;
    }
}
