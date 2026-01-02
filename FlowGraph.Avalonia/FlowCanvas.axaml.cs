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
    private Panel? _rootPanel;
    private ScaleTransform _canvasScaleTransform = new();
    private TranslateTransform _canvasTranslateTransform = new();
    private ScaleTransform _gridScaleTransform = new();
    private TranslateTransform _gridTranslateTransform = new();

    private const double GridSpacing = 20;
    private const double GridDotSize = 2;
    private const double NodeWidth = 150;
    private const double NodeHeight = 80;
    private const double PortSize = 12;
    private const double PortSpacing = 20;

    // Zoom settings
    private const double MinZoom = 0.1;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.1;
    private double _currentZoom = 1.0;

    // Pan state
    private bool _isPanning;
    private global::Avalonia.Point _panStartPoint;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    // Dragging state
    private Node? _draggingNode;
    private global::Avalonia.Point _dragStartPoint;
    private Core.Point _nodeStartPosition;

    // Connection dragging state
    private bool _isCreatingConnection;
    private Node? _connectionSourceNode;
    private Port? _connectionSourcePort;
    private bool _connectionFromOutput;
    private global::Avalonia.Point _connectionEndPoint;
    private AvaloniaPath? _tempConnectionLine;

    // Node to visual mapping
    private readonly Dictionary<string, Border> _nodeVisuals = new();
    private readonly Dictionary<(string nodeId, string portId), Ellipse> _portVisuals = new();

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
        _rootPanel = this.FindControl<Panel>("RootPanel");

        // Set up transforms programmatically
        if (_mainCanvas != null)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_canvasScaleTransform);
            transformGroup.Children.Add(_canvasTranslateTransform);
            _mainCanvas.RenderTransform = transformGroup;
        }

        if (_gridCanvas != null)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_gridScaleTransform);
            transformGroup.Children.Add(_gridTranslateTransform);
            _gridCanvas.RenderTransform = transformGroup;
        }

        if (_rootPanel != null)
        {
            _rootPanel.PointerPressed += OnRootPanelPointerPressed;
            _rootPanel.PointerMoved += OnRootPanelPointerMoved;
            _rootPanel.PointerReleased += OnRootPanelPointerReleased;
            _rootPanel.PointerWheelChanged += OnPointerWheelChanged;
        }

        RenderGrid();
        RenderGraph();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            DeleteSelectedNodes();
            e.Handled = true;
        }
        else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SelectAllNodes();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            DeselectAllNodes();
            e.Handled = true;
        }
    }

    private void DeleteSelectedNodes()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes.Where(n => n.IsSelected).ToList();
        foreach (var node in selectedNodes)
        {
            Graph.RemoveNode(node.Id);
        }
    }

    private void SelectAllNodes()
    {
        if (Graph == null) return;

        foreach (var node in Graph.Nodes)
        {
            node.IsSelected = true;
        }
    }

    private void DeselectAllNodes()
    {
        if (Graph == null) return;

        foreach (var node in Graph.Nodes)
        {
            node.IsSelected = false;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var position = e.GetPosition(_rootPanel);
        var delta = e.Delta.Y > 0 ? ZoomStep : -ZoomStep;
        var newZoom = Math.Clamp(_currentZoom + delta, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - _currentZoom) > 0.001)
        {
            // Calculate zoom center point
            var oldZoom = _currentZoom;
            _currentZoom = newZoom;

            // Adjust pan to zoom towards mouse position
            var zoomFactor = newZoom / oldZoom;
            var offsetX = position.X - (position.X - _canvasTranslateTransform.X) * zoomFactor;
            var offsetY = position.Y - (position.Y - _canvasTranslateTransform.Y) * zoomFactor;

            _canvasScaleTransform.ScaleX = newZoom;
            _canvasScaleTransform.ScaleY = newZoom;
            _canvasTranslateTransform.X = offsetX;
            _canvasTranslateTransform.Y = offsetY;

            _gridScaleTransform.ScaleX = newZoom;
            _gridScaleTransform.ScaleY = newZoom;
            _gridTranslateTransform.X = offsetX;
            _gridTranslateTransform.Y = offsetY;

            RenderGrid();
        }

        e.Handled = true;
    }

    private void OnRootPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_rootPanel);

        // Middle mouse button or Shift + Left click for panning
        if (point.Properties.IsMiddleButtonPressed ||
            (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(_rootPanel);
            _panStartOffsetX = _canvasTranslateTransform.X;
            _panStartOffsetY = _canvasTranslateTransform.Y;
            e.Pointer.Capture((IInputElement?)_rootPanel);
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            // Click on empty canvas - deselect all
            var hitElement = _mainCanvas?.InputHitTest(TransformPointToCanvas(e.GetPosition(_rootPanel)));
            if (hitElement == null || hitElement == _mainCanvas)
            {
                DeselectAllNodes();
            }

            // Take focus for keyboard input
            Focus();
        }
    }

    private void OnRootPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning)
        {
            var currentPoint = e.GetPosition(_rootPanel);
            var deltaX = currentPoint.X - _panStartPoint.X;
            var deltaY = currentPoint.Y - _panStartPoint.Y;

            _canvasTranslateTransform.X = _panStartOffsetX + deltaX;
            _canvasTranslateTransform.Y = _panStartOffsetY + deltaY;
            _gridTranslateTransform.X = _panStartOffsetX + deltaX;
            _gridTranslateTransform.Y = _panStartOffsetY + deltaY;

            RenderGrid();
            e.Handled = true;
        }
        else if (_isCreatingConnection && _mainCanvas != null)
        {
            _connectionEndPoint = TransformPointToCanvas(e.GetPosition(_rootPanel));
            UpdateTempConnectionLine();
        }
    }

    private void OnRootPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        if (_isCreatingConnection)
        {
            CompleteConnection(e);
        }
    }

    private global::Avalonia.Point TransformPointToCanvas(global::Avalonia.Point point)
    {
        // Transform screen point to canvas coordinates (accounting for pan and zoom)
        var offsetX = _canvasTranslateTransform.X;
        var offsetY = _canvasTranslateTransform.Y;
        return new global::Avalonia.Point(
            (point.X - offsetX) / _currentZoom,
            (point.Y - offsetY) / _currentZoom
        );
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
                UpdatePortPositions(node);
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

    private void UpdatePortPositions(Node node)
    {
        // Update input port positions
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var portY = GetPortY(node, i, node.Inputs.Count);
                Canvas.SetLeft(portVisual, node.Position.X - PortSize / 2);
                Canvas.SetTop(portVisual, portY - PortSize / 2);
            }
        }

        // Update output port positions
        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var port = node.Outputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var portY = GetPortY(node, i, node.Outputs.Count);
                Canvas.SetLeft(portVisual, node.Position.X + NodeWidth - PortSize / 2);
                Canvas.SetTop(portVisual, portY - PortSize / 2);
            }
        }
    }

    private double GetPortY(Node node, int portIndex, int totalPorts)
    {
        if (totalPorts == 1)
        {
            return node.Position.Y + NodeHeight / 2;
        }

        var spacing = NodeHeight / (totalPorts + 1);
        return node.Position.Y + spacing * (portIndex + 1);
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

        // Calculate visible area in canvas coordinates
        var offsetX = _gridTranslateTransform.X;
        var offsetY = _gridTranslateTransform.Y;
        var zoom = _currentZoom;

        // Expand grid to cover visible area with some padding
        var startX = -offsetX / zoom - GridSpacing;
        var startY = -offsetY / zoom - GridSpacing;
        var endX = (width - offsetX) / zoom + GridSpacing;
        var endY = (height - offsetY) / zoom + GridSpacing;

        // Snap to grid
        startX = Math.Floor(startX / GridSpacing) * GridSpacing;
        startY = Math.Floor(startY / GridSpacing) * GridSpacing;

        // Adjust dot size based on zoom
        var dotSize = Math.Max(GridDotSize / zoom, 1);

        // Draw grid dots
        for (double x = startX; x < endX; x += GridSpacing)
        {
            for (double y = startY; y < endY; y += GridSpacing)
            {
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = gridBrush
                };
                Canvas.SetLeft(dot, x - dotSize / 2);
                Canvas.SetTop(dot, y - dotSize / 2);
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
        _portVisuals.Clear();

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

        // Remove existing edges (keep nodes and ports)
        var edgesToRemove = _mainCanvas.Children.OfType<AvaloniaPath>().Where(p => p != _tempConnectionLine).ToList();
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

        // Render input ports
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            RenderPort(node, node.Inputs[i], i, node.Inputs.Count, isOutput: false);
        }

        // Render output ports
        for (int i = 0; i < node.Outputs.Count; i++)
        {
            RenderPort(node, node.Outputs[i], i, node.Outputs.Count, isOutput: true);
        }
    }

    private void RenderPort(Node node, Port port, int index, int totalPorts, bool isOutput)
    {
        if (_mainCanvas == null)
            return;

        var portBackground = GetThemeResource<IBrush>("FlowCanvasPortBackground")
            ?? new SolidColorBrush(Color.Parse("#4682B4"));
        var portBorder = GetThemeResource<IBrush>("FlowCanvasPortBorder")
            ?? new SolidColorBrush(Color.Parse("#FFFFFF"));

        var portY = GetPortY(node, index, totalPorts);
        var portX = isOutput ? node.Position.X + NodeWidth : node.Position.X;

        var portVisual = new Ellipse
        {
            Width = PortSize,
            Height = PortSize,
            Fill = portBackground,
            Stroke = portBorder,
            StrokeThickness = 2,
            Cursor = new Cursor(StandardCursorType.Cross),
            Tag = (node, port, isOutput)
        };

        portVisual.PointerPressed += OnPortPointerPressed;
        portVisual.PointerEntered += OnPortPointerEntered;
        portVisual.PointerExited += OnPortPointerExited;

        Canvas.SetLeft(portVisual, portX - PortSize / 2);
        Canvas.SetTop(portVisual, portY - PortSize / 2);

        _mainCanvas.Children.Add(portVisual);
        _portVisuals[(node.Id, port.Id)] = portVisual;
    }

    private void OnPortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Ellipse portVisual && portVisual.Tag is (Node node, Port port, bool isOutput))
        {
            var point = e.GetCurrentPoint(portVisual);

            if (point.Properties.IsLeftButtonPressed)
            {
                _isCreatingConnection = true;
                _connectionSourceNode = node;
                _connectionSourcePort = port;
                _connectionFromOutput = isOutput;
                _connectionEndPoint = TransformPointToCanvas(e.GetPosition(_rootPanel));

                // Create temporary connection line
                _tempConnectionLine = new AvaloniaPath
                {
                    Stroke = GetThemeResource<IBrush>("FlowCanvasEdgeStroke") ?? new SolidColorBrush(Color.Parse("#808080")),
                    StrokeThickness = 2,
                    StrokeDashArray = [5, 3],
                    Opacity = 0.7
                };
                _mainCanvas?.Children.Add(_tempConnectionLine);
                UpdateTempConnectionLine();

                e.Pointer.Capture(portVisual);
                e.Handled = true;
            }
        }
    }

    private void OnPortPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse portVisual)
        {
            var hoverBrush = GetThemeResource<IBrush>("FlowCanvasPortHover")
                ?? new SolidColorBrush(Color.Parse("#FF6B00"));
            portVisual.Fill = hoverBrush;
        }
    }

    private void OnPortPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse portVisual)
        {
            var normalBrush = GetThemeResource<IBrush>("FlowCanvasPortBackground")
                ?? new SolidColorBrush(Color.Parse("#4682B4"));
            portVisual.Fill = normalBrush;
        }
    }

    private void CompleteConnection(PointerReleasedEventArgs e)
    {
        // Check if released over a valid port
        var canvasPoint = TransformPointToCanvas(e.GetPosition(_rootPanel));
        var hitElement = _mainCanvas?.InputHitTest(canvasPoint);
        if (hitElement is Ellipse portVisual && portVisual.Tag is (Node targetNode, Port targetPort, bool isOutput))
        {
            // Can only connect output to input (or input to output)
            if (_connectionFromOutput != isOutput && _connectionSourceNode != null && _connectionSourcePort != null)
            {
                // Determine source and target based on direction
                var sourceNode = _connectionFromOutput ? _connectionSourceNode : targetNode;
                var sourcePort = _connectionFromOutput ? _connectionSourcePort : targetPort;
                var destNode = _connectionFromOutput ? targetNode : _connectionSourceNode;
                var destPort = _connectionFromOutput ? targetPort : _connectionSourcePort;

                // Check if connection already exists
                var existingEdge = Graph?.Edges.FirstOrDefault(edge =>
                    edge.Source == sourceNode.Id && edge.Target == destNode.Id &&
                    edge.SourcePort == sourcePort.Id && edge.TargetPort == destPort.Id);

                if (existingEdge == null)
                {
                    Graph?.AddEdge(new Edge
                    {
                        Source = sourceNode.Id,
                        Target = destNode.Id,
                        SourcePort = sourcePort.Id,
                        TargetPort = destPort.Id
                    });
                }
            }
        }

        // Clean up
        if (_tempConnectionLine != null)
        {
            _mainCanvas?.Children.Remove(_tempConnectionLine);
            _tempConnectionLine = null;
        }

        _isCreatingConnection = false;
        _connectionSourceNode = null;
        _connectionSourcePort = null;

        e.Pointer.Capture(null);
    }

    private void UpdateTempConnectionLine()
    {
        if (_tempConnectionLine == null || _connectionSourceNode == null || _connectionSourcePort == null)
            return;

        var sourcePortIndex = _connectionFromOutput
            ? _connectionSourceNode.Outputs.IndexOf(_connectionSourcePort)
            : _connectionSourceNode.Inputs.IndexOf(_connectionSourcePort);
        var totalPorts = _connectionFromOutput
            ? _connectionSourceNode.Outputs.Count
            : _connectionSourceNode.Inputs.Count;

        var sourceY = GetPortY(_connectionSourceNode, sourcePortIndex, totalPorts);
        var sourceX = _connectionFromOutput
            ? _connectionSourceNode.Position.X + NodeWidth
            : _connectionSourceNode.Position.X;

        var pathFigure = new PathFigure
        {
            StartPoint = new global::Avalonia.Point(sourceX, sourceY),
            IsClosed = false
        };

        var controlPointOffset = Math.Abs(_connectionEndPoint.X - sourceX) / 2;
        var bezierSegment = new BezierSegment
        {
            Point1 = new global::Avalonia.Point(
                _connectionFromOutput ? sourceX + controlPointOffset : sourceX - controlPointOffset,
                sourceY),
            Point2 = new global::Avalonia.Point(
                _connectionFromOutput ? _connectionEndPoint.X - controlPointOffset : _connectionEndPoint.X + controlPointOffset,
                _connectionEndPoint.Y),
            Point3 = _connectionEndPoint
        };

        pathFigure.Segments!.Add(bezierSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        _tempConnectionLine.Data = pathGeometry;
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
                _dragStartPoint = TransformPointToCanvas(e.GetPosition(_rootPanel));
                _nodeStartPosition = node.Position;

                e.Pointer.Capture(border);
                e.Handled = true;

                // Take focus for keyboard input
                Focus();
            }
        }
    }

    private void OnNodePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingNode != null && sender is Border border)
        {
            var currentPoint = TransformPointToCanvas(e.GetPosition(_rootPanel));
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

        // Find port indices
        var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
        var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);

        if (sourcePortIndex < 0) sourcePortIndex = 0;
        if (targetPortIndex < 0) targetPortIndex = 0;

        var sourceY = GetPortY(sourceNode, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count));
        var targetY = GetPortY(targetNode, targetPortIndex, Math.Max(1, targetNode.Inputs.Count));

        var sourceX = sourceNode.Position.X + NodeWidth;
        var targetX = targetNode.Position.X;

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

    // Public methods for external control
    public void ZoomIn() => SetZoom(_currentZoom + ZoomStep);
    public void ZoomOut() => SetZoom(_currentZoom - ZoomStep);
    public void ResetZoom() => SetZoom(1.0);

    public void SetZoom(double zoom)
    {
        _currentZoom = Math.Clamp(zoom, MinZoom, MaxZoom);

        _canvasScaleTransform.ScaleX = _currentZoom;
        _canvasScaleTransform.ScaleY = _currentZoom;
        _gridScaleTransform.ScaleX = _currentZoom;
        _gridScaleTransform.ScaleY = _currentZoom;

        RenderGrid();
    }

    public void FitToView()
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        // Calculate bounding box of all nodes
        var minX = Graph.Nodes.Min(n => n.Position.X);
        var minY = Graph.Nodes.Min(n => n.Position.Y);
        var maxX = Graph.Nodes.Max(n => n.Position.X + NodeWidth);
        var maxY = Graph.Nodes.Max(n => n.Position.Y + NodeHeight);

        var graphWidth = maxX - minX;
        var graphHeight = maxY - minY;

        var viewWidth = Bounds.Width;
        var viewHeight = Bounds.Height;

        if (graphWidth <= 0 || graphHeight <= 0) return;

        // Calculate zoom to fit with padding
        var padding = 50;
        var zoomX = (viewWidth - padding * 2) / graphWidth;
        var zoomY = (viewHeight - padding * 2) / graphHeight;
        var newZoom = Math.Clamp(Math.Min(zoomX, zoomY), MinZoom, MaxZoom);

        _currentZoom = newZoom;

        // Center the graph
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        var offsetX = viewWidth / 2 - centerX * newZoom;
        var offsetY = viewHeight / 2 - centerY * newZoom;

        _canvasScaleTransform.ScaleX = newZoom;
        _canvasScaleTransform.ScaleY = newZoom;
        _canvasTranslateTransform.X = offsetX;
        _canvasTranslateTransform.Y = offsetY;

        _gridScaleTransform.ScaleX = newZoom;
        _gridScaleTransform.ScaleY = newZoom;
        _gridTranslateTransform.X = offsetX;
        _gridTranslateTransform.Y = offsetY;

        RenderGrid();
    }
}
