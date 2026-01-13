namespace FlowGraph.Core;

/// <summary>
/// Position of a port on a node.
/// </summary>
public enum PortPosition
{
    /// <summary>
    /// Left side of the node (default for inputs).
    /// </summary>
    Left,

    /// <summary>
    /// Right side of the node (default for outputs).
    /// </summary>
    Right,

    /// <summary>
    /// Top side of the node.
    /// </summary>
    Top,

    /// <summary>
    /// Bottom side of the node.
    /// </summary>
    Bottom
}

/// <summary>
/// Extension methods for <see cref="PortPosition"/>.
/// </summary>
public static class PortPositionExtensions
{
    /// <summary>
    /// Gets the arrow angle in radians for an edge entering a port from the given side.
    /// This follows the GoJS convention where the angle represents the direction the arrow points.
    /// </summary>
    /// <param name="position">The port position (side of node where port is located).</param>
    /// <returns>
    /// The angle in radians:
    /// <list type="bullet">
    /// <item><see cref="PortPosition.Left"/> → π (180°) - arrow points left (edge enters from left)</item>
    /// <item><see cref="PortPosition.Right"/> → 0 - arrow points right (edge enters from right)</item>
    /// <item><see cref="PortPosition.Top"/> → -π/2 (-90°) - arrow points up (edge enters from top)</item>
    /// <item><see cref="PortPosition.Bottom"/> → π/2 (90°) - arrow points down (edge enters from bottom)</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is designed for marker/arrowhead direction calculation. The arrow points
    /// in the direction the edge is traveling when it reaches the target port.
    /// </para>
    /// <para>
    /// For a left-to-right flow (source on right side of node A, target on left side of node B),
    /// the target port position is <see cref="PortPosition.Left"/>, so the arrow should point
    /// right (angle = 0) to indicate the edge is entering from the left.
    /// </para>
    /// <para>
    /// This follows industry patterns from GoJS (getLinkDirection), React Flow (handleDirections),
    /// and mxGraph (DIRECTION_MASK).
    /// </para>
    /// </remarks>
    public static double ToIncomingArrowAngle(this PortPosition position)
    {
        return position switch
        {
            PortPosition.Left => 0,              // Edge enters from left, arrow points right
            PortPosition.Right => Math.PI,       // Edge enters from right, arrow points left (180°)
            PortPosition.Top => Math.PI / 2,     // Edge enters from top, arrow points down (90°)
            PortPosition.Bottom => -Math.PI / 2, // Edge enters from bottom, arrow points up (-90°)
            _ => 0
        };
    }

    /// <summary>
    /// Gets the outgoing direction angle in radians for an edge leaving from the given port side.
    /// </summary>
    /// <param name="position">The port position (side of node where port is located).</param>
    /// <returns>
    /// The angle in radians representing the initial direction of an outgoing edge.
    /// </returns>
    public static double ToOutgoingAngle(this PortPosition position)
    {
        return position switch
        {
            PortPosition.Left => Math.PI,        // Edge leaves going left (180°)
            PortPosition.Right => 0,             // Edge leaves going right (0°)
            PortPosition.Top => -Math.PI / 2,    // Edge leaves going up (-90°)
            PortPosition.Bottom => Math.PI / 2,  // Edge leaves going down (90°)
            _ => 0
        };
    }
}

/// <summary>
/// Represents a connection point on a node where edges can attach.
/// Ports define the interface for data flow between nodes.
/// </summary>
public record Port
{
    /// <summary>
    /// Unique identifier for the port within its node.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The data type that this port accepts or produces.
    /// Used for connection validation.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Optional display label for the port.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// The position of the port on the node.
    /// If null, defaults to Left for inputs and Right for outputs.
    /// </summary>
    public PortPosition? Position { get; init; }
}
