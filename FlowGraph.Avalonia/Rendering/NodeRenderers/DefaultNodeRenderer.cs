using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Default node renderer that creates a standard bordered box with text.
/// Supports inline label editing via IEditableNodeRenderer.
/// </summary>
public class DefaultNodeRenderer : INodeRenderer, IEditableNodeRenderer
{
    // Tag used to identify the content panel for editing
    private const string ContentPanelTag = "NodeContent";
    private const string LabelTextBlockTag = "LabelTextBlock";
    private const string EditTextBoxTag = "EditTextBox";

    public virtual Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var theme = context.Theme;
        var settings = context.Settings;

        // In transform-based rendering, use logical (unscaled) dimensions
        // The MatrixTransform on MainCanvas handles zoom
        var width = node.Width ?? GetWidth(node, settings) ?? settings.NodeWidth;
        var height = node.Height ?? GetHeight(node, settings) ?? settings.NodeHeight;

        var nodeBackground = theme.NodeBackground;
        var nodeBorder = node.IsSelected ? theme.NodeSelectedBorder : theme.NodeBorder;

        var border = new Border
        {
            Width = width,
            Height = height,
            Background = nodeBackground,
            BorderBrush = nodeBorder,
            BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2),
            CornerRadius = new CornerRadius(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2,
                OffsetY = 2,
                Blur = 8,
                Color = theme.NodeShadow
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
        var textBlock = new TextBlock
        {
            Text = GetDisplayText(node),
            Foreground = context.Theme.NodeText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeight.Medium,
            FontSize = 14, // Unscaled - transform handles zoom
            IsHitTestVisible = false,
            Tag = LabelTextBlockTag
        };

        // Wrap in a panel to allow swapping for edit mode
        var panel = new Grid
        {
            Tag = ContentPanelTag,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        panel.Children.Add(textBlock);

        return panel;
    }

    /// <summary>
    /// Gets the display text for the node. Override to customize.
    /// </summary>
    protected virtual string GetDisplayText(Node node)
    {
        var truncatedId = node.Id[..Math.Min(8, node.Id.Length)];

        // Use Label if available, otherwise use Type
        var name = !string.IsNullOrEmpty(node.Label)
            ? node.Label
            : node.Type ?? "Node";

        return $"{name}\n({truncatedId})";
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
        // In transform-based rendering, use logical (unscaled) dimensions
        if (visual is Border border)
        {
            border.Width = width;
            border.Height = height;
        }
    }

    public virtual double? GetWidth(Node node, FlowCanvasSettings settings) => null;

    public virtual double? GetHeight(Node node, FlowCanvasSettings settings) => null;

    public virtual double? GetMinWidth(Node node, FlowCanvasSettings settings) => 60;

    public virtual double? GetMinHeight(Node node, FlowCanvasSettings settings) => 40;

    #region IEditableNodeRenderer Implementation

    /// <summary>
    /// Enters edit mode - replaces the label TextBlock with a TextBox.
    /// </summary>
    public virtual void BeginEdit(Control visual, Node node, NodeRenderContext context, Action<string> onCommit, Action onCancel)
    {
        var contentPanel = FindContentPanel(visual);
        if (contentPanel == null) return;

        var labelTextBlock = FindLabelTextBlock(contentPanel);
        if (labelTextBlock == null) return;

        // Store the original display text for cancel/revert (what user sees before editing)
        var originalDisplayText = labelTextBlock.Text;
        var originalLabel = node.Label;

        // Hide the label
        labelTextBlock.IsVisible = false;

        // Create edit TextBox - show just the label or type (without ID) for cleaner editing
        var currentText = GetEditableText(node);
        var textBox = new TextBox
        {
            Text = currentText,
            FontSize = labelTextBlock.FontSize,
            Foreground = context.Theme.NodeText,
            Background = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = context.Theme.NodeSelectedBorder,
            Padding = new Thickness(4, 2),
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
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
            // Revert label to original value
            node.Label = originalLabel;
            // Restore the original display text directly (without going through GetDisplayText)
            labelTextBlock.Text = originalDisplayText;
            onCancel();
        }

        // Handle commit/cancel
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

        // Commit on focus loss (clicking elsewhere)
        textBox.LostFocus += (s, e) =>
        {
            Commit();
        };

        contentPanel.Children.Add(textBox);

        // Focus and select all text - must be done after control is in visual tree
        Dispatcher.UIThread.Post(() =>
        {
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Exits edit mode - removes the TextBox and shows the label.
    /// </summary>
    public virtual void EndEdit(Control visual, Node node, NodeRenderContext context)
    {
        var contentPanel = FindContentPanel(visual);
        if (contentPanel == null) return;

        // Remove the edit TextBox
        var textBox = contentPanel.Children.OfType<TextBox>()
            .FirstOrDefault(t => t.Tag as string == EditTextBoxTag);
        if (textBox != null)
        {
            textBox.Tag = null; // Mark as no longer editing
            contentPanel.Children.Remove(textBox);
        }

        // Show and update the label
        var labelTextBlock = FindLabelTextBlock(contentPanel);
        if (labelTextBlock != null)
        {
            labelTextBlock.Text = GetDisplayText(node);
            labelTextBlock.IsVisible = true;
        }
    }

    /// <summary>
    /// Gets whether the node is currently in edit mode.
    /// </summary>
    public virtual bool IsEditing(Control visual)
    {
        var contentPanel = FindContentPanel(visual);
        if (contentPanel == null) return false;

        return contentPanel.Children.OfType<TextBox>()
            .Any(t => t.Tag as string == EditTextBoxTag);
    }

    /// <summary>
    /// Gets the text to edit. Override to customize.
    /// </summary>
    protected virtual string GetEditableText(Node node)
    {
        // For editing, show just the label or type (without the ID)
        if (!string.IsNullOrEmpty(node.Label))
        {
            return node.Label;
        }
        return node.Type ?? "";
    }

    /// <summary>
    /// Finds the content panel in the node visual.
    /// </summary>
    protected Grid? FindContentPanel(Control visual)
    {
        if (visual is Border border && border.Child is Grid grid && grid.Tag as string == ContentPanelTag)
        {
            return grid;
        }
        return null;
    }

    /// <summary>
    /// Finds the label TextBlock in the content panel.
    /// </summary>
    protected TextBlock? FindLabelTextBlock(Grid contentPanel)
    {
        return contentPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(t => t.Tag as string == LabelTextBlockTag);
    }

    #endregion
}
