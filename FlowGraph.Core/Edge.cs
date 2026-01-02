namespace FlowGraph.Core;

public class Edge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string Source { get; set; }
    public required string Target { get; set; }
    public required string SourcePort { get; set; }
    public required string TargetPort { get; set; }
}
