using System.Collections.Immutable;

namespace FlowGraph.Core.Models;

/// <summary>
/// Immutable definition of a node's identity and structure.
/// Contains all properties that define "what" a node is, not "where" it is or its runtime state.
/// </summary>
/// <remarks>
/// This is a sealed record for value equality and immutability.
/// Use <c>with</c> expressions to create modified copies:
/// <code>
/// var renamed = definition with { Label = "New Label" };
/// </code>
/// </remarks>
public sealed record NodeDefinition
{
  /// <summary>
  /// Unique identifier for the node. Required and immutable.
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// The type/category of the node (e.g., "process", "decision", "input").
  /// Used for rendering and validation.
  /// </summary>
  public string Type { get; init; } = "default";

  /// <summary>
  /// Optional display label for the node.
  /// </summary>
  public string? Label { get; init; }

  /// <summary>
  /// The ID of the parent group node, if this node is part of a group.
  /// </summary>
  public string? ParentGroupId { get; init; }

  /// <summary>
  /// Whether this node is a group container.
  /// </summary>
  public bool IsGroup { get; init; }

  /// <summary>
  /// Custom padding around group children when calculating group bounds.
  /// Only applies when IsGroup is true. If null, uses the default from settings.
  /// </summary>
  public double? GroupPadding { get; init; }

  /// <summary>
  /// Input ports for this node (immutable list).
  /// </summary>
  public ImmutableList<PortDefinition> Inputs { get; init; } = [];

  /// <summary>
  /// Output ports for this node (immutable list).
  /// </summary>
  public ImmutableList<PortDefinition> Outputs { get; init; } = [];

  /// <summary>
  /// Custom user data associated with the node.
  /// Consider using immutable types for full immutability.
  /// </summary>
  public object? Data { get; init; }

  #region Capability Flags

  /// <summary>
  /// Whether this node can be selected. Default is true.
  /// </summary>
  public bool IsSelectable { get; init; } = true;

  /// <summary>
  /// Whether this node can be dragged. Default is true.
  /// </summary>
  public bool IsDraggable { get; init; } = true;

  /// <summary>
  /// Whether this node can be deleted. Default is true.
  /// </summary>
  public bool IsDeletable { get; init; } = true;

  /// <summary>
  /// Whether this node can have new connections. Default is true.
  /// </summary>
  public bool IsConnectable { get; init; } = true;

  /// <summary>
  /// Whether this node can be resized. Default is true.
  /// </summary>
  public bool IsResizable { get; init; } = true;

  #endregion

  /// <summary>
  /// Creates a NodeDefinition with the specified ports using a builder pattern.
  /// </summary>
  public NodeDefinition WithPorts(
      IEnumerable<PortDefinition>? inputs = null,
      IEnumerable<PortDefinition>? outputs = null)
  {
    return this with
    {
      Inputs = inputs?.ToImmutableList() ?? Inputs,
      Outputs = outputs?.ToImmutableList() ?? Outputs
    };
  }

  /// <summary>
  /// Adds an input port to the definition.
  /// </summary>
  public NodeDefinition AddInput(PortDefinition port) =>
      this with { Inputs = Inputs.Add(port) };

  /// <summary>
  /// Adds an output port to the definition.
  /// </summary>
  public NodeDefinition AddOutput(PortDefinition port) =>
      this with { Outputs = Outputs.Add(port) };
}
