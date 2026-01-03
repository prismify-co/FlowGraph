using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace FlowGraph.Avalonia.Controls;

public partial class FlowDiagnostics : UserControl
{
    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowDiagnostics, FlowCanvas?>(nameof(TargetCanvas));

    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    private TextBlock? _zoomText;
    private TextBlock? _offsetXText;
    private TextBlock? _offsetYText;
    private TextBlock? _viewSizeText;
    private TextBlock? _visibleXText;
    private TextBlock? _visibleYText;
    private TextBlock? _visibleSizeText;
    
    private ViewportState? _subscribedViewport;

    public FlowDiagnostics()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        _zoomText = this.FindControl<TextBlock>("ZoomText");
        _offsetXText = this.FindControl<TextBlock>("OffsetXText");
        _offsetYText = this.FindControl<TextBlock>("OffsetYText");
        _viewSizeText = this.FindControl<TextBlock>("ViewSizeText");
        _visibleXText = this.FindControl<TextBlock>("VisibleXText");
        _visibleYText = this.FindControl<TextBlock>("VisibleYText");
        _visibleSizeText = this.FindControl<TextBlock>("VisibleSizeText");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TargetCanvasProperty)
        {
            UnsubscribeFromViewport();

            if (change.NewValue is FlowCanvas newCanvas)
            {
                SubscribeToViewport(newCanvas.Viewport);
            }
            
            UpdateDisplay();
        }
    }

    private void SubscribeToViewport(ViewportState viewport)
    {
        _subscribedViewport = viewport;
        viewport.ViewportChanged += OnViewportChanged;
    }

    private void UnsubscribeFromViewport()
    {
        if (_subscribedViewport != null)
        {
            _subscribedViewport.ViewportChanged -= OnViewportChanged;
            _subscribedViewport = null;
        }
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        // Update on UI thread
        Dispatcher.UIThread.Post(UpdateDisplay);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnsubscribeFromViewport();
    }

    private void UpdateDisplay()
    {
        if (TargetCanvas == null)
        {
            ClearDisplay();
            return;
        }

        var viewport = TargetCanvas.Viewport;
        var visibleRect = viewport.GetVisibleRect();

        if (_zoomText != null)
            _zoomText.Text = $"{viewport.Zoom:P0} ({viewport.Zoom:F2})";
        
        if (_offsetXText != null)
            _offsetXText.Text = $"{viewport.OffsetX:F1}";
        
        if (_offsetYText != null)
            _offsetYText.Text = $"{viewport.OffsetY:F1}";
        
        if (_viewSizeText != null)
            _viewSizeText.Text = $"{viewport.ViewSize.Width:F0} x {viewport.ViewSize.Height:F0}";
        
        if (_visibleXText != null)
            _visibleXText.Text = $"{visibleRect.X:F1} to {visibleRect.Right:F1}";
        
        if (_visibleYText != null)
            _visibleYText.Text = $"{visibleRect.Y:F1} to {visibleRect.Bottom:F1}";
        
        if (_visibleSizeText != null)
            _visibleSizeText.Text = $"{visibleRect.Width:F0} x {visibleRect.Height:F0}";
    }

    private void ClearDisplay()
    {
        if (_zoomText != null) _zoomText.Text = "-";
        if (_offsetXText != null) _offsetXText.Text = "-";
        if (_offsetYText != null) _offsetYText.Text = "-";
        if (_viewSizeText != null) _viewSizeText.Text = "-";
        if (_visibleXText != null) _visibleXText.Text = "-";
        if (_visibleYText != null) _visibleYText.Text = "-";
        if (_visibleSizeText != null) _visibleSizeText.Text = "-";
    }
}
