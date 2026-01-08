using FlowGraph.Core;
using FlowGraph.Core.Models;
using System.Collections.Immutable;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Helper methods for creating test nodes and edges with the new Definition/State architecture.
/// </summary>
public static class TestHelpers
{
  /// <summary>
  /// Creates a test node with the specified id and optional parameters.
  /// </summary>
  public static Node CreateNode(
      string id,
      string type = "default",
      double x = 0,
      double y = 0,
      double? width = null,
      double? height = null,
      string? label = null,
      bool isGroup = false,
      string? parentGroupId = null,
      List<Port>? inputs = null,
      List<Port>? outputs = null,
      object? data = null)
  {
    var definition = new NodeDefinition
    {
      Id = id,
      Type = type,
      Label = label,
      IsGroup = isGroup,
      ParentGroupId = parentGroupId,
      Data = data,
      Inputs = inputs?.Select(PortDefinition.FromPort).ToImmutableList() ?? [],
      Outputs = outputs?.Select(PortDefinition.FromPort).ToImmutableList() ?? []
    };

    var state = new NodeState
    {
      X = x,
      Y = y,
      Width = width,
      Height = height
    };

    return new Node(definition, state);
  }

  /// <summary>
  /// Creates a test node with simple inputs and outputs.
  /// </summary>
  public static Node CreateNodeWithPorts(
      string id,
      string[] inputTypes,
      string[] outputTypes,
      double x = 0,
      double y = 0)
  {
    var inputs = inputTypes.Select((t, i) => new Port { Id = $"in{i}", Type = t }).ToList();
    var outputs = outputTypes.Select((t, i) => new Port { Id = $"out{i}", Type = t }).ToList();

    return CreateNode(id, x: x, y: y, inputs: inputs, outputs: outputs);
  }

  /// <summary>
  /// Creates a test edge with the specified parameters.
  /// </summary>
  public static Edge CreateEdge(
      string id,
      string source,
      string target,
      string sourcePort = "out0",
      string targetPort = "in0",
      EdgeType type = EdgeType.Bezier,
      string? label = null,
      List<Point>? waypoints = null)
  {
    var definition = new EdgeDefinition
    {
      Id = id,
      Source = source,
      Target = target,
      SourcePort = sourcePort,
      TargetPort = targetPort,
      Type = type,
      Label = label
    };

    var state = new EdgeState
    {
      Waypoints = waypoints
    };

    return new Edge(definition, state);
  }

  /// <summary>
  /// Creates a simple test graph with the specified number of nodes.
  /// </summary>
  public static Graph CreateSimpleGraph(int nodeCount)
  {
    var graph = new Graph();
    for (int i = 0; i < nodeCount; i++)
    {
      graph.AddNode(CreateNode($"node{i}", x: i * 100, y: 0));
    }
    return graph;
  }

  /// <summary>
  /// Creates a linear chain of connected nodes.
  /// </summary>
  public static Graph CreateLinearGraph(int nodeCount)
  {
    var graph = new Graph();

    for (int i = 0; i < nodeCount; i++)
    {
      var node = CreateNodeWithPorts($"node{i}", ["any"], ["any"], x: i * 150, y: 0);
      graph.AddNode(node);
    }

    for (int i = 0; i < nodeCount - 1; i++)
    {
      var edge = CreateEdge($"edge{i}", $"node{i}", $"node{i + 1}");
      graph.AddEdge(edge);
    }

    return graph;
  }
}
