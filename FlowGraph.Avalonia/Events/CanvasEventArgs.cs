using Avalonia;
using Avalonia.Controls;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using FlowGraph.Core.Elements.Shapes;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Avalonia;

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
/// Event args for connection rejected event.
/// </summary>
public class ConnectionRejectedEventArgs : EventArgs
{
    /// <summary>
    /// The connection context that was rejected.
    /// </summary>
    public ConnectionContext Context { get; }

    /// <summary>
    /// The reason the connection was rejected.
    /// </summary>
    public string? Reason { get; }

    public ConnectionRejectedEventArgs(ConnectionContext context, string? reason)
    {
        Context = context;
        Reason = reason;
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

/// <summary>
/// Event args for nodes being dragged (in progress).
/// </summary>
public class NodesDraggingEventArgs : EventArgs
{
    /// <summary>
    /// IDs of nodes currently being dragged.
    /// </summary>
    public IReadOnlyList<string> NodeIds { get; }

    public NodesDraggingEventArgs(IReadOnlyList<string> nodeIds)
    {
        NodeIds = nodeIds;
    }
}

/// <summary>
/// Event args for node drag completion.
/// </summary>
public class NodesDraggedEventArgs : EventArgs
{
    public Dictionary<string, CorePoint> OldPositions { get; }
    public Dictionary<string, CorePoint> NewPositions { get; }

    public NodesDraggedEventArgs(
        Dictionary<string, CorePoint> oldPositions,
        Dictionary<string, CorePoint> newPositions)
    {
        OldPositions = oldPositions;
        NewPositions = newPositions;
    }
}

/// <summary>
/// Event args for node resize in progress.
/// </summary>
public class NodeResizingEventArgs : EventArgs
{
    public Node Node { get; }
    public double NewWidth { get; }
    public double NewHeight { get; }
    public CorePoint NewPosition { get; }

    public NodeResizingEventArgs(Node node, double newWidth, double newHeight, CorePoint newPosition)
    {
        Node = node;
        NewWidth = newWidth;
        NewHeight = newHeight;
        NewPosition = newPosition;
    }
}

/// <summary>
/// Event args for node resize completion.
/// </summary>
public class NodeResizedEventArgs : EventArgs
{
    public Node Node { get; }
    public double OldWidth { get; }
    public double OldHeight { get; }
    public double NewWidth { get; }
    public double NewHeight { get; }
    public CorePoint OldPosition { get; }
    public CorePoint NewPosition { get; }

    public NodeResizedEventArgs(
        Node node,
        double oldWidth,
        double oldHeight,
        double newWidth,
        double newHeight,
        CorePoint oldPosition,
        CorePoint newPosition)
    {
        Node = node;
        OldWidth = oldWidth;
        OldHeight = oldHeight;
        NewWidth = newWidth;
        NewHeight = newHeight;
        OldPosition = oldPosition;
        NewPosition = newPosition;
    }
}

/// <summary>
/// Event args for group collapse toggle request.
/// </summary>
public class GroupToggleCollapseEventArgs : EventArgs
{
    /// <summary>
    /// The ID of the group to toggle.
    /// </summary>
    public string GroupId { get; }

    public GroupToggleCollapseEventArgs(string groupId)
    {
        GroupId = groupId;
    }
}

/// <summary>
/// Event args for context menu request.
/// </summary>
public class ContextMenuRequestedEventArgs : EventArgs
{
    /// <summary>
    /// The target control that was right-clicked.
    /// </summary>
    public Control? Target { get; }

    /// <summary>
    /// The screen position where the context menu was requested.
    /// </summary>
    public AvaloniaPoint ScreenPosition { get; }

    /// <summary>
    /// The canvas position where the context menu was requested.
    /// </summary>
    public CorePoint CanvasPosition { get; }

    /// <summary>
    /// The node that was right-clicked, if any.
    /// </summary>
    public Node? Node { get; }

    /// <summary>
    /// The edge that was right-clicked, if any.
    /// </summary>
    public Edge? Edge { get; }

    public ContextMenuRequestedEventArgs(
        Control? target,
        AvaloniaPoint screenPosition,
        CorePoint canvasPosition,
        Node? node = null,
        Edge? edge = null)
    {
        Target = target;
        ScreenPosition = screenPosition;
        CanvasPosition = canvasPosition;
        Node = node;
        Edge = edge;
    }
}

/// <summary>
/// Event args for selection changed event.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// The currently selected nodes.
    /// </summary>
    public IReadOnlyList<Node> SelectedNodes { get; }

    /// <summary>
    /// The currently selected edges.
    /// </summary>
    public IReadOnlyList<Edge> SelectedEdges { get; }

    /// <summary>
    /// The currently selected shapes.
    /// </summary>
    public IReadOnlyList<ShapeElement> SelectedShapes { get; }

    /// <summary>
    /// Nodes that were added to the selection.
    /// </summary>
    public IReadOnlyList<Node> AddedNodes { get; }

    /// <summary>
    /// Nodes that were removed from the selection.
    /// </summary>
    public IReadOnlyList<Node> RemovedNodes { get; }

    /// <summary>
    /// Edges that were added to the selection.
    /// </summary>
    public IReadOnlyList<Edge> AddedEdges { get; }

    /// <summary>
    /// Edges that were removed from the selection.
    /// </summary>
    public IReadOnlyList<Edge> RemovedEdges { get; }

    /// <summary>
    /// Shapes that were added to the selection.
    /// </summary>
    public IReadOnlyList<ShapeElement> AddedShapes { get; }

    /// <summary>
    /// Shapes that were removed from the selection.
    /// </summary>
    public IReadOnlyList<ShapeElement> RemovedShapes { get; }

    public SelectionChangedEventArgs(
        IReadOnlyList<Node> selectedNodes,
        IReadOnlyList<Edge> selectedEdges,
        IReadOnlyList<Node>? addedNodes = null,
        IReadOnlyList<Node>? removedNodes = null,
        IReadOnlyList<Edge>? addedEdges = null,
        IReadOnlyList<Edge>? removedEdges = null,
        IReadOnlyList<ShapeElement>? selectedShapes = null,
        IReadOnlyList<ShapeElement>? addedShapes = null,
        IReadOnlyList<ShapeElement>? removedShapes = null)
    {
        SelectedNodes = selectedNodes;
        SelectedEdges = selectedEdges;
        SelectedShapes = selectedShapes ?? [];
        AddedNodes = addedNodes ?? [];
        RemovedNodes = removedNodes ?? [];
        AddedEdges = addedEdges ?? [];
        RemovedEdges = removedEdges ?? [];
        AddedShapes = addedShapes ?? [];
        RemovedShapes = removedShapes ?? [];
    }
}

/// <summary>
/// Event args for viewport changed event.
/// </summary>
public class ViewportChangedEventArgs : EventArgs
{
    /// <summary>
    /// The current zoom level.
    /// </summary>
    public double Zoom { get; }

    /// <summary>
    /// The current X offset (pan).
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// The current Y offset (pan).
    /// </summary>
    public double OffsetY { get; }

    /// <summary>
    /// The visible area in canvas coordinates.
    /// </summary>
    public AvaloniaRect VisibleRect { get; }

    public ViewportChangedEventArgs(double zoom, double offsetX, double offsetY, AvaloniaRect visibleRect)
    {
        Zoom = zoom;
        OffsetX = offsetX;
        OffsetY = offsetY;
        VisibleRect = visibleRect;
    }
}

/// <summary>
/// Event args for node drag start event.
/// </summary>
public class NodeDragStartEventArgs : EventArgs
{
    /// <summary>
    /// The nodes being dragged.
    /// </summary>
    public IReadOnlyList<Node> Nodes { get; }

    /// <summary>
    /// Starting position in canvas coordinates.
    /// </summary>
    public CorePoint StartPosition { get; }

    public NodeDragStartEventArgs(IReadOnlyList<Node> nodes, CorePoint startPosition)
    {
        Nodes = nodes;
        StartPosition = startPosition;
    }
}

/// <summary>
/// Event args for node drag stop event.
/// </summary>
public class NodeDragStopEventArgs : EventArgs
{
    /// <summary>
    /// The nodes that were dragged.
    /// </summary>
    public IReadOnlyList<Node> Nodes { get; }

    /// <summary>
    /// Whether the drag was cancelled (e.g., by pressing Escape).
    /// </summary>
    public bool WasCancelled { get; }

    public NodeDragStopEventArgs(IReadOnlyList<Node> nodes, bool wasCancelled)
    {
        Nodes = nodes;
        WasCancelled = wasCancelled;
    }
}

/// <summary>
/// Event args for connection start event.
/// </summary>
public class ConnectStartEventArgs : EventArgs
{
    /// <summary>
    /// The source node.
    /// </summary>
    public Node SourceNode { get; }

    /// <summary>
    /// The source port.
    /// </summary>
    public Port SourcePort { get; }

    /// <summary>
    /// Whether the connection started from an output port.
    /// </summary>
    public bool IsFromOutput { get; }

    public ConnectStartEventArgs(Node sourceNode, Port sourcePort, bool isFromOutput)
    {
        SourceNode = sourceNode;
        SourcePort = sourcePort;
        IsFromOutput = isFromOutput;
    }
}

/// <summary>
/// Event args for connection end event.
/// </summary>
public class ConnectEndEventArgs : EventArgs
{
    /// <summary>
    /// The source node where the connection started.
    /// </summary>
    public Node? SourceNode { get; }

    /// <summary>
    /// The source port where the connection started.
    /// </summary>
    public Port? SourcePort { get; }

    /// <summary>
    /// The target node if connection completed, null if cancelled.
    /// </summary>
    public Node? TargetNode { get; }

    /// <summary>
    /// The target port if connection completed, null if cancelled.
    /// </summary>
    public Port? TargetPort { get; }

    /// <summary>
    /// Whether the connection was successfully completed.
    /// </summary>
    public bool WasCompleted { get; }

    public ConnectEndEventArgs(
        Node? sourceNode, 
        Port? sourcePort, 
        Node? targetNode, 
        Port? targetPort, 
        bool wasCompleted)
    {
        SourceNode = sourceNode;
        SourcePort = sourcePort;
        TargetNode = targetNode;
        TargetPort = targetPort;
        WasCompleted = wasCompleted;
    }
}

/// <summary>
/// Event args for edge reconnection.
/// </summary>
public class EdgeReconnectedEventArgs : EventArgs
{
    /// <summary>
    /// The original edge that was reconnected.
    /// </summary>
    public Edge OldEdge { get; }

    /// <summary>
    /// The new edge with updated connection.
    /// </summary>
    public Edge NewEdge { get; }

    public EdgeReconnectedEventArgs(Edge oldEdge, Edge newEdge)
    {
        OldEdge = oldEdge;
        NewEdge = newEdge;
    }
}

/// <summary>
/// Event args for edge disconnection (dropped in empty space).
/// </summary>
public class EdgeDisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// The edge that was disconnected.
    /// </summary>
    public Edge Edge { get; }

    public EdgeDisconnectedEventArgs(Edge edge)
    {
        Edge = edge;
    }
}

/// <summary>
/// Event args for node label edit request.
/// Raised when a user double-clicks a node to edit its label.
/// </summary>
public class NodeLabelEditRequestedEventArgs : EventArgs
{
    /// <summary>
    /// The node to edit.
    /// </summary>
    public Node Node { get; }

    /// <summary>
    /// The current label value.
    /// </summary>
    public string? CurrentLabel { get; }

    /// <summary>
    /// The screen position of the node (for positioning an editor).
    /// </summary>
    public AvaloniaPoint ScreenPosition { get; }

    /// <summary>
    /// Set to true to indicate the event was handled and no default behavior should occur.
    /// </summary>
    public bool Handled { get; set; }

    public NodeLabelEditRequestedEventArgs(Node node, string? currentLabel, AvaloniaPoint screenPosition)
    {
        Node = node;
        CurrentLabel = currentLabel;
        ScreenPosition = screenPosition;
    }
}

/// <summary>
/// Event args for edge label edit request.
/// Raised when a user double-clicks an edge label to edit it.
/// </summary>
public class EdgeLabelEditRequestedEventArgs : EventArgs
{
    /// <summary>
    /// The edge to edit.
    /// </summary>
    public Edge Edge { get; }

    /// <summary>
    /// The current label value.
    /// </summary>
    public string? CurrentLabel { get; }

    /// <summary>
    /// The screen position of the label (for positioning an editor).
    /// </summary>
    public AvaloniaPoint ScreenPosition { get; }

    /// <summary>
    /// Set to true to indicate the event was handled and no default behavior should occur.
    /// </summary>
    public bool Handled { get; set; }

    public EdgeLabelEditRequestedEventArgs(Edge edge, string? currentLabel, AvaloniaPoint screenPosition)
    {
        Edge = edge;
        CurrentLabel = currentLabel;
        ScreenPosition = screenPosition;
    }
}

/// <summary>
/// Event args for shape text edit request.
/// Raised when a user double-clicks a shape (like a sticky note) to edit its text.
/// </summary>
public class ShapeTextEditRequestedEventArgs : EventArgs
{
    /// <summary>
    /// The shape to edit.
    /// </summary>
    public Core.Elements.Shapes.ShapeElement Shape { get; }

    /// <summary>
    /// The current text value.
    /// </summary>
    public string? CurrentText { get; }

    /// <summary>
    /// The screen position of the shape (for positioning an editor).
    /// </summary>
    public AvaloniaPoint ScreenPosition { get; }

    /// <summary>
    /// Set to true to indicate the event was handled and no default behavior should occur.
    /// </summary>
    public bool Handled { get; set; }

    public ShapeTextEditRequestedEventArgs(Core.Elements.Shapes.ShapeElement shape, string? currentText, AvaloniaPoint screenPosition)
    {
        Shape = shape;
        CurrentText = currentText;
        ScreenPosition = screenPosition;
    }
}
