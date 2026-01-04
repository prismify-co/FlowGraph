using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for group nodes that contain other nodes.
/// </summary>
public class GroupNodeRenderer : INodeRenderer
{
    private const double HeaderHeight = 30;
    private const double MinGroupWidth = 200;
    private const double MinGroupHeight = 100;
    private const double CollapseButtonSize = 20;

    /// <summary>
    /// Event raised when the collapse button is clicked.
    /// </summary>
    public event EventHandler<GroupCollapseEventArgs>? CollapseToggled;

    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var scale = context.Scale;
        var (width, height) = GetScaledDimensions(node, context);

        // Create container
        var container = new Grid
        {
            Width = width,
            Height = height,
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        // Header background
        var headerBg = new Border
        {
            Background = context.Theme.GroupHeaderBackground,
            CornerRadius = new CornerRadius(6 * scale, 6 * scale, 0, 0),
            Height = HeaderHeight * scale,
            [Grid.RowProperty] = 0
        };

        // Header content
        var headerContent = new Grid
        {
            Height = HeaderHeight * scale,
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(8 * scale, 0),
            [Grid.RowProperty] = 0
        };

        // Collapse/expand button
        var collapseButton = CreateCollapseButton(node, context);
        collapseButton.SetValue(Grid.ColumnProperty, 0);
        collapseButton.Tag = (node, "collapse");

        // Label
        var label = new TextBlock
        {
            Text = node.Label ?? "Group",
            Foreground = context.Theme.GroupHeaderText,
            FontSize = 12 * scale,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8 * scale, 0, 0, 0),
            [Grid.ColumnProperty] = 1
        };

        headerContent.Children.Add(collapseButton);
        headerContent.Children.Add(label);

        // Body background (only visible when expanded)
        var bodyBg = new Border
        {
            Background = context.Theme.GroupBodyBackground,
            CornerRadius = new CornerRadius(0, 0, 6 * scale, 6 * scale),
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.GroupBorder,
            BorderThickness = new Thickness(2 * scale),
            IsVisible = !node.IsCollapsed,
            [Grid.RowProperty] = 1
        };

        // Main border wrapping everything
        var mainBorder = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.GroupBorder,
            BorderThickness = new Thickness(2 * scale),
            CornerRadius = new CornerRadius(6 * scale),
            Child = container
        };

        container.Children.Add(headerBg);
        container.Children.Add(headerContent);
        container.Children.Add(bodyBg);

        return mainBorder;
    }

    public void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        if (visual is Border mainBorder)
        {
            mainBorder.BorderBrush = node.IsSelected 
                ? context.Theme.NodeSelectedBorder 
                : context.Theme.GroupBorder;

            // Also update body border if visible
            if (mainBorder.Child is Grid grid)
            {
                var bodyBg = grid.Children.OfType<Border>().FirstOrDefault(b => b.GetValue(Grid.RowProperty) is 1);
                if (bodyBg != null)
                {
                    bodyBg.BorderBrush = node.IsSelected 
                        ? context.Theme.NodeSelectedBorder 
                        : context.Theme.GroupBorder;
                }
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

            if (mainBorder.Child is Grid grid)
            {
                grid.Width = scaledWidth;
                grid.Height = scaledHeight;
            }
        }
    }

    public void UpdateCollapsedState(Control visual, Node node, NodeRenderContext context)
    {
        if (visual is Border mainBorder && mainBorder.Child is Grid grid)
        {
            var scale = context.Scale;
            var (width, _) = GetScaledDimensions(node, context);
            var height = node.IsCollapsed 
                ? HeaderHeight * scale 
                : (node.Height ?? MinGroupHeight) * scale;

            mainBorder.Height = height;
            grid.Height = height;

            // Update body visibility
            var bodyBg = grid.Children.OfType<Border>().FirstOrDefault(b => b.GetValue(Grid.RowProperty) is 1);
            if (bodyBg != null)
            {
                bodyBg.IsVisible = !node.IsCollapsed;
            }

            // Update collapse button icon
            var headerContent = grid.Children.OfType<Grid>().FirstOrDefault();
            var collapseButton = headerContent?.Children.OfType<Border>().FirstOrDefault();
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
            FontSize = 10 * scale,
            Foreground = context.Theme.GroupHeaderText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
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

        // Hover effect
        button.PointerEntered += (s, e) => button.Background = context.Theme.GroupHeaderHover;
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
