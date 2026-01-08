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
