using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Rendering.ShapeRenderers;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using FlowGraph.Core.Coordinates;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Shared context passed to all input states.
/// Contains references to canvas components and raises events.
/// </summary>
public class InputStateContext
{
    private FlowCanvasSettings _settings;
    private readonly ViewportState _viewport;
    private readonly Rendering.CanvasElementManager _graphRenderer;

    // Type-safe coordinate and rendering interfaces
    private InputCoordinatesAdapter? _coordinates;
    private RenderTargetAdapter? _renderTarget;

    public InputStateContext(
        FlowCanvasSettings settings,
        ViewportState viewport,
        Rendering.CanvasElementManager graphRenderer)
    {
        _settings = settings;
        _viewport = viewport;
        _graphRenderer = graphRenderer;
    }

    /// <summary>
    /// Updates the settings used by this context.
    /// </summary>
    /// <param name="settings">The new settings to use.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

    #region Component Access

    public FlowCanvasSettings Settings => _settings;
    public ViewportState Viewport => _viewport;
    public Rendering.CanvasElementManager GraphRenderer => _graphRenderer;

    // UI elements - set by the canvas
    private Panel? _rootPanel;
    private Canvas? _mainCanvas;
    private DirectCanvasRenderer? _directRenderer;

    public Panel? RootPanel
    {
        get => _rootPanel;
        set { _rootPanel = value; InvalidateAdapters(); }
    }

    public Canvas? MainCanvas
    {
        get => _mainCanvas;
        set { _mainCanvas = value; InvalidateAdapters(); }
    }

    /// <summary>
    /// Callback to apply viewport transforms. Set by FlowCanvas.
    /// </summary>
    public Action? ApplyViewportTransformCallback { get; set; }

    public DirectCanvasRenderer? DirectRenderer
    {
        get => _directRenderer;
        set { _directRenderer = value; InvalidateAdapters(); }
    }

    public Graph? Graph { get; set; }
    public ThemeResources? Theme { get; set; }

    /// <summary>
    /// Type-safe coordinate helper that abstracts rendering mode differences.
    /// Use this instead of ViewportToCanvas/CanvasToViewport for new code.
    /// </summary>
    public IInputCoordinates Coordinates
    {
        get
        {
            _coordinates ??= new InputCoordinatesAdapter(_viewport, _rootPanel, _mainCanvas, _directRenderer);
            return _coordinates;
        }
    }

    /// <summary>
    /// Mode-agnostic render target for creating temporary visuals.
    /// Handles container selection and coordinate conversion internally.
    /// </summary>
    public IRenderTarget RenderTarget
    {
        get
        {
            _renderTarget ??= new RenderTargetAdapter(_viewport, _rootPanel, _mainCanvas, _directRenderer);
            return _renderTarget;
        }
    }

    /// <summary>
    /// Invalidates cached adapters when UI elements change.
    /// </summary>
    private void InvalidateAdapters()
    {
        _coordinates = null;
        _renderTarget = null;
    }

    /// <summary>
    /// Optional connection validator for validating new connections.
    /// Set by FlowCanvas.
    /// </summary>
    public IConnectionValidator? ConnectionValidator { get; set; }

    /// <summary>
    /// Optional snap provider for providing snap offsets during drag operations.
    /// External systems (like helper lines, guides) can register as a snap provider
    /// to influence node positions during drag without directly setting positions.
    /// Set by FlowCanvas.
    /// </summary>
    public ISnapProvider? SnapProvider { get; set; }

    /// <summary>
    /// Optional collision provider for preventing node overlap during drag.
    /// Applied after SnapProvider. Returns offset to prevent collision.
    /// Set by FlowCanvas.
    /// </summary>
    public ICollisionProvider? CollisionProvider { get; set; }

    /// <summary>
    /// Shape visual manager for updating shape selection state.
    /// Set by FlowCanvas.
    /// </summary>
    public ShapeVisualManager? ShapeVisualManager { get; set; }

    /// <summary>
    /// Reference to the InputDispatcher for processor-based input handling.
    /// Set by FlowCanvas during initialization.
    /// </summary>
    /// <remarks>
    /// This enables IdleState to delegate to the InputDispatcher instead of
    /// doing manual pattern matching on source control tags.
    /// </remarks>
    public InputDispatcher? Dispatcher { get; set; }

    #endregion

    #region Events

    public event EventHandler<ConnectionCompletedEventArgs>? ConnectionCompleted;
    public event EventHandler<EdgeClickedEventArgs>? EdgeClicked;
    public event EventHandler? DeselectAllRequested;
    public event EventHandler? SelectAllRequested;
    public event EventHandler? DeleteSelectedRequested;
    public event EventHandler? UndoRequested;
    public event EventHandler? RedoRequested;
    public event EventHandler? CopyRequested;
    public event EventHandler? CutRequested;
    public event EventHandler? PasteRequested;
    public event EventHandler? DuplicateRequested;
    public event EventHandler? GroupRequested;
    public event EventHandler? UngroupRequested;
    public event EventHandler<GroupToggleCollapseEventArgs>? GroupCollapseToggleRequested;
    public event EventHandler? GridRenderRequested;
    public event EventHandler<BoxSelectionEventArgs>? BoxSelectionChanged;
    public event EventHandler<NodesDraggingEventArgs>? NodesDragging;
    public event EventHandler<NodesDraggedEventArgs>? NodesDragged;
    public event EventHandler<NodeResizedEventArgs>? NodeResized;
    public event EventHandler<NodeResizingEventArgs>? NodeResizing;
    public event EventHandler<StateChangedEventArgs>? StateChanged;
    public event EventHandler<NodeDragStartEventArgs>? NodeDragStart;
    public event EventHandler<NodeDragStopEventArgs>? NodeDragStop;
    public event EventHandler<ConnectStartEventArgs>? ConnectStart;
    public event EventHandler<ConnectEndEventArgs>? ConnectEnd;
    public event EventHandler<EdgeReconnectedEventArgs>? EdgeReconnected;
    public event EventHandler<EdgeDisconnectedEventArgs>? EdgeDisconnected;
    public event EventHandler<NodeLabelEditRequestedEventArgs>? NodeLabelEditRequested;
    public event EventHandler<EdgeLabelEditRequestedEventArgs>? EdgeLabelEditRequested;
    public event EventHandler<ShapeTextEditRequestedEventArgs>? ShapeTextEditRequested;
    public event EventHandler<ShapeResizedEventArgs>? ShapeResized;
    public event EventHandler<ShapeResizingEventArgs>? ShapeResizing;
    public event EventHandler? SelectionChangeRequested;

    #endregion

    #region Event Raisers

    public void RaiseConnectionCompleted(Node sourceNode, Port sourcePort, Node targetNode, Port targetPort)
        => ConnectionCompleted?.Invoke(this, new ConnectionCompletedEventArgs(sourceNode, sourcePort, targetNode, targetPort));

    public void RaiseEdgeClicked(Edge edge, bool ctrlHeld)
        => EdgeClicked?.Invoke(this, new EdgeClickedEventArgs(edge, ctrlHeld));

    public void RaiseDeselectAll() => DeselectAllRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseSelectAll() => SelectAllRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseDeleteSelected() => DeleteSelectedRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseUndo() => UndoRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseRedo() => RedoRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseCopy() => CopyRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseCut() => CutRequested?.Invoke(this, EventArgs.Empty);
    public void RaisePaste() => PasteRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseDuplicate() => DuplicateRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseGroup() => GroupRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseUngroup() => UngroupRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseGroupCollapseToggle(string groupId)
        => GroupCollapseToggleRequested?.Invoke(this, new GroupToggleCollapseEventArgs(groupId));
    public void RaiseGridRender() => GridRenderRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseBoxSelectionChanged(AvaloniaRect bounds)
        => BoxSelectionChanged?.Invoke(this, new BoxSelectionEventArgs(bounds));
    public void RaiseNodesDragging(IReadOnlyList<string> nodeIds)
        => NodesDragging?.Invoke(this, new NodesDraggingEventArgs(nodeIds));
    public void RaiseNodesDragged(Dictionary<string, Core.Point> oldPositions, Dictionary<string, Core.Point> newPositions)
        => NodesDragged?.Invoke(this, new NodesDraggedEventArgs(oldPositions, newPositions));
    public void RaiseNodeResized(Node node, double oldWidth, double oldHeight, double newWidth, double newHeight, Core.Point oldPos, Core.Point newPos)
        => NodeResized?.Invoke(this, new NodeResizedEventArgs(node, oldWidth, oldHeight, newWidth, newHeight, oldPos, newPos));
    public void RaiseNodeResizing(Node node, double newWidth, double newHeight, Core.Point newPos)
        => NodeResizing?.Invoke(this, new NodeResizingEventArgs(node, newWidth, newHeight, newPos));
    public void RaiseStateChanged(string oldState, string newState)
        => StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
    public void RaiseNodeDragStart(IReadOnlyList<Node> nodes, Core.Point startPosition)
        => NodeDragStart?.Invoke(this, new NodeDragStartEventArgs(nodes, startPosition));
    public void RaiseNodeDragStop(IReadOnlyList<Node> nodes, bool cancelled)
        => NodeDragStop?.Invoke(this, new NodeDragStopEventArgs(nodes, cancelled));
    public void RaiseConnectStart(Node sourceNode, Port sourcePort, bool isOutput)
        => ConnectStart?.Invoke(this, new ConnectStartEventArgs(sourceNode, sourcePort, isOutput));
    public void RaiseConnectEnd(Node? sourceNode, Port? sourcePort, Node? targetNode, Port? targetPort, bool completed)
        => ConnectEnd?.Invoke(this, new ConnectEndEventArgs(sourceNode, sourcePort, targetNode, targetPort, completed));
    public void RaiseEdgeReconnected(Edge oldEdge, Edge newEdge)
        => EdgeReconnected?.Invoke(this, new EdgeReconnectedEventArgs(oldEdge, newEdge));
    public void RaiseEdgeDisconnected(Edge edge)
        => EdgeDisconnected?.Invoke(this, new EdgeDisconnectedEventArgs(edge));

    public void RaiseSelectionChanged()
        => SelectionChangeRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Raises the NodeLabelEditRequested event and returns whether it was handled.
    /// </summary>
    public bool RaiseNodeLabelEditRequested(Node node, AvaloniaPoint screenPosition)
    {
        var args = new NodeLabelEditRequestedEventArgs(node, node.Label, screenPosition);
        NodeLabelEditRequested?.Invoke(this, args);
        return args.Handled;
    }

    /// <summary>
    /// Raises the EdgeLabelEditRequested event and returns whether it was handled.
    /// </summary>
    public bool RaiseEdgeLabelEditRequested(Edge edge, AvaloniaPoint screenPosition)
    {
        var args = new EdgeLabelEditRequestedEventArgs(edge, edge.Label, screenPosition);
        EdgeLabelEditRequested?.Invoke(this, args);
        return args.Handled;
    }

    /// <summary>
    /// Raises the ShapeTextEditRequested event and returns whether it was handled.
    /// </summary>
    public bool RaiseShapeTextEditRequested(Core.Elements.Shapes.ShapeElement shape, AvaloniaPoint screenPosition)
    {
        // Get current text from the shape (if it's a CommentElement)
        string? currentText = null;
        if (shape is Core.Elements.Shapes.CommentElement comment)
            currentText = comment.Text;

        var args = new ShapeTextEditRequestedEventArgs(shape, currentText, screenPosition);
        ShapeTextEditRequested?.Invoke(this, args);
        return args.Handled;
    }

    /// <summary>
    /// Raises the ShapeResizing event during shape resize operation.
    /// </summary>
    public void RaiseShapeResizing(Core.Elements.Shapes.ShapeElement shape, double newWidth, double newHeight, Core.Point newPos)
        => ShapeResizing?.Invoke(this, new ShapeResizingEventArgs(shape, newWidth, newHeight, newPos));

    /// <summary>
    /// Raises the ShapeResized event after shape resize is complete.
    /// </summary>
    public void RaiseShapeResized(Core.Elements.Shapes.ShapeElement shape, double oldWidth, double oldHeight, double newWidth, double newHeight, Core.Point oldPos, Core.Point newPos)
        => ShapeResized?.Invoke(this, new ShapeResizedEventArgs(shape, oldWidth, oldHeight, newWidth, newHeight, oldPos, newPos));

    #endregion

    #region Coordinate Helpers

    public AvaloniaPoint ViewportToCanvas(AvaloniaPoint viewportPoint) => _viewport.ViewportToCanvas(viewportPoint);
    public AvaloniaPoint CanvasToViewport(AvaloniaPoint canvasPoint) => _viewport.CanvasToViewport(canvasPoint);

    #endregion

    #region Viewport Transform

    /// <summary>
    /// Applies the current viewport state to the MainCanvas transform.
    /// This enables O(1) pan/zoom updates instead of O(n) re-rendering.
    /// For DirectRendering mode, triggers a redraw since it bypasses the visual tree.
    /// </summary>
    public void ApplyViewportTransform()
    {
        // Use the callback to apply transforms (set by FlowCanvas)
        ApplyViewportTransformCallback?.Invoke();

        // DirectRendering mode bypasses visual tree, so we need to trigger a redraw
        if (DirectRenderer != null)
        {
            DirectRenderer.InvalidateVisual();
        }
    }

    #endregion
}

/// <summary>
/// Event args for state changes.
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    public string OldState { get; }
    public string NewState { get; }

    public StateChangedEventArgs(string oldState, string newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}
