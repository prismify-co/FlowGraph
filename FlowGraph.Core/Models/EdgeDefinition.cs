namespace FlowGraph.Core.Models;

/// <summary>
/// Immutable definition of an edge's identity and connection structure.
/// Contains all properties that define "what" an edge connects, not its runtime state.
/// </summary>
/// <remarks>
/// This is a sealed record for value equality and immutability.
/// Use <c>with</c> expressions to create modified copies:
/// <code>
/// var reconnected = definition with { Target = "newNodeId" };
/// </code>
/// </remarks>
public sealed record EdgeDefinition
{
  /// <summary>
  /// Unique identifier for the edge. Required and immutable.
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// The ID of the source node.
  /// </summary>
  public required string Source { get; init; }

  /// <summary>
  /// The ID of the target node.
  /// </summary>
  public required string Target { get; init; }

  /// <summary>
  /// The ID of the source port on the source node.
  /// </summary>
  public required string SourcePort { get; init; }

  /// <summary>
  /// The ID of the target port on the target node.
  /// </summary>
  public required string TargetPort { get; init; }

  /// <summary>
  /// The visual type of the edge (bezier, straight, step, etc.).
  /// </summary>
  public EdgeType Type { get; init; } = EdgeType.Bezier;

  /// <summary>
  /// The marker to display at the start of the edge.
  /// </summary>
  public EdgeMarker MarkerStart { get; init; } = EdgeMarker.None;

  /// <summary>
  /// The marker to display at the end of the edge.
  /// </summary>
  public EdgeMarker MarkerEnd { get; init; } = EdgeMarker.Arrow;

  /// <summary>
  /// Optional label to display on the edge.
  /// </summary>
  public string? Label { get; init; }

  /// <summary>
  /// Whether this edge should use automatic routing to avoid obstacles.
  /// </summary>
  public bool AutoRoute { get; init; }

  /// <summary>
  /// Creates a new definition with a different target node and port.
  /// </summary>
  public EdgeDefinition ReconnectTarget(string newTarget, string newTargetPort) =>
      this with { Target = newTarget, TargetPort = newTargetPort };

  /// <summary>
  /// Creates a new definition with a different source node and port.
  /// </summary>
  public EdgeDefinition ReconnectSource(string newSource, string newSourcePort) =>
      this with { Source = newSource, SourcePort = newSourcePort };

  /// <summary>
  /// Creates a new definition with a different label.
  /// </summary>
  public EdgeDefinition WithLabel(string? label) =>
      this with { Label = label };

  /// <summary>
  /// Creates a new definition with a different edge type.
  /// </summary>
  public EdgeDefinition WithType(EdgeType type) =>
      this with { Type = type };
}
