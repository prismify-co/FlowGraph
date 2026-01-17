using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for group nodes that contain other nodes.
/// Uses a translucent background similar to React Flow's grouping style.
/// Supports inline label editing via IEditableNodeRenderer.
/// 
/// IMPORTANT: This renderer uses constants from GraphRenderModel to ensure
/// 100% visual parity with DirectGraphRenderer.
/// </summary>
public class GroupNodeRenderer : INodeRenderer, IEditableNodeRenderer
{
    // Use constants from GraphRenderModel for visual parity with DirectGraphRenderer
    private const double HeaderHeight = GraphRenderModel.GroupHeaderHeight;
    private const double MinGroupWidth = GraphRenderModel.MinGroupWidth;
    private const double MinGroupHeight = GraphRenderModel.MinGroupHeight;
    private const double CollapseButtonSize = GraphRenderModel.GroupCollapseButtonSize;
    private const double BorderRadius = GraphRenderModel.GroupBorderRadius;
    private const double DashedStrokeThickness = GraphRenderModel.GroupDashedStrokeThickness;
    private const double HeaderMarginX = GraphRenderModel.GroupHeaderMarginX;
    private const double HeaderMarginY = GraphRenderModel.GroupHeaderMarginY;

    // Use simple ASCII characters that render in all fonts
    private const string ExpandedIcon = "-";   // Minus sign (expanded, can collapse)
    private const string CollapsedIcon = "+";  // Plus sign (collapsed, can expand)

    // Tag for identifying elements
    private const string LabelTextBlockTag = "GroupLabel";
    private const string EditTextBoxTag = "GroupEditTextBox";
    private const string HeaderPanelTag = "GroupHeader";

    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        // In transform-based rendering, use logical (unscaled) dimensions
        // MatrixTransform handles all zoom scaling
        var (width, height) = GetDimensions(node);

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
            RadiusX = BorderRadius,
            RadiusY = BorderRadius
        };

        // Border - dashed when not selected, solid when selected
        var border = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brushes.Transparent,
            Stroke = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.GroupBorder,
            StrokeThickness = DashedStrokeThickness,
            RadiusX = BorderRadius,
            RadiusY = BorderRadius
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
        // In transform-based rendering, use logical (unscaled) dimensions
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(DesignTokens.SpacingMd, DesignTokens.SpacingBase, DesignTokens.SpacingMd, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Tag = HeaderPanelTag
        };

        // Collapse/expand button
        var collapseButton = CreateCollapseButton(node, context);
        headerPanel.Children.Add(collapseButton);

        // Label
        var label = new TextBlock
        {
            Text = node.Label ?? "Group",
            Foreground = context.Theme.GroupLabelText,
            FontSize = DesignTokens.FontSizeSm,
            FontWeight = FontWeight.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(DesignTokens.SpacingSm, 0, 0, 0),
            Opacity = 0.9,
            Tag = LabelTextBlockTag
        };
        headerPanel.Children.Add(label);

        return headerPanel;
    }

    public void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        var selectedBrush = context.Theme.NodeSelectedBorder;
        var highlightedBrush = context.Theme.NodeHighlightedBorder;
        var normalBrush = context.Theme.GroupBorder;

        if (visual is Grid grid)
        {
            // Find the border rectangle (the one with Stroke set)
            var border = grid.Children.OfType<Rectangle>()
                .FirstOrDefault(r => r.Stroke != null);

            if (border != null)
            {
                // Priority: Selected > Highlighted > Normal
                border.Stroke = node.IsSelected
                    ? selectedBrush
                    : node.IsHighlighted
                        ? highlightedBrush
                        : normalBrush;

                // Toggle dashed style based on selection/highlight
                if (node.IsSelected || node.IsHighlighted)
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
        // In transform-based rendering, use logical (unscaled) dimensions
        var logicalHeight = node.IsCollapsed ? HeaderHeight : height;

        if (visual is Grid grid)
        {
            grid.Width = width;
            grid.Height = logicalHeight;

            foreach (var child in grid.Children.OfType<Rectangle>())
            {
                child.Width = width;
                child.Height = logicalHeight;
            }
        }
    }

    public void UpdateCollapsedState(Control visual, Node node, NodeRenderContext context)
    {
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

    private static (double width, double height) GetDimensions(Node node)
    {
        // In transform-based rendering, use logical (unscaled) dimensions
        var width = node.Width ?? MinGroupWidth;
        var height = node.IsCollapsed
            ? HeaderHeight
            : (node.Height ?? MinGroupHeight);
        return (width, height);
    }

    private Border CreateCollapseButton(Node node, NodeRenderContext context)
    {
        // In transform-based rendering, use logical (unscaled) dimensions
        var buttonSize = CollapseButtonSize;

        var icon = new TextBlock
        {
            Text = node.IsCollapsed ? CollapsedIcon : ExpandedIcon,
            FontSize = DesignTokens.FontSizeBase,
            FontWeight = FontWeight.Bold,
            Foreground = context.Theme.GroupLabelText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var button = new Border
        {
            Width = buttonSize,
            Height = buttonSize,
            Background = context.Theme.GroupButtonBackground,
            CornerRadius = new CornerRadius(DesignTokens.RadiusSm),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = icon,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = (node, "collapse") // Tag to identify this as a collapse button
        };

        // Hover effect - capture theme reference for use in lambda
        var theme = context.Theme;
        button.PointerEntered += (s, e) =>
        {
            button.Background = theme.GroupButtonHover;
        };
        button.PointerExited += (s, e) =>
        {
            button.Background = theme.GroupButtonBackground;
        };

        return button;
    }

    #region IEditableNodeRenderer Implementation

    /// <summary>
    /// Enters edit mode - replaces the label TextBlock with a TextBox.
    /// </summary>
    public void BeginEdit(Control visual, Node node, NodeRenderContext context, Action<string> onCommit, Action onCancel)
    {
        var headerPanel = FindHeaderPanel(visual);
        if (headerPanel == null) return;

        var labelTextBlock = FindLabelTextBlock(headerPanel);
        if (labelTextBlock == null) return;

        // Store original values for cancel/revert
        var originalDisplayText = labelTextBlock.Text;
        var originalLabel = node.Label;

        // Hide the label
        labelTextBlock.IsVisible = false;

        // Create edit TextBox
        var currentText = node.Label ?? "Group";
        var textBox = new TextBox
        {
            Text = currentText,
            FontSize = labelTextBlock.FontSize,
            Foreground = context.Theme.GroupLabelText,
            Background = Brushes.White,
            BorderThickness = new Thickness(DesignTokens.BorderThin),
            BorderBrush = context.Theme.NodeSelectedBorder,
            Padding = new Thickness(DesignTokens.SpacingSm, DesignTokens.SpacingXs),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(DesignTokens.SpacingSm, 0, 0, 0),
            MinWidth = 80,
            Tag = EditTextBoxTag,
            AcceptsReturn = false
        };

        bool finished = false;

        void Commit()
        {
            if (finished) return;
            finished = true;
            onCommit(textBox.Text ?? "");
        }

        void Cancel()
        {
            if (finished) return;
            finished = true;
            node.Label = originalLabel;
            labelTextBlock.Text = originalDisplayText;
            onCancel();
        }

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Cancel();
                e.Handled = true;
            }
        };

        textBox.LostFocus += (s, e) =>
        {
            Commit();
        };

        headerPanel.Children.Add(textBox);

        Dispatcher.UIThread.Post(() =>
        {
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Exits edit mode - removes the TextBox and shows the label.
    /// </summary>
    public void EndEdit(Control visual, Node node, NodeRenderContext context)
    {
        var headerPanel = FindHeaderPanel(visual);
        if (headerPanel == null) return;

        // Remove the edit TextBox
        var textBox = headerPanel.Children.OfType<TextBox>()
            .FirstOrDefault(t => t.Tag as string == EditTextBoxTag);
        if (textBox != null)
        {
            textBox.Tag = null;
            headerPanel.Children.Remove(textBox);
        }

        // Show and update the label
        var labelTextBlock = FindLabelTextBlock(headerPanel);
        if (labelTextBlock != null)
        {
            labelTextBlock.Text = node.Label ?? "Group";
            labelTextBlock.IsVisible = true;
        }
    }

    /// <summary>
    /// Gets whether the node is currently in edit mode.
    /// </summary>
    public bool IsEditing(Control visual)
    {
        var headerPanel = FindHeaderPanel(visual);
        if (headerPanel == null) return false;

        return headerPanel.Children.OfType<TextBox>()
            .Any(t => t.Tag as string == EditTextBoxTag);
    }

    private StackPanel? FindHeaderPanel(Control visual)
    {
        if (visual is Grid grid)
        {
            return grid.Children.OfType<StackPanel>()
                .FirstOrDefault(p => p.Tag as string == HeaderPanelTag);
        }
        return null;
    }

    private TextBlock? FindLabelTextBlock(StackPanel headerPanel)
    {
        return headerPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(t => t.Tag as string == LabelTextBlockTag);
    }

    #endregion
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
