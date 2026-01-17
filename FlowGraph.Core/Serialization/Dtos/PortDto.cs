using FlowGraph.Core.Models;

namespace FlowGraph.Core.Serialization.Dtos;

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

    public PortDefinition ToPortDefinition()
    {
        return new PortDefinition
        {
            Id = Id,
            Type = Type,
            Label = Label
        };
    }
}
