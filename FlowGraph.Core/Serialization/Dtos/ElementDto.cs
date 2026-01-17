using System.Text.Json.Serialization;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Core.Serialization.Dtos;

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
