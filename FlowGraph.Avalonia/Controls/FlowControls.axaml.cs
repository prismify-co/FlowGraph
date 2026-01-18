using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// A panel with zoom and viewport control buttons for FlowCanvas.
/// </summary>
/// <remarks>
/// Community edition includes: Zoom In, Zoom Out, Fit to View, Reset Zoom.
/// The ButtonPanel is accessible for adding custom buttons in derived classes.
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

    /// <summary>
    /// The panel containing the control buttons. Can be used to add custom buttons.
    /// </summary>
    protected StackPanel? ControlButtonPanel => ButtonPanel;

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
}
