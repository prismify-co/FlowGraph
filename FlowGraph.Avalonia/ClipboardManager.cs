using FlowGraph.Core;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages clipboard operations for nodes and edges.
/// </summary>
public class ClipboardManager
{
    private List<Node>? _copiedNodes;
    private List<Edge>? _copiedEdges;
    private Core.Point _copyOffset;

    /// <summary>
    /// Gets whether there is content on the clipboard.
    /// </summary>
    public bool HasContent => _copiedNodes?.Count > 0;

    /// <summary>
    /// Copies the specified nodes and their connecting edges to the clipboard.
    /// </summary>
    public void Copy(IEnumerable<Node> nodes, IEnumerable<Edge> allEdges)
    {
        var nodeList = nodes.ToList();
        if (nodeList.Count == 0) return;

        // Calculate center of copied nodes for offset
        var centerX = nodeList.Average(n => n.Position.X);
        var centerY = nodeList.Average(n => n.Position.Y);
        _copyOffset = new Core.Point(centerX, centerY);

        // Deep copy nodes (create new instances with same properties)
        _copiedNodes = nodeList.Select(CloneNode).ToList();

        // Copy edges that connect only between copied nodes
        var nodeIds = new HashSet<string>(nodeList.Select(n => n.Id));
        _copiedEdges = allEdges
            .Where(e => nodeIds.Contains(e.Source) && nodeIds.Contains(e.Target))
            .Select(CloneEdge)
            .ToList();
    }

    /// <summary>
    /// Pastes clipboard content into the graph at the specified position.
    /// </summary>
    /// <param name="graph">The target graph.</param>
    /// <param name="pastePosition">The canvas position to paste at (center of pasted nodes).</param>
    /// <returns>The pasted nodes and edges.</returns>
    public (List<Node> nodes, List<Edge> edges) Paste(Graph graph, Core.Point pastePosition)
    {
        var pastedNodes = new List<Node>();
        var pastedEdges = new List<Edge>();

        if (_copiedNodes == null || _copiedNodes.Count == 0)
            return (pastedNodes, pastedEdges);

        // Map old IDs to new IDs
        var idMap = new Dictionary<string, string>();

        // Calculate offset from copy center to paste position
        var offsetX = pastePosition.X - _copyOffset.X;
        var offsetY = pastePosition.Y - _copyOffset.Y;

        // Create new nodes with new IDs and adjusted positions
        foreach (var copiedNode in _copiedNodes)
        {
            var newNode = CloneNode(copiedNode);
            var newId = Guid.NewGuid().ToString();
            idMap[copiedNode.Id] = newId;
            newNode.Id = newId;
            newNode.Position = new Core.Point(
                copiedNode.Position.X + offsetX,
                copiedNode.Position.Y + offsetY);
            newNode.IsSelected = true; // Select pasted nodes

            graph.AddNode(newNode);
            pastedNodes.Add(newNode);
        }

        // Create new edges with updated node references
        if (_copiedEdges != null)
        {
            foreach (var copiedEdge in _copiedEdges)
            {
                if (idMap.TryGetValue(copiedEdge.Source, out var newSourceId) &&
                    idMap.TryGetValue(copiedEdge.Target, out var newTargetId))
                {
                    var newEdge = CloneEdge(copiedEdge);
                    newEdge.Id = Guid.NewGuid().ToString();
                    newEdge.Source = newSourceId;
                    newEdge.Target = newTargetId;

                    graph.AddEdge(newEdge);
                    pastedEdges.Add(newEdge);
                }
            }
        }

        return (pastedNodes, pastedEdges);
    }

    /// <summary>
    /// Duplicates the specified nodes in place with an offset.
    /// </summary>
    /// <param name="graph">The target graph.</param>
    /// <param name="nodes">The nodes to duplicate.</param>
    /// <param name="allEdges">All edges in the graph.</param>
    /// <param name="offset">The offset for duplicated nodes.</param>
    /// <returns>The duplicated nodes and edges.</returns>
    public (List<Node> nodes, List<Edge> edges) Duplicate(
        Graph graph,
        IEnumerable<Node> nodes,
        IEnumerable<Edge> allEdges,
        Core.Point offset)
    {
        var nodeList = nodes.ToList();
        if (nodeList.Count == 0)
            return ([], []);

        // Snapshot edges before we modify the graph
        var edgeList = allEdges.ToList();

        var duplicatedNodes = new List<Node>();
        var duplicatedEdges = new List<Edge>();
        var idMap = new Dictionary<string, string>();

        // Create duplicated nodes
        foreach (var node in nodeList)
        {
            var newNode = CloneNode(node);
            var newId = Guid.NewGuid().ToString();
            idMap[node.Id] = newId;
            newNode.Id = newId;
            newNode.Position = new Core.Point(
                node.Position.X + offset.X,
                node.Position.Y + offset.Y);
            newNode.IsSelected = true;

            graph.AddNode(newNode);
            duplicatedNodes.Add(newNode);
        }

        // Duplicate edges between duplicated nodes (use the snapshot)
        var nodeIds = new HashSet<string>(nodeList.Select(n => n.Id));
        foreach (var edge in edgeList)
        {
            if (nodeIds.Contains(edge.Source) && nodeIds.Contains(edge.Target))
            {
                if (idMap.TryGetValue(edge.Source, out var newSourceId) &&
                    idMap.TryGetValue(edge.Target, out var newTargetId))
                {
                    var newEdge = CloneEdge(edge);
                    newEdge.Id = Guid.NewGuid().ToString();
                    newEdge.Source = newSourceId;
                    newEdge.Target = newTargetId;

                    graph.AddEdge(newEdge);
                    duplicatedEdges.Add(newEdge);
                }
            }
        }

        return (duplicatedNodes, duplicatedEdges);
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public void Clear()
    {
        _copiedNodes = null;
        _copiedEdges = null;
    }

    private static Node CloneNode(Node node)
    {
        return new Node
        {
            Id = node.Id,
            Type = node.Type,
            Position = node.Position,
            Width = node.Width,
            Height = node.Height,
            IsResizable = node.IsResizable,
            Data = node.Data, // Note: shallow copy of data
            Inputs = node.Inputs.Select(ClonePort).ToList(),
            Outputs = node.Outputs.Select(ClonePort).ToList()
        };
    }

    private static Port ClonePort(Port port)
    {
        return new Port
        {
            Id = port.Id,
            Type = port.Type,
            Label = port.Label
        };
    }

    private static Edge CloneEdge(Edge edge)
    {
        return new Edge
        {
            Id = edge.Id,
            Source = edge.Source,
            Target = edge.Target,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            Type = edge.Type,
            Label = edge.Label,
            MarkerStart = edge.MarkerStart,
            MarkerEnd = edge.MarkerEnd
        };
    }
}
