using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia;

/// <summary>
/// Handles all input interactions for the FlowCanvas including 
/// pan, zoom, node dragging, and connection creation.
/// </summary>
public class CanvasInputHandler
{
    private readonly FlowCanvasSettings _settings;
    private readonly ViewportState _viewport;
    private readonly GraphRenderer _graphRenderer;

    // Pan state
    private bool _isPanning;
    private AvaloniaPoint _panStartPoint;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    // Node drag state
    private Node? _draggingNode;
    private AvaloniaPoint _dragStartPoint;
    private Core.Point _nodeStartPosition;

    // Connection creation state
    private bool _isCreatingConnection;
    private Node? _connectionSourceNode;
    private Port? _connectionSourcePort;
    private bool _connectionFromOutput;
    private AvaloniaPoint _connectionEndPoint;
    private AvaloniaPath? _tempConnectionLine;
    private Canvas? _canvas;

    /// <summary>
    /// Event raised when a connection is completed.
    /// </summary>
    public event EventHandler<ConnectionCompletedEventArgs>? ConnectionCompleted;

    /// <summary>
    /// Event raised when nodes should be deselected.
    /// </summary>
    public event EventHandler? DeselectAllRequested;

    /// <summary>
    /// Event raised when all nodes should be selected.
    /// </summary>
    public event EventHandler? SelectAllRequested;

    /// <summary>
    /// Event raised when selected nodes should be deleted.
    /// </summary>
    public event EventHandler? DeleteSelectedRequested;

    /// <summary>
    /// Event raised when the grid needs to be re-rendered.
    /// </summary>
    public event EventHandler? GridRenderRequested;

    public CanvasInputHandler(
        FlowCanvasSettings settings,
        ViewportState viewport,
        GraphRenderer graphRenderer)
    {
        _settings = settings;
        _viewport = viewport;
        _graphRenderer = graphRenderer;
    }

    /// <summary>
    /// Handles keyboard input.
    /// </summary>
    public bool HandleKeyDown(KeyEventArgs e, Graph? graph)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }
        else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SelectAllRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelConnection();
            DeselectAllRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles mouse wheel for zooming.
    /// </summary>
    public void HandlePointerWheelChanged(PointerWheelEventArgs e, Panel? rootPanel)
    {
        var position = e.GetPosition(rootPanel);
        
        if (e.Delta.Y > 0)
            _viewport.ZoomIn(position);
        else
            _viewport.ZoomOut(position);

        GridRenderRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer pressed on the root panel.
    /// </summary>
    public void HandleRootPanelPointerPressed(PointerPressedEventArgs e, Panel? rootPanel, Canvas? mainCanvas)
    {
        var point = e.GetCurrentPoint(rootPanel);

        // Middle mouse button or Shift + Left click for panning
        if (point.Properties.IsMiddleButtonPressed ||
            (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            StartPanning(e.GetPosition(rootPanel));
            e.Pointer.Capture((IInputElement?)rootPanel);
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            // Check if clicking on empty canvas
            var canvasPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
            var hitElement = mainCanvas?.InputHitTest(canvasPoint);
            
            if (hitElement == null || hitElement == mainCanvas)
            {
                DeselectAllRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Handles pointer moved on the root panel.
    /// </summary>
    public void HandleRootPanelPointerMoved(PointerEventArgs e, Panel? rootPanel)
    {
        if (_isPanning)
        {
            var currentPoint = e.GetPosition(rootPanel);
            var deltaX = currentPoint.X - _panStartPoint.X;
            var deltaY = currentPoint.Y - _panStartPoint.Y;

            _viewport.SetOffset(_panStartOffsetX + deltaX, _panStartOffsetY + deltaY);
            GridRenderRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
        else if (_isCreatingConnection)
        {
            _connectionEndPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
            UpdateTempConnectionLine();
        }
    }

    /// <summary>
    /// Handles pointer released on the root panel.
    /// </summary>
    public void HandleRootPanelPointerReleased(PointerReleasedEventArgs e, Panel? rootPanel, Canvas? mainCanvas, Graph? graph)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        if (_isCreatingConnection)
        {
            CompleteConnection(e, rootPanel, mainCanvas, graph);
        }
    }

    /// <summary>
    /// Handles node pointer pressed.
    /// </summary>
    public void HandleNodePointerPressed(Border border, Node node, PointerPressedEventArgs e, Panel? rootPanel, Graph? graph)
    {
        var point = e.GetCurrentPoint(border);

        if (point.Properties.IsLeftButtonPressed)
        {
            // Handle selection
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                // Deselect all other nodes if Ctrl is not pressed
                if (graph != null)
                {
                    foreach (var n in graph.Nodes.Where(n => n.Id != node.Id))
                    {
                        n.IsSelected = false;
                    }
                }
            }

            node.IsSelected = !node.IsSelected || !e.KeyModifiers.HasFlag(KeyModifiers.Control);

            // Start dragging
            _draggingNode = node;
            _draggingNode.IsDragging = true;
            _dragStartPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
            _nodeStartPosition = node.Position;

            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles node pointer moved.
    /// </summary>
    public void HandleNodePointerMoved(PointerEventArgs e, Panel? rootPanel)
    {
        if (_draggingNode != null)
        {
            var currentPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            _draggingNode.Position = new Core.Point(
                _nodeStartPosition.X + deltaX,
                _nodeStartPosition.Y + deltaY
            );

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles node pointer released.
    /// </summary>
    public void HandleNodePointerReleased(PointerReleasedEventArgs e)
    {
        if (_draggingNode != null)
        {
            _draggingNode.IsDragging = false;
            _draggingNode = null;

            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles port pointer pressed to start connection creation.
    /// </summary>
    public void HandlePortPointerPressed(
        Ellipse portVisual, 
        Node node, 
        Port port, 
        bool isOutput, 
        PointerPressedEventArgs e, 
        Panel? rootPanel, 
        Canvas? canvas, 
        ThemeResources theme)
    {
        var point = e.GetCurrentPoint(portVisual);

        if (point.Properties.IsLeftButtonPressed)
        {
            _canvas = canvas;
            _isCreatingConnection = true;
            _connectionSourceNode = node;
            _connectionSourcePort = port;
            _connectionFromOutput = isOutput;
            _connectionEndPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));

            // Create temporary connection line
            _tempConnectionLine = new AvaloniaPath
            {
                Stroke = theme.EdgeStroke,
                StrokeThickness = 2,
                StrokeDashArray = [5, 3],
                Opacity = 0.7
            };
            canvas?.Children.Add(_tempConnectionLine);
            UpdateTempConnectionLine();

            e.Pointer.Capture(portVisual);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles port pointer entered (hover).
    /// </summary>
    public void HandlePortPointerEntered(Ellipse portVisual, ThemeResources theme)
    {
        portVisual.Fill = theme.PortHover;
    }

    /// <summary>
    /// Handles port pointer exited (unhover).
    /// </summary>
    public void HandlePortPointerExited(Ellipse portVisual, ThemeResources theme)
    {
        portVisual.Fill = theme.PortBackground;
    }

    private void StartPanning(AvaloniaPoint position)
    {
        _isPanning = true;
        _panStartPoint = position;
        _panStartOffsetX = _viewport.OffsetX;
        _panStartOffsetY = _viewport.OffsetY;
    }

    private void CancelConnection()
    {
        if (_tempConnectionLine != null && _canvas != null)
        {
            _canvas.Children.Remove(_tempConnectionLine);
            _tempConnectionLine = null;
        }

        _isCreatingConnection = false;
        _connectionSourceNode = null;
        _connectionSourcePort = null;
    }

    private void CompleteConnection(PointerReleasedEventArgs e, Panel? rootPanel, Canvas? mainCanvas, Graph? graph)
    {
        var canvasPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
        var hitElement = mainCanvas?.InputHitTest(canvasPoint);

        if (hitElement is Ellipse portVisual && 
            portVisual.Tag is (Node targetNode, Port targetPort, bool isOutput))
        {
            // Can only connect output to input (or input to output)
            if (_connectionFromOutput != isOutput && 
                _connectionSourceNode != null && 
                _connectionSourcePort != null)
            {
                var sourceNode = _connectionFromOutput ? _connectionSourceNode : targetNode;
                var sourcePort = _connectionFromOutput ? _connectionSourcePort : targetPort;
                var destNode = _connectionFromOutput ? targetNode : _connectionSourceNode;
                var destPort = _connectionFromOutput ? targetPort : _connectionSourcePort;

                ConnectionCompleted?.Invoke(this, new ConnectionCompletedEventArgs(
                    sourceNode, sourcePort, destNode, destPort));
            }
        }

        CancelConnection();
        e.Pointer.Capture(null);
    }

    private void UpdateTempConnectionLine()
    {
        if (_tempConnectionLine == null || _connectionSourceNode == null || _connectionSourcePort == null)
            return;

        var startPoint = _graphRenderer.GetPortPosition(
            _connectionSourceNode, 
            _connectionSourcePort, 
            _connectionFromOutput);

        var pathGeometry = BezierHelper.CreateBezierPath(startPoint, _connectionEndPoint, !_connectionFromOutput);
        _tempConnectionLine.Data = pathGeometry;
    }
}

/// <summary>
/// Event args for connection completed event.
/// </summary>
public class ConnectionCompletedEventArgs : EventArgs
{
    public Node SourceNode { get; }
    public Port SourcePort { get; }
    public Node TargetNode { get; }
    public Port TargetPort { get; }

    public ConnectionCompletedEventArgs(Node sourceNode, Port sourcePort, Node targetNode, Port targetPort)
    {
        SourceNode = sourceNode;
        SourcePort = sourcePort;
        TargetNode = targetNode;
        TargetPort = targetPort;
    }
}
