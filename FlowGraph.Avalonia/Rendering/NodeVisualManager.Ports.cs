using FlowGraph.Avalonia.Rendering.PortRenderers;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Port state management and animation methods.
/// </summary>
public partial class NodeVisualManager
{
    /// <summary>
    /// Updates the visual state of a port using its renderer.
    /// </summary>
    /// <param name="node">The node containing the port.</param>
    /// <param name="port">The port to update.</param>
    /// <param name="state">The new visual state.</param>
    /// <param name="theme">The current theme.</param>
    public void UpdatePortState(Node node, Port port, PortVisualState state, ThemeResources theme)
    {
        if (!_portVisuals.TryGetValue((node.Id, port.Id), out var visual)) return;

        var renderer = _portRendererRegistry.GetRenderer(port);
        var isOutput = node.Outputs.Contains(port);
        var ports = isOutput ? node.Outputs : node.Inputs;
        var index = ports.IndexOf(port);

        var context = new PortRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = 1.0, // In transform-based rendering, scale is handled by MatrixTransform
            ViewportZoom = _renderContext.ViewportZoom,
            IsOutput = isOutput,
            Index = index,
            TotalPorts = ports.Count
        };

        renderer.UpdateState(visual, port, node, context, state);
    }

    /// <summary>
    /// Updates the visual state of all ports in the graph based on their connection status.
    /// This should be called after rendering to ensure animated ports start their animations.
    /// Detects state changes to trigger one-shot animations (e.g., ripple on connect).
    /// </summary>
    /// <param name="graph">The graph containing the nodes and edges.</param>
    /// <param name="theme">The current theme.</param>
    public void UpdateAllPortStates(Graph graph, ThemeResources theme)
    {
        // Build a map of connected ports with connection counts
        var connectionCounts = new Dictionary<(string nodeId, string portId), int>();
        foreach (var edge in graph.Elements.Edges)
        {
            var sourceKey = (edge.Source, edge.SourcePort);
            var targetKey = (edge.Target, edge.TargetPort);
            connectionCounts[sourceKey] = connectionCounts.GetValueOrDefault(sourceKey) + 1;
            connectionCounts[targetKey] = connectionCounts.GetValueOrDefault(targetKey) + 1;
        }

        // Update each port's visual state
        foreach (var node in graph.Elements.Nodes)
        {
            UpdatePortStatesForNode(node, node.Inputs, connectionCounts, theme);
            UpdatePortStatesForNode(node, node.Outputs, connectionCounts, theme);
        }
    }

    private void UpdatePortStatesForNode(
        Node node,
        IReadOnlyList<Port> ports,
        Dictionary<(string nodeId, string portId), int> connectionCounts,
        ThemeResources theme)
    {
        foreach (var port in ports)
        {
            var key = (node.Id, port.Id);
            var connectionCount = connectionCounts.GetValueOrDefault(key);
            var isConnected = connectionCount > 0;

            // Detect state change
            var previousState = _previousPortStates.GetValueOrDefault(key);
            var wasConnected = previousState?.IsConnected ?? false;

            PortStateChange change = PortStateChange.None;
            if (isConnected && !wasConnected)
            {
                change = PortStateChange.JustConnected;
            }
            else if (!isConnected && wasConnected)
            {
                change = PortStateChange.JustDisconnected;
            }

            var state = new PortVisualState
            {
                IsConnected = isConnected,
                ConnectionCount = connectionCount,
                Change = change
            };

            UpdatePortState(node, port, state, theme);

            // Store for next comparison (but clear the Change flag)
            _previousPortStates[key] = state with { Change = PortStateChange.None };
        }
    }

    /// <summary>
    /// Triggers a data pulse animation on a specific port.
    /// </summary>
    /// <param name="node">The node containing the port.</param>
    /// <param name="port">The port to trigger the pulse on.</param>
    /// <param name="theme">The current theme.</param>
    public void TriggerPortDataPulse(Node node, Port port, ThemeResources theme)
    {
        if (!_portVisuals.TryGetValue((node.Id, port.Id), out var visual)) return;

        var renderer = _portRendererRegistry.GetRenderer(port);
        var isOutput = node.Outputs.Contains(port);
        var ports = isOutput ? node.Outputs : node.Inputs;
        var index = ports.IndexOf(port);

        var context = new PortRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = 1.0,
            ViewportZoom = _renderContext.ViewportZoom,
            IsOutput = isOutput,
            Index = index,
            TotalPorts = ports.Count
        };

        renderer.TriggerDataPulse(visual, port, node, context);
    }

    /// <summary>
    /// Triggers an error animation on a specific port.
    /// </summary>
    /// <param name="node">The node containing the port.</param>
    /// <param name="port">The port to show error on.</param>
    /// <param name="message">Optional error message.</param>
    public void TriggerPortError(Node node, Port port, string? message = null)
    {
        if (!_portVisuals.TryGetValue((node.Id, port.Id), out var visual)) return;
        var renderer = _portRendererRegistry.GetRenderer(port);
        renderer.TriggerError(visual, port, node, message);
    }

    /// <summary>
    /// Triggers a success animation on a specific port.
    /// </summary>
    /// <param name="node">The node containing the port.</param>
    /// <param name="port">The port to show success on.</param>
    public void TriggerPortSuccess(Node node, Port port)
    {
        if (!_portVisuals.TryGetValue((node.Id, port.Id), out var visual)) return;
        var renderer = _portRendererRegistry.GetRenderer(port);
        renderer.TriggerSuccess(visual, port, node);
    }
}
