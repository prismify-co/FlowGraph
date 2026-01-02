using System.Collections.ObjectModel;

namespace FlowGraph.Core;

public class Graph
{
    public ObservableCollection<Node> Nodes { get; } = [];
    public ObservableCollection<Edge> Edges { get; } = [];

    public void AddNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Add(node);
    }

    public void RemoveNode(string nodeId)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            Nodes.Remove(node);
            var edgesToRemove = Edges.Where(e => e.Source == nodeId || e.Target == nodeId).ToList();
            foreach (var edge in edgesToRemove)
            {
                Edges.Remove(edge);
            }
        }
    }

    public void AddEdge(Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        
        var sourceExists = Nodes.Any(n => n.Id == edge.Source);
        var targetExists = Nodes.Any(n => n.Id == edge.Target);
        
        if (!sourceExists || !targetExists)
        {
            throw new InvalidOperationException("Source and target nodes must exist before adding an edge.");
        }
        
        Edges.Add(edge);
    }

    public void RemoveEdge(string edgeId)
    {
        var edge = Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge != null)
        {
            Edges.Remove(edge);
        }
    }
}
