namespace FlowGraph.Core.Models;

/// <summary>
/// Immutable definition of a port on a node.
/// Ports define the interface for data flow between nodes.
/// </summary>
/// <remarks>
/// This is a sealed record for value equality and immutability.
/// Use <c>with</c> expressions to create modified copies.
/// </remarks>
public sealed record PortDefinition
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

  /// <summary>
  /// Maximum number of connections allowed to this port.
  /// Null means unlimited connections.
  /// </summary>
  public int? MaxConnections { get; init; }

  /// <summary>
  /// Whether this port is required to have at least one connection.
  /// </summary>
  public bool IsRequired { get; init; }

  /// <summary>
  /// Optional tooltip text shown when hovering over the port.
  /// </summary>
  /// <remarks>
  /// Provides additional context about the port's purpose, expected data type,
  /// or usage guidelines. Rendered as a native tooltip on hover.
  /// </remarks>
  public string? Tooltip { get; init; }

  /// <summary>
  /// Creates a PortDefinition from a legacy Port record.
  /// </summary>
  public static PortDefinition FromPort(Port port) => new()
  {
    Id = port.Id,
    Type = port.Type,
    Label = port.Label,
    Position = port.Position
  };

  /// <summary>
  /// Converts to a legacy Port record for backward compatibility.
  /// </summary>
  public Port ToPort() => new()
  {
    Id = Id,
    Type = Type,
    Label = Label,
    Position = Position
  };
}
