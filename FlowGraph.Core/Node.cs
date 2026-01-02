namespace FlowGraph.Core;

public class Node
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "default";
    public Point Position { get; set; }
    public object? Data { get; set; }
    public List<Port> Inputs { get; set; } = [];
    public List<Port> Outputs { get; set; } = [];
}
