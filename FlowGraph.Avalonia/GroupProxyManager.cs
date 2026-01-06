using FlowGraph.Core;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages proxy ports and edge re-routing when groups are collapsed.
/// When a group is collapsed, edges that cross the group boundary are re-routed
/// to proxy ports on the group, maintaining visual connectivity.
/// </summary>
public class GroupProxyManager
{
    private readonly Func<Graph?> _getGraph;
    
    /// <summary>
    /// Tracks the original edge info for edges that have been re-routed to proxy ports.
    /// Key: proxy edge ID, Value: original edge info
    /// </summary>
    private readonly Dictionary<string, ProxyEdgeInfo> _proxyEdges = new();
    
    /// <summary>
    /// Tracks proxy ports created on groups.
    /// Key: group ID, Value: list of proxy port info
    /// </summary>
    private readonly Dictionary<string, List<ProxyPortInfo>> _proxyPorts = new();

    /// <summary>
    /// Event raised when proxy state changes and re-render is needed.
    /// </summary>
    public event EventHandler? ProxyStateChanged;

    public GroupProxyManager(Func<Graph?> getGraph)
    {
        _getGraph = getGraph;
    }

    /// <summary>
    /// Creates proxy ports and re-routes edges when a group is collapsed.
    /// </summary>
    public void OnGroupCollapsed(string groupId)
    {
        var graph = _getGraph();
        if (graph == null) return;

        var group = graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null) return;

        // Find all edges that cross this group's boundary
        var crossingEdges = graph.GetEdgesCrossingGroup(groupId).ToList();
        if (crossingEdges.Count == 0) return;

        var proxyPortsForGroup = new List<ProxyPortInfo>();
        var childIds = graph.GetGroupChildrenRecursive(groupId).Select(n => n.Id).ToHashSet();

        foreach (var edge in crossingEdges)
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);
            if (sourceNode == null || targetNode == null) continue;

            var sourceInGroup = childIds.Contains(edge.Source);
            var targetInGroup = childIds.Contains(edge.Target);

            if (sourceInGroup && !targetInGroup)
            {
                // Edge goes FROM inside group TO outside - need output proxy port
                var proxyPort = GetOrCreateProxyPort(group, proxyPortsForGroup, edge.SourcePort, true, edge.Source);
                
                // Store original edge info and update edge to use proxy
                _proxyEdges[edge.Id] = new ProxyEdgeInfo
                {
                    OriginalSource = edge.Source,
                    OriginalSourcePort = edge.SourcePort,
                    OriginalTarget = edge.Target,
                    OriginalTargetPort = edge.TargetPort,
                    ProxyGroupId = groupId,
                    IsSourceProxied = true
                };

                // Update edge to connect from group's proxy port
                edge.Source = groupId;
                edge.SourcePort = proxyPort.PortId;
            }
            else if (!sourceInGroup && targetInGroup)
            {
                // Edge goes FROM outside group TO inside - need input proxy port
                var proxyPort = GetOrCreateProxyPort(group, proxyPortsForGroup, edge.TargetPort, false, edge.Target);
                
                // Store original edge info and update edge to use proxy
                _proxyEdges[edge.Id] = new ProxyEdgeInfo
                {
                    OriginalSource = edge.Source,
                    OriginalSourcePort = edge.SourcePort,
                    OriginalTarget = edge.Target,
                    OriginalTargetPort = edge.TargetPort,
                    ProxyGroupId = groupId,
                    IsSourceProxied = false
                };

                // Update edge to connect to group's proxy port
                edge.Target = groupId;
                edge.TargetPort = proxyPort.PortId;
            }
        }

        if (proxyPortsForGroup.Count > 0)
        {
            _proxyPorts[groupId] = proxyPortsForGroup;
            
            // Add proxy ports to the group node
            foreach (var proxyInfo in proxyPortsForGroup)
            {
                var port = new Port
                {
                    Id = proxyInfo.PortId,
                    Type = "proxy",
                    Label = proxyInfo.Label
                };

                if (proxyInfo.IsOutput)
                    group.Outputs.Add(port);
                else
                    group.Inputs.Add(port);
            }
        }

        ProxyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes proxy ports and restores original edges when a group is expanded.
    /// </summary>
    public void OnGroupExpanded(string groupId)
    {
        var graph = _getGraph();
        if (graph == null) return;

        var group = graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null) return;

        // Restore original edges
        var edgesToRestore = _proxyEdges
            .Where(kv => kv.Value.ProxyGroupId == groupId)
            .ToList();

        foreach (var (edgeId, proxyInfo) in edgesToRestore)
        {
            var edge = graph.Edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge != null)
            {
                edge.Source = proxyInfo.OriginalSource;
                edge.SourcePort = proxyInfo.OriginalSourcePort;
                edge.Target = proxyInfo.OriginalTarget;
                edge.TargetPort = proxyInfo.OriginalTargetPort;
            }
            _proxyEdges.Remove(edgeId);
        }

        // Remove proxy ports from group
        if (_proxyPorts.TryGetValue(groupId, out var proxyPorts))
        {
            foreach (var proxyInfo in proxyPorts)
            {
                if (proxyInfo.IsOutput)
                    group.Outputs.RemoveAll(p => p.Id == proxyInfo.PortId);
                else
                    group.Inputs.RemoveAll(p => p.Id == proxyInfo.PortId);
            }
            _proxyPorts.Remove(groupId);
        }

        ProxyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Checks if an edge is currently using a proxy port.
    /// </summary>
    public bool IsProxyEdge(string edgeId)
    {
        return _proxyEdges.ContainsKey(edgeId);
    }

    /// <summary>
    /// Gets proxy edge info if the edge is proxied.
    /// </summary>
    public ProxyEdgeInfo? GetProxyEdgeInfo(string edgeId)
    {
        return _proxyEdges.TryGetValue(edgeId, out var info) ? info : null;
    }

    /// <summary>
    /// Gets all proxy ports for a group.
    /// </summary>
    public IReadOnlyList<ProxyPortInfo> GetProxyPorts(string groupId)
    {
        return _proxyPorts.TryGetValue(groupId, out var ports) 
            ? ports 
            : Array.Empty<ProxyPortInfo>();
    }

    /// <summary>
    /// Checks if a port is a proxy port.
    /// </summary>
    public bool IsProxyPort(string nodeId, string portId)
    {
        if (!_proxyPorts.TryGetValue(nodeId, out var ports))
            return false;

        return ports.Any(p => p.PortId == portId);
    }

    /// <summary>
    /// Clears all proxy state (call when graph changes completely).
    /// </summary>
    public void Clear()
    {
        _proxyEdges.Clear();
        _proxyPorts.Clear();
    }

    private ProxyPortInfo GetOrCreateProxyPort(
        Node group, 
        List<ProxyPortInfo> proxyPortsForGroup, 
        string originalPortId, 
        bool isOutput,
        string originalNodeId)
    {
        // Check if we already have a proxy for this original port
        var existing = proxyPortsForGroup.FirstOrDefault(p => 
            p.OriginalPortId == originalPortId && 
            p.OriginalNodeId == originalNodeId &&
            p.IsOutput == isOutput);
        
        if (existing != null)
            return existing;

        // Create new proxy port
        var proxyPort = new ProxyPortInfo
        {
            PortId = $"proxy_{originalNodeId}_{originalPortId}",
            OriginalPortId = originalPortId,
            OriginalNodeId = originalNodeId,
            IsOutput = isOutput,
            Label = isOutput ? "?" : "?"
        };

        proxyPortsForGroup.Add(proxyPort);
        return proxyPort;
    }
}

/// <summary>
/// Information about a proxied edge.
/// </summary>
public class ProxyEdgeInfo
{
    public required string OriginalSource { get; init; }
    public required string OriginalSourcePort { get; init; }
    public required string OriginalTarget { get; init; }
    public required string OriginalTargetPort { get; init; }
    public required string ProxyGroupId { get; init; }
    public required bool IsSourceProxied { get; init; }
}

/// <summary>
/// Information about a proxy port on a collapsed group.
/// </summary>
public class ProxyPortInfo
{
    public required string PortId { get; init; }
    public required string OriginalPortId { get; init; }
    public required string OriginalNodeId { get; init; }
    public required bool IsOutput { get; init; }
    public string? Label { get; init; }
}
