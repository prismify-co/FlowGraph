using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
    private TextBlock? _transformText;
    private TextBlock? _graphCenterText;
    
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
        _transformText = this.FindControl<TextBlock>("TransformText");
        _graphCenterText = this.FindControl<TextBlock>("GraphCenterText");
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

        // Show actual canvas transform
        if (_transformText != null)
        {
            var canvas = TargetCanvas.FindControl<Canvas>("MainCanvas");
            if (canvas?.RenderTransform is MatrixTransform mt)
            {
                var m = mt.Matrix;
                _transformText.Text = $"M11={m.M11:F2} M22={m.M22:F2} M31={m.M31:F1} M32={m.M32:F1}";
            }
            else if (canvas?.RenderTransform != null)
            {
                _transformText.Text = $"Type: {canvas.RenderTransform.GetType().Name}";
            }
            else
            {
                // Check first node position and count children
                if (canvas != null)
                {
                    var borderCount = canvas.Children.OfType<global::Avalonia.Controls.Border>().Count();
                    var firstChild = canvas.Children.OfType<global::Avalonia.Controls.Border>().FirstOrDefault();
                    if (firstChild != null)
                    {
                        var left = Canvas.GetLeft(firstChild);
                        var top = Canvas.GetTop(firstChild);
                        _transformText.Text = $"Node at ({left:F0}, {top:F0}) [{borderCount} nodes]";
                    }
                    else
                    {
                        _transformText.Text = $"No borders, {canvas.Children.Count} children";
                    }
                }
                else
                {
                    _transformText.Text = "No canvas found";
                }
            }
        }

        // Show graph center
        if (_graphCenterText != null)
        {
            var graph = TargetCanvas.Graph;
            if (graph != null && graph.Nodes.Count > 0)
            {
                var minX = graph.Nodes.Min(n => n.Position.X);
                var minY = graph.Nodes.Min(n => n.Position.Y);
                var maxX = graph.Nodes.Max(n => n.Position.X + 150); // NodeWidth
                var maxY = graph.Nodes.Max(n => n.Position.Y + 80);  // NodeHeight
                var centerX = (minX + maxX) / 2;
                var centerY = (minY + maxY) / 2;
                _graphCenterText.Text = $"({centerX:F0}, {centerY:F0})";
            }
            else
            {
                _graphCenterText.Text = "No nodes";
            }
        }
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
        if (_transformText != null) _transformText.Text = "-";
        if (_graphCenterText != null) _graphCenterText.Text = "-";
    }
}
