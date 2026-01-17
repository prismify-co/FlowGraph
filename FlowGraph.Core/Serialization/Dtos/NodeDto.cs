using System.Collections.Immutable;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Serialization.Dtos;

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

        var definition = new NodeDefinition
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

        var state = new NodeState
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
