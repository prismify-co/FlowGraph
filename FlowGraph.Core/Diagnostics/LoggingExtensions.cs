namespace FlowGraph.Core.Diagnostics;

/// <summary>
/// Extension methods for FlowGraph diagnostic logging.
/// </summary>
public static class LoggingExtensions
{
  /// <summary>
  /// Logs node creation with detailed properties.
  /// </summary>
  public static void LogNodeCreated(this Node node, string source)
  {
    if (!FlowGraphLogger.IsLevelEnabled(LogLevel.Debug, LogCategory.Nodes)) return;

    FlowGraphLogger.Log(LogLevel.Debug, LogCategory.Nodes,
        $"Node created: {node.Id}",
        new
        {
          NodeId = node.Id,
          NodeType = node.Type,
          X = node.Position.X,
          Y = node.Position.Y,
          Width = node.Width,
          Height = node.Height,
          Label = node.Label
        },
        source);
  }

  /// <summary>
  /// Logs node position change.
  /// </summary>
  public static void LogNodeMoved(this Node node, double oldX, double oldY, string source)
  {
    if (!FlowGraphLogger.IsLevelEnabled(LogLevel.Trace, LogCategory.Nodes)) return;

    FlowGraphLogger.Log(LogLevel.Trace, LogCategory.Nodes,
        $"Node moved: {node.Id}",
        new
        {
          NodeId = node.Id,
          OldX = oldX,
          OldY = oldY,
          NewX = node.Position.X,
          NewY = node.Position.Y
        },
        source);
  }

  /// <summary>
  /// Logs edge creation with detailed properties.
  /// </summary>
  public static void LogEdgeCreated(this Edge edge, string source)
  {
    if (!FlowGraphLogger.IsLevelEnabled(LogLevel.Debug, LogCategory.Edges)) return;

    FlowGraphLogger.Log(LogLevel.Debug, LogCategory.Edges,
        $"Edge created: {edge.Source} -> {edge.Target}",
        new
        {
          SourceNode = edge.Source,
          TargetNode = edge.Target,
          SourcePort = edge.SourcePort,
          TargetPort = edge.TargetPort,
          EdgeType = edge.Type
        },
        source);
  }

  /// <summary>
  /// Logs graph state.
  /// </summary>
  public static void LogGraphState(this Graph graph, string operation, string source)
  {
    if (!FlowGraphLogger.IsLevelEnabled(LogLevel.Debug, LogCategory.Graph)) return;

    FlowGraphLogger.Log(LogLevel.Debug, LogCategory.Graph,
        $"Graph {operation}",
        new
        {
          NodeCount = graph.Elements.Nodes.Count(),
          EdgeCount = graph.Elements.Edges.Count()
        },
        source);
  }

  /// <summary>
  /// Creates a diagnostic snapshot of the graph for logging.
  /// </summary>
  public static GraphDiagnosticSnapshot CreateDiagnosticSnapshot(this Graph graph)
  {
    return new GraphDiagnosticSnapshot
    {
      Timestamp = DateTime.UtcNow,
      NodeCount = graph.Elements.Nodes.Count(),
      EdgeCount = graph.Elements.Edges.Count(),
      Nodes = graph.Elements.Nodes.Select(n => new NodeDiagnosticInfo
      {
        Id = n.Id,
        NodeType = n.Type,
        X = n.Position.X,
        Y = n.Position.Y,
        Width = n.Width ?? 0,
        Height = n.Height ?? 0,
        IsSelected = n.IsSelected,
        Label = n.Label
      }).ToList(),
      Edges = graph.Elements.Edges.Select(e => new EdgeDiagnosticInfo
      {
        SourceId = e.Source,
        TargetId = e.Target,
        SourcePortId = e.SourcePort,
        TargetPortId = e.TargetPort,
        EdgeType = e.Type.ToString()
      }).ToList()
    };
  }
}

/// <summary>
/// Diagnostic snapshot of a graph's state.
/// </summary>
public sealed class GraphDiagnosticSnapshot
{
  /// <summary>
  /// When the snapshot was taken.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  /// Number of nodes in the graph.
  /// </summary>
  public int NodeCount { get; set; }

  /// <summary>
  /// Number of edges in the graph.
  /// </summary>
  public int EdgeCount { get; set; }

  /// <summary>
  /// Detailed node information.
  /// </summary>
  public List<NodeDiagnosticInfo> Nodes { get; set; } = new();

  /// <summary>
  /// Detailed edge information.
  /// </summary>
  public List<EdgeDiagnosticInfo> Edges { get; set; } = new();

  /// <summary>
  /// Formats the snapshot as a string for logging.
  /// </summary>
  public override string ToString()
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"=== Graph Snapshot ({Timestamp:HH:mm:ss.fff}) ===");
    sb.AppendLine($"Nodes: {NodeCount}, Edges: {EdgeCount}");
    sb.AppendLine();

    if (Nodes.Count > 0)
    {
      sb.AppendLine("--- Nodes ---");
      foreach (var node in Nodes)
      {
        sb.AppendLine($"  [{node.Id}] Type={node.NodeType}, Pos=({node.X:F1},{node.Y:F1}), Size=({node.Width:F1}x{node.Height:F1}), Label=\"{node.Label}\"");
      }
    }

    if (Edges.Count > 0)
    {
      sb.AppendLine("--- Edges ---");
      foreach (var edge in Edges)
      {
        sb.AppendLine($"  {edge.SourceId}:{edge.SourcePortId} -> {edge.TargetId}:{edge.TargetPortId} [Type={edge.EdgeType}]");
      }
    }

    return sb.ToString();
  }
}

/// <summary>
/// Diagnostic information about a node.
/// </summary>
public sealed class NodeDiagnosticInfo
{
  /// <summary>
  /// Node identifier.
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// Node type.
  /// </summary>
  public string? NodeType { get; set; }

  /// <summary>
  /// X position.
  /// </summary>
  public double X { get; set; }

  /// <summary>
  /// Y position.
  /// </summary>
  public double Y { get; set; }

  /// <summary>
  /// Width.
  /// </summary>
  public double Width { get; set; }

  /// <summary>
  /// Height.
  /// </summary>
  public double Height { get; set; }

  /// <summary>
  /// Whether the node is selected.
  /// </summary>
  public bool IsSelected { get; set; }

  /// <summary>
  /// Node label.
  /// </summary>
  public string? Label { get; set; }
}

/// <summary>
/// Diagnostic information about an edge.
/// </summary>
public sealed class EdgeDiagnosticInfo
{
  /// <summary>
  /// Source node ID.
  /// </summary>
  public string SourceId { get; set; } = string.Empty;

  /// <summary>
  /// Target node ID.
  /// </summary>
  public string TargetId { get; set; } = string.Empty;

  /// <summary>
  /// Source port ID.
  /// </summary>
  public string? SourcePortId { get; set; }

  /// <summary>
  /// Target port ID.
  /// </summary>
  public string? TargetPortId { get; set; }

  /// <summary>
  /// Edge type.
  /// </summary>
  public string? EdgeType { get; set; }
}
