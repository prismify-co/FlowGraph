using Avalonia.Controls;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Avalonia.Rendering.PortRenderers;
using FlowGraph.Core;
using FlowGraph.Core.Diagnostics;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Node and port rendering methods.
/// </summary>
public partial class NodeVisualManager
{
    // Cache for O(1) parent lookups in GetGroupDepth
    private Dictionary<string, Node>? _nodeByIdCache;

    /// <summary>
    /// Renders all nodes in the graph to the canvas.
    /// Groups are rendered first (behind), then regular nodes.
    /// Nodes hidden by collapsed groups are not rendered.
    /// When virtualization is enabled, only nodes in the visible viewport are rendered.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="graph">The graph containing nodes.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onNodeCreated">Optional callback when a node visual is created.</param>
    public void RenderNodes(
        Canvas canvas,
        Graph graph,
        ThemeResources theme,
        Action<Control, Node>? onNodeCreated = null)
    {
        // Build node lookup cache for O(1) parent lookups in GetGroupDepth
        _nodeByIdCache = graph.Elements.Nodes.ToDictionary(n => n.Id);

        // Render groups first (they should be behind their children)
        // Order by hierarchy depth - outermost groups first
        var groups = graph.Elements.Nodes
            .Where(n => n.IsGroup && IsNodeVisible(graph, n) && IsInVisibleBounds(n))
            .OrderBy(n => GetGroupDepth(graph, n))
            .ToList();

        foreach (var group in groups)
        {
            RenderNode(canvas, group, theme, onNodeCreated);
        }

        // Then render non-group nodes that are visible
        foreach (var node in graph.Elements.Nodes.Where(n => !n.IsGroup && IsNodeVisible(graph, n) && IsInVisibleBounds(n)))
        {
            RenderNode(canvas, node, theme, onNodeCreated);
        }
    }

    /// <summary>
    /// Checks if a node is within the visible viewport bounds (with buffer for virtualization).
    /// </summary>
    private bool IsInVisibleBounds(Node node)
    {
        var bounds = _model.GetNodeBounds(node);
        return _renderContext.IsInVisibleBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Renders a single node with its ports.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="node">The node to render.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onNodeCreated">Optional callback when the node visual is created.</param>
    /// <returns>The created node visual control.</returns>
    public Control RenderNode(
        Canvas canvas,
        Node node,
        ThemeResources theme,
        Action<Control, Node>? onNodeCreated = null)
    {
        FlowGraphLogger.Debug(LogCategory.Nodes,
            $"RenderNode called for '{node.Label}' (type={node.Type}, id={node.Id})",
            "NodeVisualManager.RenderNode");

        var scale = _renderContext.Scale;
        var viewportZoom = _renderContext.ViewportZoom;
        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);

        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = scale,
            ViewportZoom = viewportZoom
        };

        // Create the node visual using the renderer
        var control = renderer.CreateNodeVisual(node, context);

        // Preserve any existing metadata (like ResizableVisual) when setting the node tag
        if (control.Tag is Dictionary<string, object> existingTags)
        {
            // Renderer used ResizableVisual or similar - preserve metadata and add node
            existingTags["Node"] = node;
        }
        else
        {
            // Simple tag - just set the node
            control.Tag = node;
        }

        // Position in canvas coordinates (transform will be applied by MainCanvas.RenderTransform)
        // The MatrixTransform handles both zoom and pan, so we use raw node positions
        var canvasX = node.Position.X;
        var canvasY = node.Position.Y;

        if (node.Type == "sequence-message")
        {
            var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
            FlowGraphLogger.Debug(LogCategory.CustomRenderers,
                $"Rendering '{node.Label}': canvasPos=({canvasX:F1},{canvasY:F1}), visualSize=({nodeWidth:F1}x{nodeHeight:F1}), scale={scale:F2}",
                "NodeVisualManager.RenderNode");
        }

        Canvas.SetLeft(control, canvasX);
        Canvas.SetTop(control, canvasY);

        canvas.Children.Add(control);
        _nodeVisuals[node.Id] = control;

        onNodeCreated?.Invoke(control, node);

        // Render ports using model for positioning (unless ShowPorts is disabled)
        if (_renderContext.Settings.ShowPorts)
        {
            for (int i = 0; i < node.Inputs.Count; i++)
            {
                RenderPort(canvas, node, node.Inputs[i], i, node.Inputs.Count, false, theme);
            }

            for (int i = 0; i < node.Outputs.Count; i++)
            {
                RenderPort(canvas, node, node.Outputs[i], i, node.Outputs.Count, true, theme);
            }
        }

        return control;
    }

    /// <summary>
    /// Renders a single port using GraphRenderModel for positioning.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="node">The parent node.</param>
    /// <param name="port">The port to render.</param>
    /// <param name="index">Index of this port among siblings.</param>
    /// <param name="totalPorts">Total number of sibling ports.</param>
    /// <param name="isOutput">True if this is an output port.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onPortCreated">Optional callback when the port visual is created.</param>
    /// <returns>The created port visual control.</returns>
    public Control RenderPort(
        Canvas canvas,
        Node node,
        Port port,
        int index,
        int totalPorts,
        bool isOutput,
        ThemeResources theme,
        Action<Control, Node, Port, bool>? onPortCreated = null)
    {
        var scale = _renderContext.Scale;
        var viewportZoom = _renderContext.ViewportZoom;
        var renderer = _portRendererRegistry.GetRenderer(port);

        var context = new PortRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = scale,
            ViewportZoom = viewportZoom,
            IsOutput = isOutput,
            Index = index,
            TotalPorts = totalPorts
        };

        // Get port size from renderer or use default
        // In transform-based rendering, use unscaled port size - the transform handles zoom
        var portSize = renderer.GetSize(port, node, _renderContext.Settings) ?? _renderContext.Settings.PortSize;

        // Use GraphRenderModel for port position calculation
        var canvasPos = _model.GetPortPositionByIndex(node, index, totalPorts, isOutput);

        // Create the port visual using the renderer
        var portVisual = renderer.CreatePortVisual(port, node, context);

        // Position in canvas coordinates (transform handles zoom/pan)
        // Use unscaled port size for offset - the MatrixTransform scales everything uniformly
        var visualLeft = canvasPos.X - portSize / 2;
        var visualTop = canvasPos.Y - portSize / 2;
        Canvas.SetLeft(portVisual, visualLeft);
        Canvas.SetTop(portVisual, visualTop);

        canvas.Children.Add(portVisual);
        _portVisuals[(node.Id, port.Id)] = portVisual;

        onPortCreated?.Invoke(portVisual, node, port, isOutput);

        return portVisual;
    }

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed ancestor group).
    /// Delegates to GraphRenderModel.IsNodeVisible.
    /// </summary>
    /// <param name="graph">The graph containing the node.</param>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is visible.</returns>
    public static bool IsNodeVisible(Graph graph, Node node)
    {
        return CanvasRenderModel.IsNodeVisible(graph, node);
    }

    /// <summary>
    /// Gets the nesting depth of a group (0 = top level).
    /// Uses cached dictionary for O(1) parent lookup instead of O(n) FirstOrDefault.
    /// </summary>
    private int GetGroupDepth(Graph graph, Node node)
    {
        int depth = 0;
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            depth++;
            // Use O(1) dictionary lookup instead of O(n) FirstOrDefault
            if (_nodeByIdCache != null && _nodeByIdCache.TryGetValue(currentParentId, out var parent))
            {
                currentParentId = parent.ParentGroupId;
            }
            else
            {
                // Fallback to slow path if cache not available
                var parentNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == currentParentId);
                if (parentNode == null) break;
                currentParentId = parentNode.ParentGroupId;
            }
        }
        return depth;
    }
}
