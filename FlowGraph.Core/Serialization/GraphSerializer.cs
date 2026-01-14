using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowGraph.Core.Elements;
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
/// Version 2 uses polymorphic elements[] array for unified element storage.
/// Version 1 compatibility maintained via separate Nodes/Edges/Shapes lists.
/// </summary>
public class GraphDto
{
    /// <summary>
    /// Format version. Version 1 uses Nodes/Edges/Shapes lists, Version 2 uses Elements array.
    /// </summary>
    public int Version { get; set; } = 2;

    public string? Id { get; set; }

    /// <summary>
    /// Polymorphic elements array (Version 2 format).
    /// Uses "kind" discriminator: "node", "edge", "shape.rectangle", etc.
    /// </summary>
    public List<ElementDto>? Elements { get; set; }

    /// <summary>
    /// Legacy nodes list (Version 1 format, maintained for backward compatibility).
    /// When deserializing, if Elements is null but Nodes is not, treat as Version 1.
    /// </summary>
    public List<NodeDto>? Nodes { get; set; }

    /// <summary>
    /// Legacy edges list (Version 1 format, maintained for backward compatibility).
    /// </summary>
    public List<EdgeDto>? Edges { get; set; }

    /// <summary>
    /// Legacy shapes list (Version 1 format, maintained for backward compatibility).
    /// </summary>
    public List<ShapeDto>? Shapes { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public static GraphDto FromGraph(Graph graph)
    {
        // Always serialize as Version 2 with elements[] array
        var elements = new List<ElementDto>();

        // Add nodes as element DTOs
        foreach (var node in graph.Elements.Nodes)
        {
            elements.Add(ElementDto.FromNode(node));
        }

        // Add edges as element DTOs
        foreach (var edge in graph.Elements.Edges)
        {
            elements.Add(ElementDto.FromEdge(edge));
        }

        // Add shapes as element DTOs
        foreach (var shape in graph.Elements.OfElementType<ShapeElement>())
        {
            var shapeDto = ElementDto.FromShape(shape);
            if (shapeDto != null)
            {
                elements.Add(shapeDto);
            }
        }

        return new GraphDto
        {
            Version = 2,
            Elements = elements
        };
    }

    public Graph ToGraph()
    {
        var graph = new Graph();

        // Version 2: Use Elements array
        if (Elements != null && Elements.Count > 0)
        {
            foreach (var elementDto in Elements)
            {
                var element = elementDto.ToElement();
                if (element != null)
                {
                    graph.AddElement(element);
                }
            }
            return graph;
        }

        // Version 1 backward compatibility: Use Nodes/Edges/Shapes lists
        if (Nodes != null)
        {
            foreach (var nodeDto in Nodes)
            {
                graph.AddNode(nodeDto.ToNode());
            }
        }

        if (Edges != null)
        {
            foreach (var edgeDto in Edges)
            {
                graph.AddEdge(edgeDto.ToEdge());
            }
        }

        if (Shapes != null)
        {
            foreach (var shapeDto in Shapes)
            {
                var shape = shapeDto.ToShape();
                if (shape != null)
                {
                    graph.AddElement(shape);
                }
            }
        }

        return graph;
    }
}

/// <summary>
/// Polymorphic element DTO base class for Version 2 format.
/// Uses "kind" discriminator to identify element type.
/// </summary>
[JsonDerivedType(typeof(NodeElementDto), "node")]
[JsonDerivedType(typeof(EdgeElementDto), "edge")]
[JsonDerivedType(typeof(ShapeElementDto), "shape")]
public abstract class ElementDto
{
    /// <summary>
    /// Element ID (unique within graph).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Element kind discriminator (e.g., "node", "edge", "shape.rectangle").
    /// Used by JsonDerivedType for polymorphic deserialization.
    /// </summary>
    public required string Kind { get; set; }

    /// <summary>
    /// Converts this DTO to a canvas element.
    /// </summary>
    public abstract ICanvasElement? ToElement();

    /// <summary>
    /// Factory method: creates NodeElementDto from a Node.
    /// </summary>
    public static NodeElementDto FromNode(Node node) => NodeElementDto.FromNodeInstance(node);

    /// <summary>
    /// Factory method: creates EdgeElementDto from an Edge.
    /// </summary>
    public static EdgeElementDto FromEdge(Edge edge) => EdgeElementDto.FromEdgeInstance(edge);

    /// <summary>
    /// Factory method: creates ShapeElementDto from a ShapeElement.
    /// </summary>
    public static ShapeElementDto? FromShape(ShapeElement shape) => ShapeElementDto.FromShapeInstance(shape);
}

/// <summary>
/// Element DTO for Node type.
/// </summary>
public class NodeElementDto : ElementDto
{
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
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; } = Elements.CanvasElement.ZIndexNodes;
    public Dictionary<string, object>? Metadata { get; set; }

    public static NodeElementDto FromNodeInstance(Node node)
    {
        return new NodeElementDto
        {
            Id = node.Id,
            Kind = "node",
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
            IsResizable = node.IsResizable,
            IsVisible = node.IsVisible,
            ZIndex = node.ZIndex
        };
    }

    public override ICanvasElement ToElement()
    {
        var definition = new NodeDefinition
        {
            Id = Id,
            Type = Type ?? "default",
            Label = Label,
            ParentGroupId = ParentGroupId,
            IsGroup = IsGroup,
            Data = Data,
            Inputs = Inputs.Select(p => new PortDefinition { Id = p.Id, Type = p.Type, Label = p.Label }).ToImmutableList(),
            Outputs = Outputs.Select(p => new PortDefinition { Id = p.Id, Type = p.Type, Label = p.Label }).ToImmutableList()
        };

        var state = new NodeState
        {
            X = Position.X,
            Y = Position.Y,
            Width = Width,
            Height = Height,
            IsCollapsed = IsCollapsed,
            IsVisible = IsVisible,
            ZIndex = ZIndex
        };

        return new Node(definition, state);
    }
}

/// <summary>
/// Element DTO for Edge type.
/// </summary>
public class EdgeElementDto : ElementDto
{
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
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; } = Elements.CanvasElement.ZIndexEdges;
    public Dictionary<string, object>? Metadata { get; set; }

    public static EdgeElementDto FromEdgeInstance(Edge edge)
    {
        return new EdgeElementDto
        {
            Id = edge.Id,
            Kind = "edge",
            Source = edge.Source,
            Target = edge.Target,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            Type = edge.Type,
            MarkerStart = edge.MarkerStart,
            MarkerEnd = edge.MarkerEnd,
            Label = edge.Label,
            AutoRoute = edge.AutoRoute,
            Waypoints = edge.Waypoints?.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList(),
            IsVisible = edge.IsVisible,
            ZIndex = edge.ZIndex
        };
    }

    public override ICanvasElement ToElement()
    {
        var definition = new EdgeDefinition
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
            AutoRoute = AutoRoute
        };

        var state = new EdgeState
        {
            Waypoints = Waypoints?.Select(p => new Point(p.X, p.Y)).ToList(),
            IsVisible = IsVisible,
            ZIndex = ZIndex
        };

        return new Edge(definition, state);
    }
}

/// <summary>
/// Element DTO for Shape types (rectangle, line, text, ellipse).
/// </summary>
public class ShapeElementDto : ElementDto
{
    public string? ShapeType { get; set; } // "rectangle", "line", "text", "ellipse"
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
    public int ZIndex { get; set; } = Elements.CanvasElement.ZIndexShapes;

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
    public string? TextAlignment { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public static ShapeElementDto? FromShapeInstance(ShapeElement shape)
    {
        var shapeType = shape.Type; // "rectangle", "line", "text", "ellipse"
        var dto = new ShapeElementDto
        {
            Id = shape.Id,
            Kind = "shape",  // Use single discriminator for polymorphic JSON
            ShapeType = shapeType,
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
            dto.TextAlignment = text.TextAlignment.ToString();
        }
        else if (shape is EllipseElement)
        {
            // Ellipse has no additional properties
        }

        return dto;
    }

    public override ICanvasElement? ToElement()
    {
        var shapeType = ShapeType?.ToLowerInvariant() ?? Kind.Replace("shape.", "");
        ShapeElement? shape = shapeType switch
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
                FontFamily = FontFamily ?? "Segoe UI",
                FontWeight = Enum.TryParse<Elements.Shapes.FontWeight>(FontWeight, out var fw) ? fw : Elements.Shapes.FontWeight.Normal,
                TextAlignment = Enum.TryParse<TextAlignment>(TextAlignment, out var ta) ? ta : Elements.Shapes.TextAlignment.Left
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

/// <summary>
/// Legacy data transfer object for Node serialization (Version 1 format).
/// Maintained for backward compatibility when deserializing old files.
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
    public double PerpendicularOffset { get; set; }

    public static LabelInfoDto FromLabelInfo(LabelInfo labelInfo)
    {
        return new LabelInfoDto
        {
            Text = labelInfo.Text,
            Anchor = labelInfo.Anchor,
            OffsetX = labelInfo.OffsetX,
            OffsetY = labelInfo.OffsetY,
            PerpendicularOffset = labelInfo.PerpendicularOffset
        };
    }

    public LabelInfo ToLabelInfo()
    {
        return new LabelInfo(Text, Anchor, OffsetX, OffsetY, PerpendicularOffset);
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
    public string? TextAlignment { get; set; }

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
            dto.TextAlignment = text.TextAlignment.ToString();
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
                FontFamily = FontFamily ?? "Segoe UI",
                FontWeight = Enum.TryParse<FontWeight>(FontWeight, out var fw) ? fw : Elements.Shapes.FontWeight.Normal,
                TextAlignment = Enum.TryParse<TextAlignment>(TextAlignment, out var ta) ? ta : Elements.Shapes.TextAlignment.Left
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

