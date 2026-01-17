using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Core.Serialization.Dtos;

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
