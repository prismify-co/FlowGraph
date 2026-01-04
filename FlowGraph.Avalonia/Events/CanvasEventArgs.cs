using Avalonia;
using Avalonia.Controls;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;
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
