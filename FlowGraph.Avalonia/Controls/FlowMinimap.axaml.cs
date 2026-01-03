using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using FlowGraph.Core;
using System.Collections.Specialized;
using System.ComponentModel;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Controls;

public partial class FlowMinimap : UserControl
{
    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowMinimap, FlowCanvas?>(nameof(TargetCanvas));

    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    private Canvas? _minimapCanvas;
    private Rectangle? _viewportRect;
    private bool _isDragging;
    private Graph? _subscribedGraph;
    private FlowCanvas? _subscribedCanvas;
    private ViewportState? _subscribedViewport;
    
    // Cached transform values for coordinate conversion
    // These transform from canvas coordinates to minimap coordinates
    private double _scale;
    private double _translateX;
    private double _translateY;

    private const double NodeWidth = 150;
    private const double NodeHeight = 80;
    private const double MinimapPadding = 50;

    public FlowMinimap()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _minimapCanvas = this.FindControl<Canvas>("MinimapCanvas");

        if (_minimapCanvas != null)
        {
            _minimapCanvas.PointerPressed += OnMinimapPointerPressed;
            _minimapCanvas.PointerMoved += OnMinimapPointerMoved;
            _minimapCanvas.PointerReleased += OnMinimapPointerReleased;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TargetCanvasProperty)
        {
            // Unsubscribe from old canvas and viewport
            UnsubscribeFromCanvas();
            UnsubscribeFromGraph();

            if (change.NewValue is FlowCanvas newCanvas)
            {
                SubscribeToCanvas(newCanvas);

                // Subscribe to graph if it's already set
                if (newCanvas.Graph != null)
                {
                    SubscribeToGraph(newCanvas.Graph);
                }
            }
            
            RenderMinimap();
        }
    }

    private void SubscribeToCanvas(FlowCanvas canvas)
    {
        _subscribedCanvas = canvas;
        canvas.PropertyChanged += OnTargetCanvasPropertyChanged;
        
        // Subscribe to viewport changes
        _subscribedViewport = canvas.Viewport;
        _subscribedViewport.ViewportChanged += OnViewportChanged;
    }

    private void UnsubscribeFromCanvas()
    {
        if (_subscribedViewport != null)
        {
            _subscribedViewport.ViewportChanged -= OnViewportChanged;
            _subscribedViewport = null;
        }

        if (_subscribedCanvas != null)
        {
            _subscribedCanvas.PropertyChanged -= OnTargetCanvasPropertyChanged;
            _subscribedCanvas = null;
        }
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        // Only update the viewport rectangle, not the entire minimap
        UpdateViewportRect();
    }

    private void OnTargetCanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FlowCanvas.GraphProperty)
        {
            UnsubscribeFromGraph();

            if (e.NewValue is Graph newGraph)
            {
                SubscribeToGraph(newGraph);
            }

            RenderMinimap();
        }
    }

    private void SubscribeToGraph(Graph graph)
    {
        if (_subscribedGraph == graph) return;

        UnsubscribeFromGraph();

        _subscribedGraph = graph;
        graph.Nodes.CollectionChanged += OnGraphChanged;
        graph.Edges.CollectionChanged += OnGraphChanged;

        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged += OnNodeChanged;
        }
    }

    private void UnsubscribeFromGraph()
    {
        if (_subscribedGraph == null) return;

        _subscribedGraph.Nodes.CollectionChanged -= OnGraphChanged;
        _subscribedGraph.Edges.CollectionChanged -= OnGraphChanged;

        foreach (var node in _subscribedGraph.Nodes)
        {
            node.PropertyChanged -= OnNodeChanged;
        }

        _subscribedGraph = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnsubscribeFromCanvas();
        UnsubscribeFromGraph();
    }

    private void OnGraphChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is Node node)
                {
                    node.PropertyChanged += OnNodeChanged;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is Node node)
                {
                    node.PropertyChanged -= OnNodeChanged;
                }
            }
        }

        RenderMinimap();
    }

    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Node.Position) || e.PropertyName == nameof(Node.IsSelected))
        {
            RenderMinimap();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RenderMinimap();
    }

    /// <summary>
    /// Transforms a canvas coordinate to minimap coordinate.
    /// </summary>
    private AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
    {
        return new AvaloniaPoint(
            canvasX * _scale + _translateX,
            canvasY * _scale + _translateY
        );
    }

    /// <summary>
    /// Transforms a minimap coordinate to canvas coordinate.
    /// </summary>
    private AvaloniaPoint MinimapToCanvas(double minimapX, double minimapY)
    {
        return new AvaloniaPoint(
            (minimapX - _translateX) / _scale,
            (minimapY - _translateY) / _scale
        );
    }

    private void RenderMinimap()
    {
        if (_minimapCanvas == null || TargetCanvas?.Graph == null)
            return;

        _minimapCanvas.Children.Clear();
        _viewportRect = null;

        var graph = TargetCanvas.Graph;
        if (graph.Nodes.Count == 0)
            return;

        // Calculate bounding box of all nodes (in canvas coordinates)
        var minX = graph.Nodes.Min(n => n.Position.X) - MinimapPadding;
        var minY = graph.Nodes.Min(n => n.Position.Y) - MinimapPadding;
        var maxX = graph.Nodes.Max(n => n.Position.X + NodeWidth) + MinimapPadding;
        var maxY = graph.Nodes.Max(n => n.Position.Y + NodeHeight) + MinimapPadding;

        var graphWidth = maxX - minX;
        var graphHeight = maxY - minY;

        var minimapWidth = Bounds.Width - 8; // Account for border padding
        var minimapHeight = Bounds.Height - 8;

        if (minimapWidth <= 0 || minimapHeight <= 0 || graphWidth <= 0 || graphHeight <= 0)
            return;

        // Calculate scale to fit graph in minimap
        var scaleX = minimapWidth / graphWidth;
        var scaleY = minimapHeight / graphHeight;
        _scale = Math.Min(scaleX, scaleY);

        // Calculate translation to center the graph in the minimap
        // Formula: minimapPos = canvasPos * scale + translate
        // We want the center of the graph to map to the center of the minimap
        var graphCenterX = (minX + maxX) / 2;
        var graphCenterY = (minY + maxY) / 2;
        var minimapCenterX = minimapWidth / 2;
        var minimapCenterY = minimapHeight / 2;

        _translateX = minimapCenterX - graphCenterX * _scale;
        _translateY = minimapCenterY - graphCenterY * _scale;

        // Draw edges
        var edgeBrush = new SolidColorBrush(Color.Parse("#808080"));
        foreach (var edge in graph.Edges)
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

            if (sourceNode == null || targetNode == null)
                continue;

            var startPos = CanvasToMinimap(
                sourceNode.Position.X + NodeWidth / 2,
                sourceNode.Position.Y + NodeHeight / 2);
            var endPos = CanvasToMinimap(
                targetNode.Position.X + NodeWidth / 2,
                targetNode.Position.Y + NodeHeight / 2);

            var line = new Line
            {
                StartPoint = startPos,
                EndPoint = endPos,
                Stroke = edgeBrush,
                StrokeThickness = 1
            };
            _minimapCanvas.Children.Add(line);
        }

        // Draw nodes
        var nodeBrush = new SolidColorBrush(Color.Parse("#4682B4"));
        var selectedBrush = new SolidColorBrush(Color.Parse("#FF6B00"));
        foreach (var node in graph.Nodes)
        {
            var pos = CanvasToMinimap(node.Position.X, node.Position.Y);

            var rect = new Rectangle
            {
                Width = Math.Max(NodeWidth * _scale, 4),
                Height = Math.Max(NodeHeight * _scale, 3),
                Fill = node.IsSelected ? selectedBrush : nodeBrush,
                RadiusX = 2,
                RadiusY = 2
            };

            Canvas.SetLeft(rect, pos.X);
            Canvas.SetTop(rect, pos.Y);

            _minimapCanvas.Children.Add(rect);
        }

        // Create and add viewport rectangle (ReactFlow style - cyan/blue accent color)
        _viewportRect = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.Parse("#0EA5E9")), // Sky blue - visible on both light/dark
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(25, 14, 165, 233)), // Very subtle fill
            IsHitTestVisible = false
        };
        _minimapCanvas.Children.Add(_viewportRect);
        UpdateViewportRect();
    }

    private void UpdateViewportRect()
    {
        if (_viewportRect == null || TargetCanvas == null || _scale <= 0)
            return;

        var viewport = TargetCanvas.Viewport;
        var visibleRect = viewport.GetVisibleRect();

        // Check if rect is valid (Width > 0 means view size is set)
        if (visibleRect.Width <= 0)
            return;

        // Transform viewport rect corners to minimap coordinates
        var topLeft = CanvasToMinimap(visibleRect.X, visibleRect.Y);
        var size = new Size(visibleRect.Width * _scale, visibleRect.Height * _scale);

        Canvas.SetLeft(_viewportRect, topLeft.X);
        Canvas.SetTop(_viewportRect, topLeft.Y);
        _viewportRect.Width = Math.Max(size.Width, 10);
        _viewportRect.Height = Math.Max(size.Height, 10);
    }

    private void OnMinimapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TargetCanvas?.Graph == null || TargetCanvas.Graph.Nodes.Count == 0)
            return;

        var point = e.GetCurrentPoint(_minimapCanvas);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            NavigateToPoint(e.GetPosition(_minimapCanvas));
            e.Pointer.Capture(_minimapCanvas);
            e.Handled = true;
        }
    }

    private void OnMinimapPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            NavigateToPoint(e.GetPosition(_minimapCanvas));
            e.Handled = true;
        }
    }

    private void OnMinimapPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void NavigateToPoint(AvaloniaPoint minimapPoint)
    {
        if (TargetCanvas == null || _scale <= 0)
            return;

        // Convert minimap point to canvas coordinates
        var canvasPoint = MinimapToCanvas(minimapPoint.X, minimapPoint.Y);

        // Center the canvas on this point
        TargetCanvas.CenterOn(canvasPoint.X, canvasPoint.Y);
    }
}
