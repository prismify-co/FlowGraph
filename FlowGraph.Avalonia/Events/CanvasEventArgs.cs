using Avalonia;
using FlowGraph.Core;

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
    public Dictionary<string, Core.Point> OldPositions { get; }
    public Dictionary<string, Core.Point> NewPositions { get; }

    public NodesDraggedEventArgs(
        Dictionary<string, Core.Point> oldPositions,
        Dictionary<string, Core.Point> newPositions)
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
    public Core.Point NewPosition { get; }

    public NodeResizingEventArgs(Node node, double newWidth, double newHeight, Core.Point newPosition)
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
    public Core.Point OldPosition { get; }
    public Core.Point NewPosition { get; }

    public NodeResizedEventArgs(
        Node node,
        double oldWidth,
        double oldHeight,
        double newWidth,
        double newHeight,
        Core.Point oldPosition,
        Core.Point newPosition)
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
