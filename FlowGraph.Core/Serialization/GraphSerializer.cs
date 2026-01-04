using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowGraph.Core.Serialization;

/// <summary>
/// Serializes and deserializes graphs to/from JSON.
/// </summary>
public static class GraphSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        return options;
    }

    /// <summary>
    /// Serializes a graph to a JSON string.
    /// </summary>
    public static string Serialize(Graph graph, JsonSerializerOptions? options = null)
    {
        var dto = GraphDto.FromGraph(graph);
        return JsonSerializer.Serialize(dto, options ?? DefaultOptions);
    }

    /// <summary>
    /// Serializes a graph to a JSON file.
    /// </summary>
    public static async Task SerializeToFileAsync(Graph graph, string filePath, JsonSerializerOptions? options = null)
    {
        var dto = GraphDto.FromGraph(graph);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, dto, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes a graph from a JSON string.
    /// </summary>
    public static Graph? Deserialize(string json, JsonSerializerOptions? options = null)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<GraphDto>(json, options ?? DefaultOptions);
            return dto?.ToGraph();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes a graph from a JSON file.
    /// </summary>
    public static async Task<Graph?> DeserializeFromFileAsync(string filePath, JsonSerializerOptions? options = null)
    {
        await using var stream = File.OpenRead(filePath);
        var dto = await JsonSerializer.DeserializeAsync<GraphDto>(stream, options ?? DefaultOptions);
        return dto?.ToGraph();
    }

    /// <summary>
    /// Validates JSON without fully deserializing.
    /// </summary>
    public static bool TryValidate(string json, out string? errorMessage)
    {
        try
        {
            JsonSerializer.Deserialize<GraphDto>(json, DefaultOptions);
            errorMessage = null;
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}

/// <summary>
/// Data transfer object for Graph serialization.
/// </summary>
public class GraphDto
{
    public string? Id { get; set; }
    public List<NodeDto> Nodes { get; set; } = [];
    public List<EdgeDto> Edges { get; set; } = [];
    public Dictionary<string, object>? Metadata { get; set; }

    public static GraphDto FromGraph(Graph graph)
    {
        return new GraphDto
        {
            Nodes = graph.Nodes.Select(NodeDto.FromNode).ToList(),
            Edges = graph.Edges.Select(EdgeDto.FromEdge).ToList()
        };
    }

    public Graph ToGraph()
    {
        var graph = new Graph();

        foreach (var nodeDto in Nodes)
        {
            graph.AddNode(nodeDto.ToNode());
        }

        foreach (var edgeDto in Edges)
        {
            graph.AddEdge(edgeDto.ToEdge());
        }

        return graph;
    }
}

/// <summary>
/// Data transfer object for Node serialization.
/// </summary>
public class NodeDto
{
    public required string Id { get; set; }
    public string? Type { get; set; }
    public string? Label { get; set; }
    public PointDto Position { get; set; } = new();
    public double? Width { get; set; }
    public double? Height { get; set; }
    public object? Data { get; set; }
    public List<PortDto> Inputs { get; set; } = [];
    public List<PortDto> Outputs { get; set; } = [];
    public bool IsGroup { get; set; }
    public bool IsCollapsed { get; set; }
    public string? ParentGroupId { get; set; }
    public bool IsResizable { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static NodeDto FromNode(Node node)
    {
        return new NodeDto
        {
            Id = node.Id,
            Type = node.Type,
            Label = node.Label,
            Position = new PointDto { X = node.Position.X, Y = node.Position.Y },
            Width = node.Width,
            Height = node.Height,
            Data = node.Data,
            Inputs = node.Inputs.Select(PortDto.FromPort).ToList(),
            Outputs = node.Outputs.Select(PortDto.FromPort).ToList(),
            IsGroup = node.IsGroup,
            IsCollapsed = node.IsCollapsed,
            ParentGroupId = node.ParentGroupId,
            IsResizable = node.IsResizable
        };
    }

    public Node ToNode()
    {
        var node = new Node
        {
            Id = Id,
            Type = Type,
            Label = Label,
            Position = new Point(Position.X, Position.Y),
            Width = Width,
            Height = Height,
            Data = Data,
            IsGroup = IsGroup,
            IsCollapsed = IsCollapsed,
            ParentGroupId = ParentGroupId,
            IsResizable = IsResizable
        };

        foreach (var input in Inputs)
        {
            node.Inputs.Add(input.ToPort());
        }

        foreach (var output in Outputs)
        {
            node.Outputs.Add(output.ToPort());
        }

        return node;
    }
}

/// <summary>
/// Data transfer object for Edge serialization.
/// </summary>
public class EdgeDto
{
    public required string Id { get; set; }
    public required string Source { get; set; }
    public required string Target { get; set; }
    public required string SourcePort { get; set; }
    public required string TargetPort { get; set; }
    public EdgeType Type { get; set; } = EdgeType.Bezier;
    public EdgeMarker MarkerStart { get; set; } = EdgeMarker.None;
    public EdgeMarker MarkerEnd { get; set; } = EdgeMarker.Arrow;
    public string? Label { get; set; }
    public bool AutoRoute { get; set; }
    public List<PointDto>? Waypoints { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static EdgeDto FromEdge(Edge edge)
    {
        return new EdgeDto
        {
            Id = edge.Id,
            Source = edge.Source,
            Target = edge.Target,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            Type = edge.Type,
            MarkerStart = edge.MarkerStart,
            MarkerEnd = edge.MarkerEnd,
            Label = edge.Label,
            AutoRoute = edge.AutoRoute,
            Waypoints = edge.Waypoints?.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
        };
    }

    public Edge ToEdge()
    {
        return new Edge
        {
            Id = Id,
            Source = Source,
            Target = Target,
            SourcePort = SourcePort,
            TargetPort = TargetPort,
            Type = Type,
            MarkerStart = MarkerStart,
            MarkerEnd = MarkerEnd,
            Label = Label,
            AutoRoute = AutoRoute,
            Waypoints = Waypoints?.Select(p => new Point(p.X, p.Y)).ToList()
        };
    }
}

/// <summary>
/// Data transfer object for Port serialization.
/// </summary>
public class PortDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public string? Label { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static PortDto FromPort(Port port)
    {
        return new PortDto
        {
            Id = port.Id,
            Type = port.Type,
            Label = port.Label
        };
    }

    public Port ToPort()
    {
        return new Port
        {
            Id = Id,
            Type = Type,
            Label = Label
        };
    }
}

/// <summary>
/// Data transfer object for Point serialization.
/// </summary>
public class PointDto
{
    public double X { get; set; }
    public double Y { get; set; }
}
