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

public record Port
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? Label { get; init; }

    /// <summary>
    /// The position of the port on the node.
    /// If null, defaults to Left for inputs and Right for outputs.
    /// </summary>
    public PortPosition? Position { get; init; }
}
