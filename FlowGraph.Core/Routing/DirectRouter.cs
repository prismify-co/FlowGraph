namespace FlowGraph.Core.Routing;

/// <summary>
/// Simple edge router that doesn't modify paths (direct connection).
/// </summary>
public class DirectRouter : IEdgeRouter
{
    public static DirectRouter Instance { get; } = new();

    public IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge)
    {
        var sourceNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = context.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return [];

        var sourcePort = GetPortPosition(sourceNode, edge.SourcePort, true, context);
        var targetPort = GetPortPosition(targetNode, edge.TargetPort, false, context);

        return [sourcePort, targetPort];
    }

    /// <summary>
    /// Gets the position of a port on a node.
    /// Distributes ports evenly along the node edge based on port index.
    /// </summary>
    public static Point GetPortPosition(Node node, string? portId, bool isOutput, EdgeRoutingContext context)
    {
        var nodeWidth = node.Width ?? context.DefaultNodeWidth;
        var nodeHeight = node.Height ?? context.DefaultNodeHeight;

        // Find port index
        var ports = isOutput ? node.Outputs : node.Inputs;
        var portIndex = 0;
        if (!string.IsNullOrEmpty(portId))
        {
            var idx = ports.FindIndex(p => p.Id == portId);
            if (idx >= 0)
            {
                portIndex = idx;
            }
            else
            {
                // Port ID specified but not found - fall back to first port position.
                // This can happen if edge references a port that was removed or renamed.
                System.Diagnostics.Debug.WriteLine(
                    $"[DirectRouter] Port '{portId}' not found on node '{node.Id}'. " +
                    $"Available {(isOutput ? "outputs" : "inputs")}: [{string.Join(", ", ports.Select(p => p.Id))}]. " +
                    "Falling back to index 0.");
            }
        }

        var totalPorts = Math.Max(1, ports.Count);
        var spacing = nodeHeight / (totalPorts + 1);
        var portY = node.Position.Y + spacing * (portIndex + 1);
        var portX = isOutput ? node.Position.X + nodeWidth : node.Position.X;

        return new Point(portX, portY);
    }
}
