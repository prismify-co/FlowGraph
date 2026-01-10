using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowGraph.Core.Models;
using FlowGraph.Core.Elements.Shapes;

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
    public List<ShapeDto> Shapes { get; set; } = [];
    public Dictionary<string, object>? Metadata { get; set; }

    public static GraphDto FromGraph(Graph graph)
    {
        return new GraphDto
        {
            Nodes = graph.Elements.Nodes.Select(NodeDto.FromNode).ToList(),
            Edges = graph.Elements.Edges.Select(EdgeDto.FromEdge).ToList(),
            Shapes = graph.Elements.OfElementType<ShapeElement>().Select(ShapeDto.FromShape).ToList()
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

        foreach (var shapeDto in Shapes)
        {
            var shape = shapeDto.ToShape();
            if (shape != null)
            {
                graph.AddElement(shape);
            }
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
        var inputPorts = Inputs.Select(i => i.ToPortDefinition()).ToImmutableList();
        var outputPorts = Outputs.Select(o => o.ToPortDefinition()).ToImmutableList();

        var definition = new Models.NodeDefinition
        {
            Id = Id,
            Type = Type ?? "default",
            Label = Label,
            Data = Data,
            IsGroup = IsGroup,
            ParentGroupId = ParentGroupId,
            IsResizable = IsResizable,
            Inputs = inputPorts,
            Outputs = outputPorts
        };

        var state = new Models.NodeState
        {
            X = Position.X,
            Y = Position.Y,
            Width = Width,
            Height = Height,
            IsCollapsed = IsCollapsed
        };

        return new Node(definition, state);
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
    public LabelInfoDto? LabelInfo { get; set; }
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
            LabelInfo = edge.Definition.LabelInfo != null
                ? LabelInfoDto.FromLabelInfo(edge.Definition.LabelInfo)
                : null,
            AutoRoute = edge.AutoRoute,
            Waypoints = edge.Waypoints?.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
        };
    }

    public Edge ToEdge()
    {
        var definition = new Models.EdgeDefinition
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
            LabelInfo = LabelInfo?.ToLabelInfo(),
            AutoRoute = AutoRoute
        };

        var state = new Models.EdgeState
        {
            Waypoints = Waypoints?.Select(p => new Point(p.X, p.Y)).ToList()
        };

        return new Edge(definition, state);
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

    public Models.PortDefinition ToPortDefinition()
    {
        return new Models.PortDefinition
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

/// <summary>
/// Data transfer object for LabelInfo serialization.
/// </summary>
public class LabelInfoDto
{
    public required string Text { get; set; }
    public LabelAnchor Anchor { get; set; } = LabelAnchor.Center;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }

    public static LabelInfoDto FromLabelInfo(LabelInfo labelInfo)
    {
        return new LabelInfoDto
        {
            Text = labelInfo.Text,
            Anchor = labelInfo.Anchor,
            OffsetX = labelInfo.OffsetX,
            OffsetY = labelInfo.OffsetY
        };
    }

    public LabelInfo ToLabelInfo()
    {
        return new LabelInfo(Text, Anchor, OffsetX, OffsetY);
    }
}

/// <summary>
/// Data transfer object for Shape serialization.
/// Supports polymorphic shape types (rectangle, line, text, ellipse).
/// </summary>
public class ShapeDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public PointDto Position { get; set; } = new();
    public double? Width { get; set; }
    public double? Height { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double StrokeWidth { get; set; } = 1.0;
    public double Opacity { get; set; } = 1.0;
    public double Rotation { get; set; }
    public string? Label { get; set; }
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; } = CanvasElement.ZIndexShapes;

    // Rectangle-specific
    public double? CornerRadius { get; set; }

    // Line-specific
    public double? EndX { get; set; }
    public double? EndY { get; set; }
    public string? StrokeDashArray { get; set; }
    public string? StartCap { get; set; }
    public string? EndCap { get; set; }

    // Text-specific
    public string? Text { get; set; }
    public double? FontSize { get; set; }
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? TextAlign { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public static ShapeDto FromShape(ShapeElement shape)
    {
        var dto = new ShapeDto
        {
            Id = shape.Id,
            Type = shape.Type,
            Position = new PointDto { X = shape.Position.X, Y = shape.Position.Y },
            Width = shape.Width,
            Height = shape.Height,
            Fill = shape.Fill,
            Stroke = shape.Stroke,
            StrokeWidth = shape.StrokeWidth,
            Opacity = shape.Opacity,
            Rotation = shape.Rotation,
            Label = shape.Label,
            IsVisible = shape.IsVisible,
            ZIndex = shape.ZIndex
        };

        // Populate type-specific properties
        if (shape is RectangleElement rect)
        {
            dto.CornerRadius = rect.CornerRadius;
        }
        else if (shape is LineElement line)
        {
            dto.EndX = line.EndX;
            dto.EndY = line.EndY;
            dto.StrokeDashArray = line.StrokeDashArray;
            dto.StartCap = line.StartCap.ToString();
            dto.EndCap = line.EndCap.ToString();
        }
        else if (shape is TextElement text)
        {
            dto.Text = text.Text;
            dto.FontSize = text.FontSize;
            dto.FontFamily = text.FontFamily;
            dto.FontWeight = text.FontWeight.ToString();
            dto.TextAlign = text.TextAlign.ToString();
        }
        else if (shape is EllipseElement)
        {
            // Ellipse has no additional properties beyond base
        }

        return dto;
    }

    public ShapeElement? ToShape()
    {
        ShapeElement? shape = Type.ToLowerInvariant() switch
        {
            "rectangle" => new RectangleElement(Id)
            {
                CornerRadius = CornerRadius ?? 0
            },
            "line" => new LineElement(Id)
            {
                EndX = EndX ?? 0,
                EndY = EndY ?? 0,
                StrokeDashArray = StrokeDashArray,
                StartCap = Enum.TryParse<LineCapStyle>(StartCap, out var sc) ? sc : LineCapStyle.None,
                EndCap = Enum.TryParse<LineCapStyle>(EndCap, out var ec) ? ec : LineCapStyle.None
            },
            "text" => new TextElement(Id)
            {
                Text = Text ?? string.Empty,
                FontSize = FontSize ?? 14,
                FontFamily = FontFamily,
                FontWeight = Enum.TryParse<FontWeight>(FontWeight, out var fw) ? fw : Elements.Shapes.FontWeight.Normal,
                TextAlign = Enum.TryParse<TextAlign>(TextAlign, out var ta) ? ta : Elements.Shapes.TextAlign.Left
            },
            "ellipse" => new EllipseElement(Id),
            _ => null
        };

        if (shape != null)
        {
            shape.Position = new Point(Position.X, Position.Y);
            shape.Width = Width;
            shape.Height = Height;
            shape.Fill = Fill;
            shape.Stroke = Stroke;
            shape.StrokeWidth = StrokeWidth;
            shape.Opacity = Opacity;
            shape.Rotation = Rotation;
            shape.Label = Label;
            shape.IsVisible = IsVisible;
            shape.ZIndex = ZIndex;
        }

        return shape;
    }
}

