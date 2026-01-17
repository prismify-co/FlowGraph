namespace FlowGraph.Core.Events;

/// <summary>
/// Event arguments for node position changes.
/// </summary>
public class PositionChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous position.
    /// </summary>
    public Point OldPosition { get; }

    /// <summary>
    /// The new position.
    /// </summary>
    public Point NewPosition { get; }

    /// <summary>
    /// Creates position changed event args.
    /// </summary>
    public PositionChangedEventArgs(Point oldPosition, Point newPosition)
    {
        OldPosition = oldPosition;
        NewPosition = newPosition;
    }
}

/// <summary>
/// Event arguments for node bounds changes (position or size).
/// </summary>
public class BoundsChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous position.
    /// </summary>
    public Point OldPosition { get; }

    /// <summary>
    /// The new position.
    /// </summary>
    public Point NewPosition { get; }

    /// <summary>
    /// The previous width (null if default).
    /// </summary>
    public double? OldWidth { get; }

    /// <summary>
    /// The new width (null if default).
    /// </summary>
    public double? NewWidth { get; }

    /// <summary>
    /// The previous height (null if default).
    /// </summary>
    public double? OldHeight { get; }

    /// <summary>
    /// The new height (null if default).
    /// </summary>
    public double? NewHeight { get; }

    /// <summary>
    /// Whether only the position changed (not size).
    /// </summary>
    public bool PositionOnly => OldWidth == NewWidth && OldHeight == NewHeight;

    /// <summary>
    /// Creates bounds changed event args.
    /// </summary>
    public BoundsChangedEventArgs(
        Point oldPosition, Point newPosition,
        double? oldWidth, double? newWidth,
        double? oldHeight, double? newHeight)
    {
        OldPosition = oldPosition;
        NewPosition = newPosition;
        OldWidth = oldWidth;
        NewWidth = newWidth;
        OldHeight = oldHeight;
        NewHeight = newHeight;
    }
}

/// <summary>
/// Event arguments for graph-level node bounds changes.
/// Wraps the node-level <see cref="BoundsChangedEventArgs"/> with the affected node.
/// </summary>
public class NodeBoundsChangedEventArgs : EventArgs
{
    /// <summary>
    /// The node whose bounds changed.
    /// </summary>
    public Node Node { get; }

    /// <summary>
    /// The bounds change details.
    /// </summary>
    public BoundsChangedEventArgs BoundsChange { get; }

    /// <summary>
    /// Creates node bounds changed event args.
    /// </summary>
    public NodeBoundsChangedEventArgs(Node node, BoundsChangedEventArgs boundsChange)
    {
        Node = node;
        BoundsChange = boundsChange;
    }
}
