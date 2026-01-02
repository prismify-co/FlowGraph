using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FlowGraph.Avalonia.Controls;

public partial class FlowControls : UserControl
{
    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowControls, FlowCanvas?>(nameof(TargetCanvas));

    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    private Button? _zoomInButton;
    private Button? _zoomOutButton;
    private Button? _fitViewButton;
    private Button? _resetZoomButton;

    public FlowControls()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _zoomInButton = this.FindControl<Button>("ZoomInButton");
        _zoomOutButton = this.FindControl<Button>("ZoomOutButton");
        _fitViewButton = this.FindControl<Button>("FitViewButton");
        _resetZoomButton = this.FindControl<Button>("ResetZoomButton");

        if (_zoomInButton != null)
            _zoomInButton.Click += OnZoomInClick;
        if (_zoomOutButton != null)
            _zoomOutButton.Click += OnZoomOutClick;
        if (_fitViewButton != null)
            _fitViewButton.Click += OnFitViewClick;
        if (_resetZoomButton != null)
            _resetZoomButton.Click += OnResetZoomClick;
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        TargetCanvas?.ZoomIn();
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        TargetCanvas?.ZoomOut();
    }

    private void OnFitViewClick(object? sender, RoutedEventArgs e)
    {
        TargetCanvas?.FitToView();
    }

    private void OnResetZoomClick(object? sender, RoutedEventArgs e)
    {
        TargetCanvas?.ResetZoom();
    }
}
