using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FlowGraph.Avalonia.Controls;

public partial class FlowControls : UserControl
{
    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowControls, FlowCanvas?>(nameof(TargetCanvas));

    public static readonly StyledProperty<FlowDiagnostics?> DiagnosticsPanelProperty =
        AvaloniaProperty.Register<FlowControls, FlowDiagnostics?>(nameof(DiagnosticsPanel));

    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    /// <summary>
    /// The diagnostics panel to show/hide with the debug toggle button.
    /// </summary>
    public FlowDiagnostics? DiagnosticsPanel
    {
        get => GetValue(DiagnosticsPanelProperty);
        set => SetValue(DiagnosticsPanelProperty, value);
    }

    private Button? _zoomInButton;
    private Button? _zoomOutButton;
    private Button? _fitViewButton;
    private Button? _centerButton;
    private Button? _resetZoomButton;
    private Button? _debugToggleButton;

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
        _centerButton = this.FindControl<Button>("CenterButton");
        _resetZoomButton = this.FindControl<Button>("ResetZoomButton");
        _debugToggleButton = this.FindControl<Button>("DebugToggleButton");

        if (_zoomInButton != null)
            _zoomInButton.Click += OnZoomInClick;
        if (_zoomOutButton != null)
            _zoomOutButton.Click += OnZoomOutClick;
        if (_fitViewButton != null)
            _fitViewButton.Click += OnFitViewClick;
        if (_centerButton != null)
            _centerButton.Click += OnCenterClick;
        if (_resetZoomButton != null)
            _resetZoomButton.Click += OnResetZoomClick;
        if (_debugToggleButton != null)
            _debugToggleButton.Click += OnDebugToggleClick;
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

    private void OnCenterClick(object? sender, RoutedEventArgs e)
    {
        TargetCanvas?.CenterOnGraph();
    }

    private void OnResetZoomClick(object? sender, RoutedEventArgs e)
    {
        TargetCanvas?.ResetZoom();
    }

    private void OnDebugToggleClick(object? sender, RoutedEventArgs e)
    {
        if (DiagnosticsPanel != null)
        {
            DiagnosticsPanel.IsVisible = !DiagnosticsPanel.IsVisible;
        }
    }
}
