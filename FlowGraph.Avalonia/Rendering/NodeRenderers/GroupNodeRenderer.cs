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

    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var (width, height) = GetScaledDimensions(node, context);

        // Main container with translucent background
        var mainBorder = new Border
        {
            Width = width,
            Height = height,
            Background = context.Theme.GroupBackground,
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.GroupBorder,
            BorderThickness = new Thickness(DashedStrokeThickness * scale),
            CornerRadius = new CornerRadius(BorderRadius * scale),
            // Use dashed border via a custom style or overlay
            Child = CreateContent(node, context, width, height)
        };

        // Create dashed border overlay for non-selected state
        if (!node.IsSelected)
        {
            var container = new Grid
            {
                Width = width,
                Height = height
            };

            // Background fill
            var backgroundRect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = context.Theme.GroupBackground,
                RadiusX = BorderRadius * scale,
                RadiusY = BorderRadius * scale
            };

            // Dashed border
            var dashedBorder = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Transparent,
                Stroke = context.Theme.GroupBorder,
                StrokeThickness = DashedStrokeThickness * scale,
                StrokeDashArray = new AvaloniaList<double> { 4, 2 },
                RadiusX = BorderRadius * scale,
                RadiusY = BorderRadius * scale
            };

            // Content overlay
            var content = CreateContent(node, context, width, height);

            container.Children.Add(backgroundRect);
            container.Children.Add(dashedBorder);
            container.Children.Add(content);

            return container;
        }

        return mainBorder;
    }

    private Control CreateContent(Node node, NodeRenderContext context, double width, double height)
    {
        var scale = context.Scale;

        // Header with collapse button and label
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

        if (visual is Border mainBorder)
        {
            mainBorder.BorderBrush = node.IsSelected ? selectedBrush : normalBrush;
            // Switch to solid border when selected
            if (node.IsSelected)
            {
                mainBorder.BorderThickness = new Thickness(DashedStrokeThickness * scale);
            }
        }
        else if (visual is Grid grid)
        {
            // Update dashed border color
            var dashedBorder = grid.Children.OfType<Rectangle>()
                .FirstOrDefault(r => r.StrokeDashArray?.Count > 0);
            if (dashedBorder != null)
            {
                dashedBorder.Stroke = node.IsSelected ? selectedBrush : normalBrush;
            }
        }
    }

    public void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
    {
        var scale = context.Scale;
        var scaledWidth = width * scale;
        var scaledHeight = node.IsCollapsed ? HeaderHeight * scale : height * scale;

        if (visual is Border mainBorder)
        {
            mainBorder.Width = scaledWidth;
            mainBorder.Height = scaledHeight;
        }
        else if (visual is Grid grid)
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
        var (width, _) = GetScaledDimensions(node, context);
        var height = node.IsCollapsed
            ? HeaderHeight * scale
            : (node.Height ?? MinGroupHeight) * scale;

        UpdateSize(visual, node, context, node.Width ?? MinGroupWidth, node.Height ?? MinGroupHeight);

        // Update collapse button icon
        StackPanel? headerPanel = null;
        if (visual is Border border && border.Child is StackPanel sp)
        {
            headerPanel = sp;
        }
        else if (visual is Grid grid)
        {
            headerPanel = grid.Children.OfType<StackPanel>().FirstOrDefault();
        }

        if (headerPanel != null)
        {
            var collapseButton = headerPanel.Children.OfType<Border>().FirstOrDefault();
            if (collapseButton?.Child is TextBlock icon)
            {
                icon.Text = node.IsCollapsed ? "?" : "?";
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
            Text = node.IsCollapsed ? "?" : "?",
            FontSize = 9 * scale,
            Foreground = context.Theme.GroupLabelText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };

        var button = new Border
        {
            Width = buttonSize,
            Height = buttonSize,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(3 * scale),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = icon,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Hover effect - slightly visible background
        button.PointerEntered += (s, e) =>
        {
            button.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
        };
        button.PointerExited += (s, e) => button.Background = Brushes.Transparent;

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
