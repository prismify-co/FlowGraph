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
    
    // Transform: minimapPos = (canvasPos - extentOrigin) * scale
    private double _scale;
    private double _extentX;
    private double _extentY;

    private const double NodeWidth = 150;
    private const double NodeHeight = 80;

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
                    SubscribeToGraph(newCanvas.Graph);
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
        // Viewport changed - need full re-render because extent may change
        RenderMinimap();
    }

    private void OnTargetCanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FlowCanvas.GraphProperty)
        {
            UnsubscribeFromGraph();
            if (e.NewValue is Graph newGraph)
                SubscribeToGraph(newGraph);
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
            node.PropertyChanged += OnNodeChanged;
    }

    private void UnsubscribeFromGraph()
    {
        if (_subscribedGraph == null) return;

        _subscribedGraph.Nodes.CollectionChanged -= OnGraphChanged;
        _subscribedGraph.Edges.CollectionChanged -= OnGraphChanged;

        foreach (var node in _subscribedGraph.Nodes)
            node.PropertyChanged -= OnNodeChanged;

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
            foreach (var item in e.NewItems)
                if (item is Node node)
                    node.PropertyChanged += OnNodeChanged;

        if (e.OldItems != null)
            foreach (var item in e.OldItems)
                if (item is Node node)
                    node.PropertyChanged -= OnNodeChanged;

        RenderMinimap();
    }

    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Node.Position) || e.PropertyName == nameof(Node.IsSelected))
            RenderMinimap();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RenderMinimap();
    }

    /// <summary>
    /// Transform canvas coordinates to minimap coordinates.
    /// Like Nodify: position = canvasPos - Extent.Location, then scaled
    /// </summary>
    private AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
    {
        return new AvaloniaPoint(
            (canvasX - _extentX) * _scale,
            (canvasY - _extentY) * _scale
        );
    }

    /// <summary>
    /// Transform minimap coordinates to canvas coordinates.
    /// </summary>
    private AvaloniaPoint MinimapToCanvas(double minimapX, double minimapY)
    {
        return new AvaloniaPoint(
            minimapX / _scale + _extentX,
            minimapY / _scale + _extentY
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

        // Get minimap display area
        var minimapWidth = _minimapCanvas.Bounds.Width;
        var minimapHeight = _minimapCanvas.Bounds.Height;
        if (minimapWidth <= 0 || minimapHeight <= 0)
        {
            minimapWidth = Bounds.Width - 2;
            minimapHeight = Bounds.Height - 2;
        }
        if (minimapWidth <= 0 || minimapHeight <= 0)
            return;

        // Step 1: Calculate ItemsExtent (bounding box of all nodes)
        var itemsMinX = graph.Nodes.Min(n => n.Position.X);
        var itemsMinY = graph.Nodes.Min(n => n.Position.Y);
        var itemsMaxX = graph.Nodes.Max(n => n.Position.X + NodeWidth);
        var itemsMaxY = graph.Nodes.Max(n => n.Position.Y + NodeHeight);
        var itemsExtent = new Rect(itemsMinX, itemsMinY, itemsMaxX - itemsMinX, itemsMaxY - itemsMinY);

        // Step 2: Get ViewportLocation and ViewportSize (like Nodify)
        // ViewportLocation = top-left of visible area in canvas coords
        // ViewportSize = size of visible area in canvas coords
        var viewport = TargetCanvas.Viewport;
        var viewportRect = viewport.GetVisibleRect();
        
        AvaloniaPoint viewportLocation;
        Size viewportSize;
        
        if (viewportRect.Width > 0 && viewportRect.Height > 0)
        {
            viewportLocation = new AvaloniaPoint(viewportRect.X, viewportRect.Y);
            viewportSize = new Size(viewportRect.Width, viewportRect.Height);
        }
        else
        {
            // Fallback if viewport not ready
            viewportLocation = new AvaloniaPoint(0, 0);
            viewportSize = new Size(0, 0);
        }

        // Step 3: Calculate Extent = union of ItemsExtent and Viewport
        // This is exactly what Nodify does with ResizeToViewport=true
        var extent = itemsExtent;
        if (viewportSize.Width > 0 && viewportSize.Height > 0)
        {
            var viewportBounds = new Rect(viewportLocation, viewportSize);
            extent = extent.Union(viewportBounds);
        }

        // Add padding
        const double padding = 20;
        extent = extent.Inflate(padding);

        if (extent.Width <= 0 || extent.Height <= 0)
            return;

        // Step 4: Calculate scale to fit extent in minimap
        var scaleX = minimapWidth / extent.Width;
        var scaleY = minimapHeight / extent.Height;
        _scale = Math.Min(scaleX, scaleY);

        // Store extent origin for coordinate transforms
        _extentX = extent.X;
        _extentY = extent.Y;

        // Center the content
        var scaledWidth = extent.Width * _scale;
        var scaledHeight = extent.Height * _scale;
        var offsetX = (minimapWidth - scaledWidth) / 2;
        var offsetY = (minimapHeight - scaledHeight) / 2;
        
        // Adjust extent origin to account for centering
        _extentX -= offsetX / _scale;
        _extentY -= offsetY / _scale;

        // Draw edges
        var edgeBrush = new SolidColorBrush(Color.Parse("#808080"));
        foreach (var edge in graph.Edges)
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);
            if (sourceNode == null || targetNode == null) continue;

            var startPos = CanvasToMinimap(
                sourceNode.Position.X + NodeWidth / 2,
                sourceNode.Position.Y + NodeHeight / 2);
            var endPos = CanvasToMinimap(
                targetNode.Position.X + NodeWidth / 2,
                targetNode.Position.Y + NodeHeight / 2);

            _minimapCanvas.Children.Add(new Line
            {
                StartPoint = startPos,
                EndPoint = endPos,
                Stroke = edgeBrush,
                StrokeThickness = 1
            });
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

        // Draw viewport rectangle
        if (viewportSize.Width > 0 && viewportSize.Height > 0)
        {
            var vpTopLeft = CanvasToMinimap(viewportLocation.X, viewportLocation.Y);
            var vpWidth = viewportSize.Width * _scale;
            var vpHeight = viewportSize.Height * _scale;

            _viewportRect = new Rectangle
            {
                Width = Math.Max(vpWidth, 10),
                Height = Math.Max(vpHeight, 10),
                Stroke = new SolidColorBrush(Color.Parse("#0EA5E9")),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(25, 14, 165, 233)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(_viewportRect, vpTopLeft.X);
            Canvas.SetTop(_viewportRect, vpTopLeft.Y);
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
