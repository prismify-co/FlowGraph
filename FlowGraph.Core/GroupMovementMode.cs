namespace FlowGraph.Core;

/// <summary>
/// Specifies how a group moves when dragged.
/// </summary>
public enum GroupMovementMode
{
    /// <summary>
    /// When a group is dragged, all its children move with it (default behavior).
    /// </summary>
    MoveWithChildren,
    
    /// <summary>
    /// When a group is dragged, only the group itself moves - children stay in place.
    /// This effectively repositions the group boundary around different nodes.
    /// </summary>
    MoveGroupOnly
}
