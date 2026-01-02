using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Styling;
using FlowGraph.Core;
using System.Collections.Specialized;
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
            }

            if (change.NewValue is Graph newGraph)
            {
                newGraph.Nodes.CollectionChanged += OnNodesChanged;
                newGraph.Edges.CollectionChanged += OnEdgesChanged;
                RenderGraph();
            }
        }
    }

    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderGraph();
    }

    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderGraph();
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

        foreach (var edge in Graph.Edges)
        {
            RenderEdge(edge);
        }

        foreach (var node in Graph.Nodes)
        {
            RenderNode(node);
        }
    }

    private void RenderNode(Node node)
    {
        if (_mainCanvas == null)
            return;

        var nodeBackground = GetThemeResource<IBrush>("FlowCanvasNodeBackground") 
            ?? new SolidColorBrush(Color.Parse("#2D2D30"));
        var nodeBorder = GetThemeResource<IBrush>("FlowCanvasNodeBorder") 
            ?? new SolidColorBrush(Color.Parse("#4682B4"));
        var nodeText = GetThemeResource<IBrush>("FlowCanvasNodeText") 
            ?? Brushes.White;

        var border = new Border
        {
            Width = 150,
            Height = 80,
            Background = nodeBackground,
            BorderBrush = nodeBorder,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2,
                OffsetY = 2,
                Blur = 8,
                Color = Color.FromArgb(60, 0, 0, 0)
            }),
            Child = new TextBlock
            {
                Text = $"{node.Type}\n{node.Id[..8]}",
                Foreground = nodeText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeight.Medium
            }
        };

        Canvas.SetLeft(border, node.Position.X);
        Canvas.SetTop(border, node.Position.Y);

        _mainCanvas.Children.Add(border);
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
        var sourceX = sourceNode.Position.X + 150; // Right edge of source node
        var sourceY = sourceNode.Position.Y + 40;  // Middle of source node
        var targetX = targetNode.Position.X;       // Left edge of target node
        var targetY = targetNode.Position.Y + 40;  // Middle of target node

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

        _mainCanvas.Children.Add(path);
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
