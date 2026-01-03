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
            // Unsubscribe from old canvas
            if (_subscribedCanvas != null)
            {
                _subscribedCanvas.PropertyChanged -= OnTargetCanvasPropertyChanged;
                _subscribedCanvas = null;
            }

            UnsubscribeFromGraph();

            if (change.NewValue is FlowCanvas newCanvas)
            {
                _subscribedCanvas = newCanvas;
                newCanvas.PropertyChanged += OnTargetCanvasPropertyChanged;

                // Subscribe to graph if it's already set
                if (newCanvas.Graph != null)
                {
                    SubscribeToGraph(newCanvas.Graph);
                }
            }
            
            RenderMinimap();
        }
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
        
        if (_subscribedCanvas != null)
        {
            _subscribedCanvas.PropertyChanged -= OnTargetCanvasPropertyChanged;
            _subscribedCanvas = null;
        }
        
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

    private void RenderMinimap()
    {
        if (_minimapCanvas == null || TargetCanvas?.Graph == null)
            return;

        _minimapCanvas.Children.Clear();

        var graph = TargetCanvas.Graph;
        if (graph.Nodes.Count == 0)
            return;

        // Calculate bounding box
        var minX = graph.Nodes.Min(n => n.Position.X);
        var minY = graph.Nodes.Min(n => n.Position.Y);
        var maxX = graph.Nodes.Max(n => n.Position.X + NodeWidth);
        var maxY = graph.Nodes.Max(n => n.Position.Y + NodeHeight);

        var graphWidth = maxX - minX + 100; // Add padding
        var graphHeight = maxY - minY + 100;

        var minimapWidth = Bounds.Width - 8; // Account for border padding
        var minimapHeight = Bounds.Height - 8;

        if (minimapWidth <= 0 || minimapHeight <= 0)
            return;

        // Calculate scale
        var scaleX = minimapWidth / graphWidth;
        var scaleY = minimapHeight / graphHeight;
        var scale = Math.Min(scaleX, scaleY);

        // Calculate offset to center
        var offsetX = (minimapWidth - graphWidth * scale) / 2 - (minX - 50) * scale;
        var offsetY = (minimapHeight - graphHeight * scale) / 2 - (minY - 50) * scale;

        // Draw edges
        var edgeBrush = new SolidColorBrush(Color.Parse("#808080"));
        foreach (var edge in graph.Edges)
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

            if (sourceNode == null || targetNode == null)
                continue;

            var line = new Line
            {
                StartPoint = new AvaloniaPoint(
                    sourceNode.Position.X * scale + offsetX + (NodeWidth * scale / 2),
                    sourceNode.Position.Y * scale + offsetY + (NodeHeight * scale / 2)),
                EndPoint = new AvaloniaPoint(
                    targetNode.Position.X * scale + offsetX + (NodeWidth * scale / 2),
                    targetNode.Position.Y * scale + offsetY + (NodeHeight * scale / 2)),
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
            var rect = new Rectangle
            {
                Width = Math.Max(NodeWidth * scale, 4),
                Height = Math.Max(NodeHeight * scale, 3),
                Fill = node.IsSelected ? selectedBrush : nodeBrush,
                RadiusX = 2,
                RadiusY = 2
            };

            Canvas.SetLeft(rect, node.Position.X * scale + offsetX);
            Canvas.SetTop(rect, node.Position.Y * scale + offsetY);

            _minimapCanvas.Children.Add(rect);
        }

        // Draw viewport rectangle (if we had viewport tracking)
        _viewportRect = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.Parse("#FFFFFF")),
            StrokeThickness = 1,
            Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
        };
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
        if (TargetCanvas?.Graph == null || TargetCanvas.Graph.Nodes.Count == 0)
            return;

        var graph = TargetCanvas.Graph;

        // Calculate the same scale/offset as in RenderMinimap
        var minX = graph.Nodes.Min(n => n.Position.X);
        var minY = graph.Nodes.Min(n => n.Position.Y);
        var maxX = graph.Nodes.Max(n => n.Position.X + NodeWidth);
        var maxY = graph.Nodes.Max(n => n.Position.Y + NodeHeight);

        var graphWidth = maxX - minX + 100;
        var graphHeight = maxY - minY + 100;

        var minimapWidth = Bounds.Width - 8;
        var minimapHeight = Bounds.Height - 8;

        var scaleX = minimapWidth / graphWidth;
        var scaleY = minimapHeight / graphHeight;
        var scale = Math.Min(scaleX, scaleY);

        var offsetX = (minimapWidth - graphWidth * scale) / 2 - (minX - 50) * scale;
        var offsetY = (minimapHeight - graphHeight * scale) / 2 - (minY - 50) * scale;

        // Convert minimap point to graph coordinates
        var graphX = (minimapPoint.X - offsetX) / scale;
        var graphY = (minimapPoint.Y - offsetY) / scale;

        // TODO: Center the canvas on this point when viewport state is exposed
    }
}
