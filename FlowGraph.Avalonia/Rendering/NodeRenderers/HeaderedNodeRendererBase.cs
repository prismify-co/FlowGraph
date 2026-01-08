using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Base class for nodes with a header section and content area (React Flow style).
/// Structure: Outer border (rounded) -> Header (semi-transparent) + Body (solid white)
/// </summary>
public abstract class HeaderedNodeRendererBase : DataNodeRendererBase
{
    protected const string OuterBorderTag = "OuterBorder";
    protected const string HeaderPanelTag = "HeaderPanel";
    protected const string HeaderLabelTag = "HeaderLabel";
    protected const string BodyPanelTag = "BodyPanel";
    protected const string ContentPanelTag = "ContentPanel";

    /// <summary>
    /// Horizontal padding for content inside the header and body.
    /// </summary>
    protected virtual double HorizontalPadding => 12;

    /// <summary>
    /// Vertical padding for the header area.
    /// </summary>
    protected virtual double HeaderVerticalPadding => 8;

    /// <summary>
    /// Vertical padding for content inside the body.
    /// </summary>
    protected virtual double ContentVerticalPadding => 12;

    /// <summary>
    /// The corner radius for the outer node border.
    /// </summary>
    protected virtual double CornerRadius => 8;

    /// <summary>
    /// The font size for the header label.
    /// </summary>
    protected virtual double HeaderFontSize => 11;

    /// <summary>
    /// Gets the header text color.
    /// </summary>
    public virtual Color HeaderTextColor => Color.FromRgb(100, 100, 100);

    /// <summary>
    /// Gets the header background color. Default is glassy semi-transparent (~50% opacity).
    /// Override this in subclasses to provide accent colors.
    /// </summary>
    public virtual Color HeaderBackgroundColor => Color.FromArgb(128, 248, 248, 248); // ~50% opacity light gray (glassy)

    /// <summary>
    /// Gets the body background color.
    /// </summary>
    public virtual Color BodyBackgroundColor => Colors.White;

    /// <summary>
    /// Gets the node border color (outer border).
    /// </summary>
    public virtual Color NodeBorderColor => Color.FromRgb(230, 230, 230);

    /// <summary>
    /// Gets the border thickness for the outer node border.
    /// </summary>
    protected virtual double BorderThickness => 1;

    /// <summary>
    /// Gets the selected border thickness.
    /// </summary>
    protected virtual double SelectedBorderThickness => 2;

    /// <inheritdoc />
    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        return CreateDataBoundVisual(node, null, context);
    }

    /// <inheritdoc />
    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var scale = context.Scale;
        var baseWidth = node.Width ?? GetWidth(node, context.Settings) ?? context.Settings.NodeWidth;
        var baseHeight = node.Height ?? GetHeight(node, context.Settings) ?? context.Settings.NodeHeight;

        // Main vertical layout - holds header and body
        var mainPanel = new DockPanel
        {
            Width = baseWidth - 4, // Account for border
            LastChildFill = true
        };

        // Header panel - glassy semi-transparent background with bottom border separator
        var headerPanel = new Border
        {
            Background = new SolidColorBrush(HeaderBackgroundColor),
            BorderBrush = new SolidColorBrush(NodeBorderColor),
            BorderThickness = new Thickness(0, 0, 0, 1), // Bottom border only - separator line
            Padding = new Thickness(HorizontalPadding, HeaderVerticalPadding, HorizontalPadding, HeaderVerticalPadding),
            Tag = HeaderPanelTag,
            Child = new TextBlock
            {
                Text = node.Label ?? GetDefaultLabel(),
                FontWeight = FontWeight.Medium,
                FontSize = HeaderFontSize,
                Foreground = new SolidColorBrush(HeaderTextColor),
                Tag = HeaderLabelTag
            }
        };
        DockPanel.SetDock(headerPanel, Dock.Top);
        mainPanel.Children.Add(headerPanel);

        // Body panel - solid white background
        var bodyPanel = new Border
        {
            Background = new SolidColorBrush(BodyBackgroundColor),
            Padding = new Thickness(HorizontalPadding, ContentVerticalPadding, HorizontalPadding, ContentVerticalPadding),
            Tag = BodyPanelTag,
            Child = CreateContent(node, processor, context)
        };
        mainPanel.Children.Add(bodyPanel);

        // Outer border - wraps entire node with rounded corners and shadow
        var outerBorder = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : new SolidColorBrush(NodeBorderColor),
            BorderThickness = new Thickness(node.IsSelected ? SelectedBorderThickness : BorderThickness),
            CornerRadius = new CornerRadius(CornerRadius),
            ClipToBounds = true, // Clip children to rounded corners
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 2,
                Blur = 8,
                Spread = -2,
                Color = Color.FromArgb(30, 0, 0, 0)
            }),
            Tag = OuterBorderTag,
            Child = mainPanel
        };

        // Size container
        var sizeContainer = new Border
        {
            Width = baseWidth * scale,
            Height = baseHeight * scale,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = node
        };

        // Use Viewbox for uniform scaling
        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = outerBorder,
            Margin = new Thickness(1 * scale)
        };

        sizeContainer.Child = viewbox;

        return sizeContainer;
    }

    /// <summary>
    /// Gets the default label to display when the node has no label set.
    /// </summary>
    protected virtual string GetDefaultLabel() => "Node";

    /// <summary>
    /// Creates the content area control(s). 
    /// </summary>
    protected abstract Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context);

    /// <inheritdoc />
    public override void UpdateSelection(Control visual, Node node, NodeRenderContext context)
    {
        if (visual is not Border sizeContainer) return;

        // Find the outer border and update its border brush
        var outerBorder = FindByTag<Border>(sizeContainer, OuterBorderTag);
        if (outerBorder != null)
        {
            outerBorder.BorderBrush = node.IsSelected
                ? context.Theme.NodeSelectedBorder
                : new SolidColorBrush(NodeBorderColor);
            outerBorder.BorderThickness = new Thickness(node.IsSelected ? SelectedBorderThickness : BorderThickness);
        }
    }

    /// <inheritdoc />
    public override void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
    {
        if (visual is Border border)
        {
            border.Width = width * context.Scale;
            border.Height = height * context.Scale;

            // Update inner panel width
            var outerBorder = FindByTag<Border>(border, OuterBorderTag);
            if (outerBorder?.Child is DockPanel dockPanel)
            {
                dockPanel.Width = width - 4;
            }
        }
    }
}

/// <summary>
/// Headered node with accent-colored header (pink/rose tint).
/// The header has a glassy semi-transparent pink background and pink text.
/// </summary>
public abstract class StyledHeaderedNodeRendererBase : HeaderedNodeRendererBase
{
    /// <summary>
    /// Pink/rose header text color.
    /// </summary>
    public override Color HeaderTextColor => Color.FromRgb(200, 80, 120);

    /// <summary>
    /// Glassy semi-transparent pink header background (~50% opacity for glass effect).
    /// </summary>
    public override Color HeaderBackgroundColor => Color.FromArgb(128, 255, 235, 240); // Light pink with 50% opacity (glassy)
}

/// <summary>
/// Headered node with neutral gray header (subtle gray tint).
/// The header has a glassy semi-transparent gray background and gray text.
/// </summary>
public abstract class WhiteHeaderedNodeRendererBase : HeaderedNodeRendererBase
{
    /// <summary>
    /// Gray header text color.
    /// </summary>
    public override Color HeaderTextColor => Color.FromRgb(100, 100, 100);

    /// <summary>
    /// Glassy semi-transparent light gray header background (~50% opacity for glass effect).
    /// </summary>
    public override Color HeaderBackgroundColor => Color.FromArgb(128, 248, 248, 248); // Light gray with 50% opacity (glassy)
}
