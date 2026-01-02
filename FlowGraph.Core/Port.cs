namespace FlowGraph.Core;

public record Port
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? Label { get; init; }
}
