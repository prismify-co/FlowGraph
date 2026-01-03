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
    
    // Transform from canvas coordinates to minimap coordinates
    // Formula: minimapPos = (canvasPos - boundingRect.TopLeft) * scale
    private double _scale;
    private double _boundingRectX;
    private double _boundingRectY;

    private const double NodeWidth = 150;
    private const double NodeHeight = 80;
    private const double OffsetScale = 5; // Padding factor like ReactFlow

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
            UnsubscribeFromCanvas();
            UnsubscribeFromGraph();

            if (change.NewValue is FlowCanvas newCanvas)
            {
                SubscribeToCanvas(newCanvas);

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
        // ReactFlow recalculates the bounding rect on every viewport change
        // because the bounding rect is the union of nodes AND viewport
        RenderMinimap();
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
                    node.PropertyChanged += OnNodeChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is Node node)
                    node.PropertyChanged -= OnNodeChanged;
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
    /// Transforms canvas coordinates to minimap coordinates.
    /// </summary>
    private AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
    {
        return new AvaloniaPoint(
            (canvasX - _boundingRectX) * _scale,
            (canvasY - _boundingRectY) * _scale
        );
    }

    /// <summary>
    /// Transforms minimap coordinates to canvas coordinates.
    /// </summary>
    private AvaloniaPoint MinimapToCanvas(double minimapX, double minimapY)
    {
        return new AvaloniaPoint(
            minimapX / _scale + _boundingRectX,
            minimapY / _scale + _boundingRectY
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

        // Get minimap display size
        var minimapWidth = _minimapCanvas.Bounds.Width;
        var minimapHeight = _minimapCanvas.Bounds.Height;

        if (minimapWidth <= 0 || minimapHeight <= 0)
        {
            minimapWidth = Bounds.Width - 2;
            minimapHeight = Bounds.Height - 2;
        }

        if (minimapWidth <= 0 || minimapHeight <= 0)
            return;

        // Step 1: Calculate viewBB (visible viewport in canvas coordinates)
        // This is exactly how ReactFlow does it: viewBB = { x: -offsetX/zoom, y: -offsetY/zoom, width: viewWidth/zoom, height: viewHeight/zoom }
        var viewport = TargetCanvas.Viewport;
        var viewBB = viewport.GetVisibleRect();

        // Step 2: Calculate node bounds in canvas coordinates
        var nodeMinX = graph.Nodes.Min(n => n.Position.X);
        var nodeMinY = graph.Nodes.Min(n => n.Position.Y);
        var nodeMaxX = graph.Nodes.Max(n => n.Position.X + NodeWidth);
        var nodeMaxY = graph.Nodes.Max(n => n.Position.Y + NodeHeight);

        // Step 3: Calculate boundingRect as union of nodeBounds AND viewBB (ReactFlow's approach)
        double boundingMinX, boundingMinY, boundingMaxX, boundingMaxY;
        
        if (viewBB.Width > 0 && viewBB.Height > 0)
        {
            // Union of node bounds and viewport bounds
            boundingMinX = Math.Min(nodeMinX, viewBB.X);
            boundingMinY = Math.Min(nodeMinY, viewBB.Y);
            boundingMaxX = Math.Max(nodeMaxX, viewBB.Right);
            boundingMaxY = Math.Max(nodeMaxY, viewBB.Bottom);
        }
        else
        {
            boundingMinX = nodeMinX;
            boundingMinY = nodeMinY;
            boundingMaxX = nodeMaxX;
            boundingMaxY = nodeMaxY;
        }

        var boundingWidth = boundingMaxX - boundingMinX;
        var boundingHeight = boundingMaxY - boundingMinY;

        if (boundingWidth <= 0 || boundingHeight <= 0)
            return;

        // Step 4: Calculate scale to fit boundingRect in minimap (like ReactFlow's viewScale)
        var scaleX = minimapWidth / boundingWidth;
        var scaleY = minimapHeight / boundingHeight;
        var viewScale = Math.Min(scaleX, scaleY);

        // Add offset/padding
        var offset = OffsetScale * viewScale;
        
        // Adjust bounding rect with padding
        _boundingRectX = boundingMinX - offset / viewScale;
        _boundingRectY = boundingMinY - offset / viewScale;
        var totalWidth = boundingWidth + 2 * offset / viewScale;
        var totalHeight = boundingHeight + 2 * offset / viewScale;

        // Recalculate scale with padding
        scaleX = minimapWidth / totalWidth;
        scaleY = minimapHeight / totalHeight;
        _scale = Math.Min(scaleX, scaleY);

        // Center the content in the minimap
        var scaledWidth = totalWidth * _scale;
        var scaledHeight = totalHeight * _scale;
        var offsetX = (minimapWidth - scaledWidth) / 2;
        var offsetY = (minimapHeight - scaledHeight) / 2;

        // Adjust bounding rect origin to account for centering
        _boundingRectX -= offsetX / _scale;
        _boundingRectY -= offsetY / _scale;

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

        // Draw viewport rectangle using the same coordinate transform
        if (viewBB.Width > 0 && viewBB.Height > 0)
        {
            var viewportTopLeft = CanvasToMinimap(viewBB.X, viewBB.Y);
            var viewportWidth = viewBB.Width * _scale;
            var viewportHeight = viewBB.Height * _scale;

            _viewportRect = new Rectangle
            {
                Stroke = new SolidColorBrush(Color.Parse("#0EA5E9")),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(25, 14, 165, 233)),
                IsHitTestVisible = false,
                Width = Math.Max(viewportWidth, 10),
                Height = Math.Max(viewportHeight, 10)
            };

            Canvas.SetLeft(_viewportRect, viewportTopLeft.X);
            Canvas.SetTop(_viewportRect, viewportTopLeft.Y);

            _minimapCanvas.Children.Add(_viewportRect);
        }
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

        var canvasPoint = MinimapToCanvas(minimapPoint.X, minimapPoint.Y);
        TargetCanvas.CenterOn(canvasPoint.X, canvasPoint.Y);
    }
}
