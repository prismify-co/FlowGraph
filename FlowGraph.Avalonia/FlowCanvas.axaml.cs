using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using FlowGraph.Core;
using System.Collections.Specialized;
using System.ComponentModel;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace FlowGraph.Avalonia;

public partial class FlowCanvas : UserControl
{
    public static readonly StyledProperty<Graph?> GraphProperty =
        AvaloniaProperty.Register<FlowCanvas, Graph?>(nameof(Graph));

    public Graph? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    private Canvas? _mainCanvas;
    private Canvas? _gridCanvas;
    private const double GridSpacing = 20;
    private const double GridDotSize = 2;
    private const double NodeWidth = 150;
    private const double NodeHeight = 80;

    // Dragging state
    private Node? _draggingNode;
    private global::Avalonia.Point _dragStartPoint;
    private Core.Point _nodeStartPosition;

    // Node to visual mapping
    private readonly Dictionary<string, Border> _nodeVisuals = new();

    public FlowCanvas()
    {
        InitializeComponent();
        // Re-render when theme changes
        this.ActualThemeVariantChanged += (_, _) =>
        {
            RenderGrid();
            RenderGraph();
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _mainCanvas = this.FindControl<Canvas>("MainCanvas");
        _gridCanvas = this.FindControl<Canvas>("GridCanvas");
        RenderGrid();
        RenderGraph();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RenderGrid();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GraphProperty)
        {
            if (change.OldValue is Graph oldGraph)
            {
                oldGraph.Nodes.CollectionChanged -= OnNodesChanged;
                oldGraph.Edges.CollectionChanged -= OnEdgesChanged;
                UnsubscribeFromNodeChanges(oldGraph);
            }

            if (change.NewValue is Graph newGraph)
            {
                newGraph.Nodes.CollectionChanged += OnNodesChanged;
                newGraph.Edges.CollectionChanged += OnEdgesChanged;
                SubscribeToNodeChanges(newGraph);
                RenderGraph();
            }
        }
    }

    private void SubscribeToNodeChanges(Graph graph)
    {
        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged += OnNodePropertyChanged;
        }
    }

    private void UnsubscribeFromNodeChanges(Graph graph)
    {
        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Node node)
        {
            if (e.PropertyName == nameof(Node.Position))
            {
                UpdateNodePosition(node);
                RenderEdges();
            }
            else if (e.PropertyName == nameof(Node.IsSelected))
            {
                UpdateNodeSelection(node);
            }
        }
    }

    private void UpdateNodePosition(Node node)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var border))
        {
            Canvas.SetLeft(border, node.Position.X);
            Canvas.SetTop(border, node.Position.Y);
        }
    }

    private void UpdateNodeSelection(Node node)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var border))
        {
            var selectedBorder = GetThemeResource<IBrush>("FlowCanvasNodeSelectedBorder")
                ?? new SolidColorBrush(Color.Parse("#FF6B00"));
            var normalBorder = GetThemeResource<IBrush>("FlowCanvasNodeBorder")
                ?? new SolidColorBrush(Color.Parse("#4682B4"));

            border.BorderBrush = node.IsSelected ? selectedBorder : normalBorder;
            border.BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2);
        }
    }

    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (Node node in e.OldItems)
            {
                node.PropertyChanged -= OnNodePropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (Node node in e.NewItems)
            {
                node.PropertyChanged += OnNodePropertyChanged;
            }
        }

        RenderGraph();
    }

    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderEdges();
    }

    private void RenderGrid()
    {
        if (_gridCanvas == null)
            return;

        _gridCanvas.Children.Clear();

        var width = Bounds.Width;
        var height = Bounds.Height;

        if (width <= 0 || height <= 0)
            return;

        var gridBrush = GetThemeResource<IBrush>("FlowCanvasGridColor")
            ?? new SolidColorBrush(Color.Parse("#333333"));

        // Draw grid dots
        for (double x = GridSpacing; x < width; x += GridSpacing)
        {
            for (double y = GridSpacing; y < height; y += GridSpacing)
            {
                var dot = new Ellipse
                {
                    Width = GridDotSize,
                    Height = GridDotSize,
                    Fill = gridBrush
                };
                Canvas.SetLeft(dot, x - GridDotSize / 2);
                Canvas.SetTop(dot, y - GridDotSize / 2);
                _gridCanvas.Children.Add(dot);
            }
        }
    }

    private void RenderGraph()
    {
        if (_mainCanvas == null || Graph == null)
            return;

        _mainCanvas.Children.Clear();
        _nodeVisuals.Clear();

        RenderEdges();

        foreach (var node in Graph.Nodes)
        {
            RenderNode(node);
        }
    }

    private void RenderEdges()
    {
        if (_mainCanvas == null || Graph == null)
            return;

        // Remove existing edges (keep nodes)
        var edgesToRemove = _mainCanvas.Children.OfType<AvaloniaPath>().ToList();
        foreach (var edge in edgesToRemove)
        {
            _mainCanvas.Children.Remove(edge);
        }

        foreach (var edge in Graph.Edges)
        {
            RenderEdge(edge);
        }
    }

    private void RenderNode(Node node)
    {
        if (_mainCanvas == null)
            return;

        var nodeBackground = GetThemeResource<IBrush>("FlowCanvasNodeBackground")
            ?? new SolidColorBrush(Color.Parse("#2D2D30"));
        var nodeBorder = node.IsSelected
            ? (GetThemeResource<IBrush>("FlowCanvasNodeSelectedBorder") ?? new SolidColorBrush(Color.Parse("#FF6B00")))
            : (GetThemeResource<IBrush>("FlowCanvasNodeBorder") ?? new SolidColorBrush(Color.Parse("#4682B4")));
        var nodeText = GetThemeResource<IBrush>("FlowCanvasNodeText")
            ?? Brushes.White;

        var border = new Border
        {
            Width = NodeWidth,
            Height = NodeHeight,
            Background = nodeBackground,
            BorderBrush = nodeBorder,
            BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2),
            CornerRadius = new CornerRadius(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2,
                OffsetY = 2,
                Blur = 8,
                Color = Color.FromArgb(60, 0, 0, 0)
            }),
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = node,
            Child = new TextBlock
            {
                Text = $"{node.Type}\n{node.Id[..8]}",
                Foreground = nodeText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeight.Medium,
                IsHitTestVisible = false
            }
        };

        // Attach event handlers for dragging
        border.PointerPressed += OnNodePointerPressed;
        border.PointerMoved += OnNodePointerMoved;
        border.PointerReleased += OnNodePointerReleased;

        Canvas.SetLeft(border, node.Position.X);
        Canvas.SetTop(border, node.Position.Y);

        _mainCanvas.Children.Add(border);
        _nodeVisuals[node.Id] = border;
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Node node)
        {
            var point = e.GetCurrentPoint(border);

            if (point.Properties.IsLeftButtonPressed)
            {
                // Handle selection
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    // Deselect all other nodes if Ctrl is not pressed
                    if (Graph != null)
                    {
                        foreach (var n in Graph.Nodes.Where(n => n.Id != node.Id))
                        {
                            n.IsSelected = false;
                        }
                    }
                }

                node.IsSelected = !node.IsSelected || !e.KeyModifiers.HasFlag(KeyModifiers.Control);

                // Start dragging
                _draggingNode = node;
                _draggingNode.IsDragging = true;
                _dragStartPoint = e.GetPosition(_mainCanvas);
                _nodeStartPosition = node.Position;

                e.Pointer.Capture(border);
                e.Handled = true;
            }
        }
    }

    private void OnNodePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingNode != null && sender is Border border)
        {
            var currentPoint = e.GetPosition(_mainCanvas);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            _draggingNode.Position = new Core.Point(
                _nodeStartPosition.X + deltaX,
                _nodeStartPosition.Y + deltaY
            );

            e.Handled = true;
        }
    }

    private void OnNodePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggingNode != null && sender is Border border)
        {
            _draggingNode.IsDragging = false;
            _draggingNode = null;

            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void RenderEdge(Edge edge)
    {
        if (_mainCanvas == null || Graph == null)
            return;

        var sourceNode = Graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = Graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return;

        var edgeStroke = GetThemeResource<IBrush>("FlowCanvasEdgeStroke")
            ?? new SolidColorBrush(Color.Parse("#808080"));

        // Calculate connection points (right side of source, left side of target)
        var sourceX = sourceNode.Position.X + NodeWidth;  // Right edge of source node
        var sourceY = sourceNode.Position.Y + NodeHeight / 2;  // Middle of source node
        var targetX = targetNode.Position.X;       // Left edge of target node
        var targetY = targetNode.Position.Y + NodeHeight / 2;  // Middle of target node

        // Create a bezier curve for a smoother connection
        var pathFigure = new PathFigure
        {
            StartPoint = new global::Avalonia.Point(sourceX, sourceY),
            IsClosed = false
        };

        var controlPointOffset = Math.Abs(targetX - sourceX) / 2;
        var bezierSegment = new BezierSegment
        {
            Point1 = new global::Avalonia.Point(sourceX + controlPointOffset, sourceY),
            Point2 = new global::Avalonia.Point(targetX - controlPointOffset, targetY),
            Point3 = new global::Avalonia.Point(targetX, targetY)
        };

        pathFigure.Segments!.Add(bezierSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        var path = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = edgeStroke,
            StrokeThickness = 2
        };

        // Insert edges at the beginning so nodes render on top
        _mainCanvas.Children.Insert(0, path);
    }

    private T? GetThemeResource<T>(string key) where T : class
    {
        if (this.TryGetResource(key, ActualThemeVariant, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return null;
    }
}
