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
/// pan, zoom, node dragging, box selection, and connection creation.
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
    private bool _isDraggingNodes;
    private AvaloniaPoint _dragStartPoint;
    private Dictionary<string, Core.Point> _dragStartPositions = new();

    // Box selection state
    private bool _isBoxSelecting;
    private AvaloniaPoint _boxSelectStart;
    private AvaloniaPoint _boxSelectEnd;
    private Rectangle? _selectionBox;
    private Canvas? _selectionCanvas;

    // Connection creation state
    private bool _isCreatingConnection;
    private Node? _connectionSourceNode;
    private Port? _connectionSourcePort;
    private bool _connectionFromOutput;
    private AvaloniaPoint _connectionEndPoint;
    private AvaloniaPath? _tempConnectionLine;
    private Canvas? _canvas;
    private Panel? _rootPanel;
    private Ellipse? _capturedPortVisual;  // The port visual that captured the pointer
    private Cursor? _previousPortCursor;   // The port's original cursor

    /// <summary>
    /// Event raised when a connection is completed.
    /// </summary>
    public event EventHandler<ConnectionCompletedEventArgs>? ConnectionCompleted;

    /// <summary>
    /// Event raised when an edge is clicked.
    /// </summary>
    public event EventHandler<EdgeClickedEventArgs>? EdgeClicked;

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

    /// <summary>
    /// Event raised when box selection changes.
    /// </summary>
    public event EventHandler<BoxSelectionEventArgs>? BoxSelectionChanged;

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
            CancelBoxSelection();
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
    public void HandleRootPanelPointerPressed(PointerPressedEventArgs e, Panel? rootPanel, Canvas? mainCanvas, Graph? graph)
    {
        var point = e.GetCurrentPoint(rootPanel);
        var position = e.GetPosition(rootPanel);

        // Middle mouse button always pans
        if (point.Properties.IsMiddleButtonPressed)
        {
            StartPanning(position);
            e.Pointer.Capture((IInputElement?)rootPanel);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            // Use screen position for hit testing since visuals are in screen coords
            var hitElement = mainCanvas?.InputHitTest(position);
            var isEmptyCanvas = hitElement == null || hitElement == mainCanvas;

            if (isEmptyCanvas)
            {
                bool shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

                // Determine action based on settings and modifier keys
                bool shouldPan = _settings.PanOnDrag ? !shiftHeld : shiftHeld;

                if (shouldPan)
                {
                    // Deselect all when clicking on empty canvas (unless Ctrl is held)
                    if (!ctrlHeld)
                    {
                        DeselectAllRequested?.Invoke(this, EventArgs.Empty);
                    }
                    StartPanning(position);
                    e.Pointer.Capture((IInputElement?)rootPanel);
                }
                else
                {
                    // Start box selection - deselect unless Ctrl is held
                    if (!ctrlHeld)
                    {
                        DeselectAllRequested?.Invoke(this, EventArgs.Empty);
                    }
                    var canvasPoint = _viewport.ScreenToCanvas(position);
                    StartBoxSelection(canvasPoint, mainCanvas);
                    e.Pointer.Capture((IInputElement?)rootPanel);
                }
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Handles pointer moved on the root panel.
    /// </summary>
    public void HandleRootPanelPointerMoved(PointerEventArgs e, Panel? rootPanel, Graph? graph)
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
        else if (_isBoxSelecting && graph != null)
        {
            _boxSelectEnd = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
            UpdateSelectionBox();
            UpdateBoxSelection(graph, e.KeyModifiers.HasFlag(KeyModifiers.Control));
            e.Handled = true;
        }
        else if (_isCreatingConnection)
        {
            // Store the screen position directly (not converted to canvas)
            _connectionEndPoint = e.GetPosition(rootPanel);
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

        if (_isBoxSelecting)
        {
            CancelBoxSelection();
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
    public void HandleNodePointerPressed(Control control, Node node, PointerPressedEventArgs e, Panel? rootPanel, Graph? graph)
    {
        var point = e.GetCurrentPoint(control);

        if (point.Properties.IsLeftButtonPressed && graph != null)
        {
            bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            // Handle selection
            if (!ctrlHeld && !node.IsSelected)
            {
                // Clicking unselected node without Ctrl: select only this node
                foreach (var n in graph.Nodes.Where(n => n.Id != node.Id))
                {
                    n.IsSelected = false;
                }
                node.IsSelected = true;
            }
            else if (ctrlHeld)
            {
                // Ctrl+click: toggle selection
                node.IsSelected = !node.IsSelected;
            }
            // else: clicking already selected node - keep current selection for multi-drag

            // Start dragging all selected nodes
            StartDraggingNodes(graph, e.GetPosition(rootPanel));

            e.Pointer.Capture(control);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles node pointer moved.
    /// </summary>
    public void HandleNodePointerMoved(PointerEventArgs e, Panel? rootPanel, Graph? graph)
    {
        if (_isDraggingNodes && graph != null)
        {
            var currentPoint = _viewport.ScreenToCanvas(e.GetPosition(rootPanel));
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            // Move all selected nodes
            foreach (var node in graph.Nodes.Where(n => n.IsSelected))
            {
                if (_dragStartPositions.TryGetValue(node.Id, out var startPos))
                {
                    var newX = startPos.X + deltaX;
                    var newY = startPos.Y + deltaY;

                    // Apply snap to grid on the final position
                    if (_settings.SnapToGrid)
                    {
                        var snapSize = _settings.EffectiveSnapGridSize;
                        newX = Math.Round(newX / snapSize) * snapSize;
                        newY = Math.Round(newY / snapSize) * snapSize;
                    }

                    node.Position = new Core.Point(newX, newY);
                }
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles node pointer released.
    /// </summary>
    public void HandleNodePointerReleased(PointerReleasedEventArgs e, Graph? graph)
    {
        if (_isDraggingNodes && graph != null)
        {
            // Mark all dragging nodes as not dragging
            foreach (var node in graph.Nodes.Where(n => n.IsSelected))
            {
                node.IsDragging = false;
            }

            _isDraggingNodes = false;
            _dragStartPositions.Clear();

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
            _rootPanel = rootPanel;
            _isCreatingConnection = true;
            _connectionSourceNode = node;
            _connectionSourcePort = port;
            _connectionFromOutput = isOutput;
            // Store screen coordinates (not canvas coordinates)
            _connectionEndPoint = e.GetPosition(rootPanel);

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

            // Change the port visual's cursor to hand during drag
            _capturedPortVisual = portVisual;
            _previousPortCursor = portVisual.Cursor;
            portVisual.Cursor = new Cursor(StandardCursorType.Hand);

            e.Pointer.Capture(portVisual);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles edge pointer pressed (click on edge).
    /// </summary>
    public void HandleEdgePointerPressed(AvaloniaPath edgePath, Edge edge, PointerPressedEventArgs e, Graph? graph)
    {
        var point = e.GetCurrentPoint(edgePath);

        if (point.Properties.IsLeftButtonPressed && graph != null)
        {
            bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            // If Ctrl is not held, deselect all other edges and nodes
            if (!ctrlHeld)
            {
                foreach (var n in graph.Nodes)
                {
                    n.IsSelected = false;
                }
                foreach (var ed in graph.Edges.Where(ed => ed.Id != edge.Id))
                {
                    ed.IsSelected = false;
                }
            }

            // Toggle or select the edge
            edge.IsSelected = ctrlHeld ? !edge.IsSelected : true;
            
            EdgeClicked?.Invoke(this, new EdgeClickedEventArgs(edge, ctrlHeld));
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

    #region Private Methods - Panning

    private void StartPanning(AvaloniaPoint position)
    {
        _isPanning = true;
        _panStartPoint = position;
        _panStartOffsetX = _viewport.OffsetX;
        _panStartOffsetY = _viewport.OffsetY;
    }

    #endregion

    #region Private Methods - Node Dragging

    private void StartDraggingNodes(Graph graph, AvaloniaPoint screenPosition)
    {
        _isDraggingNodes = true;
        _dragStartPoint = _viewport.ScreenToCanvas(screenPosition);
        _dragStartPositions.Clear();

        // Store start positions of all selected nodes
        foreach (var node in graph.Nodes.Where(n => n.IsSelected))
        {
            node.IsDragging = true;
            _dragStartPositions[node.Id] = node.Position;
        }
    }

    #endregion

    #region Private Methods - Box Selection

    private void StartBoxSelection(AvaloniaPoint canvasPoint, Canvas? canvas)
    {
        _isBoxSelecting = true;
        _boxSelectStart = canvasPoint;
        _boxSelectEnd = canvasPoint;
        _selectionCanvas = canvas;

        // Create selection rectangle visual
        _selectionBox = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.Parse("#0078D4")),
            StrokeThickness = 1,
            Fill = new SolidColorBrush(Color.FromArgb(40, 0, 120, 212)),
            IsHitTestVisible = false
        };
        canvas?.Children.Add(_selectionBox);
        UpdateSelectionBox();
    }

    private void UpdateSelectionBox()
    {
        if (_selectionBox == null) return;

        var left = Math.Min(_boxSelectStart.X, _boxSelectEnd.X);
        var top = Math.Min(_boxSelectStart.Y, _boxSelectEnd.Y);
        var width = Math.Abs(_boxSelectEnd.X - _boxSelectStart.X);
        var height = Math.Abs(_boxSelectEnd.Y - _boxSelectStart.Y);

        Canvas.SetLeft(_selectionBox, left);
        Canvas.SetTop(_selectionBox, top);
        _selectionBox.Width = width;
        _selectionBox.Height = height;
    }

    private void UpdateBoxSelection(Graph graph, bool addToSelection)
    {
        var selectionRect = new Rect(
            Math.Min(_boxSelectStart.X, _boxSelectEnd.X),
            Math.Min(_boxSelectStart.Y, _boxSelectEnd.Y),
            Math.Abs(_boxSelectEnd.X - _boxSelectStart.X),
            Math.Abs(_boxSelectEnd.Y - _boxSelectStart.Y)
        );

        foreach (var node in graph.Nodes)
        {
            var nodeRect = new Rect(
                node.Position.X,
                node.Position.Y,
                _settings.NodeWidth,
                _settings.NodeHeight
            );

            bool shouldSelect = _settings.SelectionMode == SelectionMode.Full
                ? selectionRect.Contains(nodeRect)
                : selectionRect.Intersects(nodeRect);

            if (addToSelection)
            {
                // In add mode, only add to selection, don't remove
                if (shouldSelect)
                {
                    node.IsSelected = true;
                }
            }
            else
            {
                node.IsSelected = shouldSelect;
            }
        }

        BoxSelectionChanged?.Invoke(this, new BoxSelectionEventArgs(selectionRect));
    }

    private void CancelBoxSelection()
    {
        if (_selectionBox != null && _selectionCanvas != null)
        {
            _selectionCanvas.Children.Remove(_selectionBox);
            _selectionBox = null;
        }
        _isBoxSelecting = false;
    }

    #endregion

    #region Private Methods - Connection

    private void CancelConnection()
    {
        if (_tempConnectionLine != null && _canvas != null)
        {
            _canvas.Children.Remove(_tempConnectionLine);
            _tempConnectionLine = null;
        }

        // Restore the port visual's original cursor
        if (_capturedPortVisual != null && _previousPortCursor != null)
        {
            _capturedPortVisual.Cursor = _previousPortCursor;
            _previousPortCursor = null;
            _capturedPortVisual = null;
        }

        _isCreatingConnection = false;
        _connectionSourceNode = null;
        _connectionSourcePort = null;
        _rootPanel = null;
    }

    private void CompleteConnection(PointerReleasedEventArgs e, Panel? rootPanel, Canvas? mainCanvas, Graph? graph)
    {
        // Use screen position for hit testing since visuals are in screen coords
        var screenPoint = e.GetPosition(rootPanel);
        var hitElement = mainCanvas?.InputHitTest(screenPoint);

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

    #endregion
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

/// <summary>
/// Event args for edge clicked event.
/// </summary>
public class EdgeClickedEventArgs : EventArgs
{
    public Edge Edge { get; }
    public bool WasCtrlHeld { get; }

    public EdgeClickedEventArgs(Edge edge, bool wasCtrlHeld)
    {
        Edge = edge;
        WasCtrlHeld = wasCtrlHeld;
    }
}

/// <summary>
/// Event args for box selection changes.
/// </summary>
public class BoxSelectionEventArgs : EventArgs
{
    public Rect SelectionRect { get; }

    public BoxSelectionEventArgs(Rect selectionRect)
    {
        SelectionRect = selectionRect;
    }
}
