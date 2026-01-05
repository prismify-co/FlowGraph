using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for output nodes with a distinctive red/orange color scheme.
/// Supports inline label editing.
/// </summary>
public class OutputNodeRenderer : DefaultNodeRenderer
{
    private static readonly IBrush OutputBackground = new SolidColorBrush(Color.Parse("#FBE9E7"));
    private static readonly IBrush OutputBackgroundDark = new SolidColorBrush(Color.Parse("#BF360C"));
    private static readonly IBrush OutputBorder = new SolidColorBrush(Color.Parse("#FF5722"));

    private const string IconTag = "NodeIcon";
    private const string LabelTag = "NodeLabel";

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
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "NodeContent"
        };

        // Simple target/output icon using text
        panel.Children.Add(new TextBlock
        {
            Text = "O",
            FontSize = 18 * context.Scale,
            FontWeight = FontWeight.Bold,
            Foreground = OutputBorder,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsHitTestVisible = false,
            Tag = IconTag
        });

        panel.Children.Add(new TextBlock
        {
            Text = GetDisplayText(node),
            Foreground = context.Theme.NodeText,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeight.Medium,
            FontSize = 12 * context.Scale,
            IsHitTestVisible = false,
            Tag = LabelTag
        });

        return panel;
    }

    protected override string GetDisplayText(Node node)
    {
        // Use Label if available, then Data, then default
        if (!string.IsNullOrEmpty(node.Label))
            return node.Label;
        if (node.Data is string label && !string.IsNullOrEmpty(label))
            return label;
        return "Output";
    }

    protected override string GetEditableText(Node node)
    {
        // For output nodes, edit the Label or Data
        if (!string.IsNullOrEmpty(node.Label))
            return node.Label;
        if (node.Data is string data && !string.IsNullOrEmpty(data))
            return data;
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

    #region IEditableNodeRenderer Override

    public override void BeginEdit(Control visual, Node node, NodeRenderContext context, Action<string> onCommit, Action onCancel)
    {
        var contentPanel = FindStackPanelContent(visual);
        if (contentPanel == null) return;

        var labelTextBlock = contentPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(t => t.Tag as string == LabelTag);
        if (labelTextBlock == null) return;

        // Store original values for cancel/revert
        var originalDisplayText = labelTextBlock.Text;
        var originalLabel = node.Label;

        // Hide the label
        labelTextBlock.IsVisible = false;

        // Create edit TextBox
        var currentText = GetEditableText(node);
        var textBox = new TextBox
        {
            Text = currentText,
            FontSize = labelTextBlock.FontSize,
            Foreground = context.Theme.NodeText,
            Background = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = OutputBorder,
            Padding = new Thickness(4, 2),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 60,
            Tag = "EditTextBox",
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

        contentPanel.Children.Add(textBox);
        
        Dispatcher.UIThread.Post(() =>
        {
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Render);
    }

    public override void EndEdit(Control visual, Node node, NodeRenderContext context)
    {
        var contentPanel = FindStackPanelContent(visual);
        if (contentPanel == null) return;

        var textBox = contentPanel.Children.OfType<TextBox>()
            .FirstOrDefault(t => t.Tag as string == "EditTextBox");
        if (textBox != null)
        {
            textBox.Tag = null;
            contentPanel.Children.Remove(textBox);
        }

        var labelTextBlock = contentPanel.Children.OfType<TextBlock>()
            .FirstOrDefault(t => t.Tag as string == LabelTag);
        if (labelTextBlock != null)
        {
            labelTextBlock.Text = GetDisplayText(node);
            labelTextBlock.IsVisible = true;
        }
    }

    public override bool IsEditing(Control visual)
    {
        var contentPanel = FindStackPanelContent(visual);
        if (contentPanel == null) return false;

        return contentPanel.Children.OfType<TextBox>()
            .Any(t => t.Tag as string == "EditTextBox");
    }

    private StackPanel? FindStackPanelContent(Control visual)
    {
        if (visual is Border border && border.Child is StackPanel panel)
        {
            return panel;
        }
        return null;
    }

    #endregion
}
