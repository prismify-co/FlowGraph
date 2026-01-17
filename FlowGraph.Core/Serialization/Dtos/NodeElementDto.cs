using System.Collections.Immutable;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Serialization.Dtos;

/// <summary>
/// Element DTO for Node type (Version 2 format).
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
    public int ZIndex { get; set; } = CanvasElement.ZIndexNodes;
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
