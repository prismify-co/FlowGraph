using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Shared context passed to all input states.
/// Contains references to canvas components and raises events.
/// </summary>
public class InputStateContext
{
    private readonly FlowCanvasSettings _settings;
    private readonly ViewportState _viewport;
    private readonly Rendering.GraphRenderer _graphRenderer;
    
    public InputStateContext(
        FlowCanvasSettings settings,
        ViewportState viewport,
        Rendering.GraphRenderer graphRenderer)
    {
        _settings = settings;
        _viewport = viewport;
        _graphRenderer = graphRenderer;
    }

    #region Component Access

    public FlowCanvasSettings Settings => _settings;
    public ViewportState Viewport => _viewport;
    public Rendering.GraphRenderer GraphRenderer => _graphRenderer;

    // UI elements - set by the canvas
    public Panel? RootPanel { get; set; }
    public Canvas? MainCanvas { get; set; }
    public Graph? Graph { get; set; }
    public Rendering.ThemeResources? Theme { get; set; }
    
    /// <summary>
    /// Optional connection validator for validating new connections.
    /// Set by FlowCanvas.
    /// </summary>
    public IConnectionValidator? ConnectionValidator { get; set; }

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
    
    /// <summary>
    /// Raises the NodeLabelEditRequested event and returns whether it was handled.
    /// </summary>
    public bool RaiseNodeLabelEditRequested(Node node, AvaloniaPoint screenPosition)
    {
        var args = new NodeLabelEditRequestedEventArgs(node, node.Label, screenPosition);
        NodeLabelEditRequested?.Invoke(this, args);
        return args.Handled;
    }

    #endregion

    #region Coordinate Helpers

    public AvaloniaPoint ScreenToCanvas(AvaloniaPoint screenPoint) => _viewport.ScreenToCanvas(screenPoint);
    public AvaloniaPoint CanvasToScreen(AvaloniaPoint canvasPoint) => _viewport.CanvasToScreen(canvasPoint);

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
