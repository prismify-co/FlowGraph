using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using System;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// A panel with zoom and viewport control buttons for FlowCanvas.
/// </summary>
/// <remarks>
/// Community edition includes: Zoom In, Zoom Out, Fit to View, Reset Zoom.
/// Use <see cref="AddButton"/> to add custom buttons to the panel.
/// </remarks>
public partial class FlowControls : UserControl
{
    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowControls, FlowCanvas?>(nameof(TargetCanvas));

    /// <summary>
    /// The FlowCanvas this control panel operates on.
    /// </summary>
    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    public FlowControls()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        var zoomInButton = this.FindControl<Button>("ZoomInButton");
        var zoomOutButton = this.FindControl<Button>("ZoomOutButton");
        var fitViewButton = this.FindControl<Button>("FitViewButton");
        var resetZoomButton = this.FindControl<Button>("ResetZoomButton");

        if (zoomInButton != null)
            zoomInButton.Click += (_, _) => TargetCanvas?.ZoomIn();
        if (zoomOutButton != null)
            zoomOutButton.Click += (_, _) => TargetCanvas?.ZoomOut();
        if (fitViewButton != null)
            fitViewButton.Click += (_, _) => TargetCanvas?.FitToView();
        if (resetZoomButton != null)
            resetZoomButton.Click += (_, _) => TargetCanvas?.ResetZoom();
    }

    /// <summary>
    /// Adds a separator line to the control panel.
    /// </summary>
    public void AddSeparator()
    {
        if (ButtonPanel == null) return;

        var separator = new Rectangle
        {
            Height = 1,
            Margin = new Thickness(6, 4),
        };
        separator.Bind(Shape.FillProperty, this.GetResourceObservable("FlowControlsSeparator"));
        ButtonPanel.Children.Add(separator);
    }

    /// <summary>
    /// Adds a custom button to the control panel with a Lucide icon path.
    /// </summary>
    /// <param name="iconPathData">SVG path data for the icon (e.g., from Lucide icons)</param>
    /// <param name="tooltip">Tooltip text for the button</param>
    /// <param name="onClick">Action to execute when clicked</param>
    /// <returns>The created button for further customization</returns>
    public Button AddButton(string iconPathData, string tooltip, Action onClick)
    {
        if (ButtonPanel == null)
            throw new InvalidOperationException("ButtonPanel not initialized");

        var path = new global::Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(iconPathData),
            Width = 24,
            Height = 24,
            Stretch = Stretch.None
        };
        path.Bind(Shape.StrokeProperty, this.GetResourceObservable("FlowControlsIconStroke"));
        path.StrokeThickness = 2;
        path.StrokeLineCap = PenLineCap.Round;

        var button = new Button
        {
            Content = path,
            Classes = { "flow-control" }
        };
        ToolTip.SetTip(button, tooltip);
        button.Click += (_, _) => onClick();

        ButtonPanel.Children.Add(button);
        return button;
    }

    /// <summary>
    /// Adds a custom button to the control panel with text content.
    /// </summary>
    /// <param name="text">Text to display on the button</param>
    /// <param name="tooltip">Tooltip text for the button</param>
    /// <param name="onClick">Action to execute when clicked</param>
    /// <returns>The created button for further customization</returns>
    public Button AddTextButton(string text, string tooltip, Action onClick)
    {
        if (ButtonPanel == null)
            throw new InvalidOperationException("ButtonPanel not initialized");

        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold
        };
        textBlock.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("FlowControlsIconStroke"));

        var button = new Button
        {
            Content = textBlock,
            Classes = { "flow-control" }
        };
        ToolTip.SetTip(button, tooltip);
        button.Click += (_, _) => onClick();

        ButtonPanel.Children.Add(button);
        return button;
    }
}
