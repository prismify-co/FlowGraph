using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Base class for styled node renderers with icon and label support.
/// Provides customizable colors, icons (text or geometry), and inline editing.
/// <para>
/// Override the abstract properties to customize appearance:
/// - <see cref="DefaultLabel"/>: Default text when no label is set
/// - <see cref="GetNodeBackground"/>, <see cref="GetNodeBorder"/>: Node colors
/// - <see cref="GetIconForeground"/>, <see cref="GetTextForeground"/>: Text/icon colors
/// </para>
/// <para>
/// For icons, override ONE of:
/// - <see cref="IconText"/>: Simple text icon (e.g., ">", "O", "?")
/// - <see cref="IconGeometry"/>: Vector path for scalable icons (compatible with any icon library)
/// </para>
/// <example>
/// Using a Lucide icon path:
/// <code>
/// // Get path data from any icon library (Lucide, Material, etc.)
/// protected override Geometry? IconGeometry => 
///     Geometry.Parse("M5 12h14M12 5l7 7-7 7"); // arrow-right
/// </code>
/// </example>
/// </summary>
public abstract class StyledNodeRendererBase : DefaultNodeRenderer
{
    /// <summary>
    /// Tag used to identify the icon element.
    /// </summary>
    protected const string IconTag = "NodeIcon";
    
    /// <summary>
    /// Tag used to identify the label TextBlock.
    /// </summary>
    protected const string LabelTag = "NodeLabel";

    /// <summary>
    /// Gets the text icon to display (e.g., ">", "O", "?").
    /// Override <see cref="IconGeometry"/> instead for vector icons.
    /// Default is null (no text icon).
    /// </summary>
    protected virtual string? IconText => null;

    /// <summary>
    /// Gets the geometry for a vector icon. This takes precedence over <see cref="IconText"/>.
    /// Use this to integrate with any icon library (Lucide, Material, FontAwesome, etc.)
    /// by providing the icon's path data.
    /// <example>
    /// <code>
    /// // Simple arrow icon
    /// protected override Geometry? IconGeometry => 
    ///     Geometry.Parse("M5 12h14M12 5l7 7-7 7");
    /// 
    /// // Or use a static geometry for better performance
    /// private static readonly Geometry _icon = Geometry.Parse("M5 12h14");
    /// protected override Geometry? IconGeometry => _icon;
    /// </code>
    /// </example>
    /// </summary>
    protected virtual Geometry? IconGeometry => null;

    /// <summary>
    /// Gets the icon size in canvas units (before scaling).
    /// Default is 16.
    /// </summary>
    protected virtual double IconSize => 16;

    /// <summary>
    /// Gets the default label when no label or data is set.
    /// </summary>
    protected abstract string DefaultLabel { get; }

    /// <summary>
    /// Gets the background brush for this node type.
    /// </summary>
    protected abstract IBrush GetNodeBackground(ThemeResources theme);

    /// <summary>
    /// Gets the border brush for this node type (when not selected).
    /// </summary>
    protected abstract IBrush GetNodeBorder(ThemeResources theme);

    /// <summary>
    /// Gets the icon foreground brush for this node type.
    /// </summary>
    protected abstract IBrush GetIconForeground(ThemeResources theme);

    /// <summary>
    /// Gets the text foreground brush for this node type.
    /// </summary>
    protected abstract IBrush GetTextForeground(ThemeResources theme);

    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var control = base.CreateNodeVisual(node, context);
        
        if (control is Border border)
        {
            border.Background = GetNodeBackground(context.Theme);
            if (!node.IsSelected)
            {
                border.BorderBrush = GetNodeBorder(context.Theme);
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

        // Add icon (geometry takes precedence over text)
        var iconElement = CreateIconElement(context);
        if (iconElement != null)
        {
            iconElement.Tag = IconTag;
            panel.Children.Add(iconElement);
        }

        // Label
        panel.Children.Add(new TextBlock
        {
            Text = GetDisplayText(node),
            Foreground = GetTextForeground(context.Theme),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeight.Medium,
            FontSize = 12 * context.Scale,
            IsHitTestVisible = false,
            Tag = LabelTag
        });

        return panel;
    }

    /// <summary>
    /// Creates the icon element based on <see cref="IconGeometry"/> or <see cref="IconText"/>.
    /// </summary>
    protected virtual Control? CreateIconElement(NodeRenderContext context)
    {
        var geometry = IconGeometry;
        if (geometry != null)
        {
            // Use Path for vector icon
            return new AvaloniaPath
            {
                Data = geometry,
                Fill = GetIconForeground(context.Theme),
                Stretch = Stretch.Uniform,
                Width = IconSize * context.Scale,
                Height = IconSize * context.Scale,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };
        }

        var text = IconText;
        if (!string.IsNullOrEmpty(text))
        {
            // Use TextBlock for text icon
            return new TextBlock
            {
                Text = text,
                FontSize = 18 * context.Scale,
                FontWeight = FontWeight.Bold,
                Foreground = GetIconForeground(context.Theme),
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };
        }

        return null;
    }

    protected override string GetDisplayText(Node node)
    {
        var truncatedId = node.Id[..Math.Min(8, node.Id.Length)];
        
        string name;
        if (!string.IsNullOrEmpty(node.Label))
            name = node.Label;
        else if (node.Data is string data && !string.IsNullOrEmpty(data))
            name = data;
        else
            name = DefaultLabel;
        
        return $"{name}\n({truncatedId})";
    }

    protected override string GetEditableText(Node node)
    {
        if (!string.IsNullOrEmpty(node.Label))
            return node.Label;
        if (node.Data is string data && !string.IsNullOrEmpty(data))
            return data;
        return DefaultLabel;
    }

    public override void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        base.UpdateSelection(visual, node, context);
        
        if (visual is Border border && !node.IsSelected)
        {
            border.BorderBrush = GetNodeBorder(context.Theme);
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

        var originalDisplayText = labelTextBlock.Text;
        var originalLabel = node.Label;

        labelTextBlock.IsVisible = false;

        var currentText = GetEditableText(node);
        var textBox = new TextBox
        {
            Text = currentText,
            FontSize = labelTextBlock.FontSize,
            Foreground = context.Theme.NodeText,
            Background = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = GetNodeBorder(context.Theme),
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

        textBox.LostFocus += (s, e) => Commit();

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

    /// <summary>
    /// Finds the StackPanel content in the node visual.
    /// </summary>
    protected StackPanel? FindStackPanelContent(Control visual)
    {
        if (visual is Border border && border.Child is StackPanel panel)
        {
            return panel;
        }
        return null;
    }

    #endregion
}
