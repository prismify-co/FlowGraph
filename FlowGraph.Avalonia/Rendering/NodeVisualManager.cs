using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Avalonia.Rendering.PortRenderers;
using FlowGraph.Core;
using FlowGraph.Core.Diagnostics;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Manages rendering and tracking of node and port visuals.
/// Responsible for creating, updating, and removing node/port UI elements.
/// Uses GraphRenderModel for all geometry calculations to ensure visual parity with DirectGraphRenderer.
/// </summary>
public class NodeVisualManager
{
    private readonly RenderContext _renderContext;
    private readonly NodeRendererRegistry _nodeRendererRegistry;
    private readonly PortRendererRegistry _portRendererRegistry;
    private readonly GraphRenderModel _model;

    // Visual tracking
    private readonly Dictionary<string, Control> _nodeVisuals = new();
    private readonly Dictionary<(string nodeId, string portId), Control> _portVisuals = new();

    /// <summary>
    /// Creates a new node visual manager.
    /// </summary>
    /// <param name="renderContext">Shared render context.</param>
    /// <param name="nodeRendererRegistry">Registry for custom node renderers. If null, a default registry is created.</param>
    /// <param name="portRendererRegistry">Registry for custom port renderers. If null, a default registry is created.</param>
    public NodeVisualManager(
        RenderContext renderContext,
        NodeRendererRegistry? nodeRendererRegistry = null,
        PortRendererRegistry? portRendererRegistry = null)
    {
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
        _nodeRendererRegistry = nodeRendererRegistry ?? new NodeRendererRegistry();
        _portRendererRegistry = portRendererRegistry ?? new PortRendererRegistry();
        _model = new GraphRenderModel(renderContext.Settings, _nodeRendererRegistry);
    }

    /// <summary>
    /// Gets the node renderer registry for registering custom node types.
    /// </summary>
    public NodeRendererRegistry NodeRenderers => _nodeRendererRegistry;

    /// <summary>
    /// Gets the port renderer registry for registering custom port types.
    /// </summary>
    public PortRendererRegistry PortRenderers => _portRendererRegistry;

    /// <summary>
    /// Gets the render model used for geometry calculations.
    /// </summary>
    public GraphRenderModel Model => _model;

    /// <summary>
    /// Updates the settings used by this manager and its render model.
    /// </summary>
    /// <param name="settings">The new settings to use.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _model.UpdateSettings(settings);
    }

    /// <summary>
    /// Gets the visual control for a node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>The node's visual control, or null if not found.</returns>
    public Control? GetNodeVisual(string nodeId)
    {
        return _nodeVisuals.TryGetValue(nodeId, out var control) ? control : null;
    }

    /// <summary>
    /// Gets the visual control for a port.
    /// </summary>
    /// <param name="nodeId">The parent node ID.</param>
    /// <param name="portId">The port ID.</param>
    /// <returns>The port's visual control, or null if not found.</returns>
    public Control? GetPortVisual(string nodeId, string portId)
    {
        return _portVisuals.TryGetValue((nodeId, portId), out var visual) ? visual : null;
    }

    /// <summary>
    /// Clears all tracked node and port visuals.
    /// Note: This does not remove them from the canvas.
    /// </summary>
    public void Clear()
    {
        _nodeVisuals.Clear();
        _portVisuals.Clear();
    }

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
        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);

        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = scale
        };

        // Create the node visual using the renderer
        var control = renderer.CreateNodeVisual(node, context);
        control.Tag = node;

        // Transform position to screen coordinates
        var screenPos = _renderContext.CanvasToScreen(node.Position.X, node.Position.Y);

        if (node.Type == "sequence-message")
        {
            var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
            FlowGraphLogger.Debug(LogCategory.CustomRenderers,
                $"Rendering '{node.Label}': canvas=({node.Position.X:F1},{node.Position.Y:F1}), screen=({screenPos.X:F1},{screenPos.Y:F1}), canvasSize=({node.Width}x{node.Height}), visualSize=({nodeWidth:F1}x{nodeHeight:F1}), scale={_renderContext.Scale:F2}",
                "NodeVisualManager.RenderNode");
        }

        Canvas.SetLeft(control, screenPos.X);
        Canvas.SetTop(control, screenPos.Y);

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
        var renderer = _portRendererRegistry.GetRenderer(port);

        var context = new PortRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = scale,
            IsOutput = isOutput,
            Index = index,
            TotalPorts = totalPorts
        };

        // Get port size from renderer or use default
        var portSize = renderer.GetSize(port, node, _renderContext.Settings) ?? _renderContext.Settings.PortSize;
        var scaledPortSize = portSize * scale;

        // Use GraphRenderModel for port position calculation
        var canvasPos = _model.GetPortPositionByIndex(node, index, totalPorts, isOutput);
        var screenPos = _renderContext.CanvasToScreen(canvasPos.X, canvasPos.Y);

        // Create the port visual using the renderer
        var portVisual = renderer.CreatePortVisual(port, node, context);

        Canvas.SetLeft(portVisual, screenPos.X - scaledPortSize / 2);
        Canvas.SetTop(portVisual, screenPos.Y - scaledPortSize / 2);

        canvas.Children.Add(portVisual);
        _portVisuals[(node.Id, port.Id)] = portVisual;

        onPortCreated?.Invoke(portVisual, node, port, isOutput);

        return portVisual;
    }

    /// <summary>
    /// Updates the position of a node visual on the canvas.
    /// </summary>
    /// <param name="node">The node whose position changed.</param>
    public void UpdateNodePosition(Node node)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var screenPos = _renderContext.CanvasToScreen(node.Position.X, node.Position.Y);
            Canvas.SetLeft(control, screenPos.X);
            Canvas.SetTop(control, screenPos.Y);
        }

        UpdatePortPositions(node);
    }

    /// <summary>
    /// Updates all existing node visuals to their current screen positions.
    /// This is an optimized path for viewport changes (pan/zoom) that avoids
    /// recreating the visual tree. Only updates positions, not visual properties.
    /// </summary>
    /// <param name="graph">The graph containing the nodes.</param>
    public void UpdateAllNodePositions(Graph graph)
    {
        foreach (var node in graph.Elements.Nodes)
        {
            if (_nodeVisuals.TryGetValue(node.Id, out var control))
            {
                var screenPos = _renderContext.CanvasToScreen(node.Position.X, node.Position.Y);
                Canvas.SetLeft(control, screenPos.X);
                Canvas.SetTop(control, screenPos.Y);
            }
        }
        
        // Update all port positions
        foreach (var node in graph.Elements.Nodes)
        {
            UpdatePortPositions(node);
        }
    }

    /// <summary>
    /// Updates the positions of all ports for a node using GraphRenderModel.
    /// </summary>
    /// <param name="node">The node whose ports need updating.</param>
    public void UpdatePortPositions(Node node)
    {
        var scale = _renderContext.Scale;

        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var renderer = _portRendererRegistry.GetRenderer(port);
                var portSize = renderer.GetSize(port, node, _renderContext.Settings) ?? _renderContext.Settings.PortSize;
                var scaledPortSize = portSize * scale;

                var canvasPos = _model.GetPortPositionByIndex(node, i, node.Inputs.Count, false);
                var screenPos = _renderContext.CanvasToScreen(canvasPos.X, canvasPos.Y);
                Canvas.SetLeft(portVisual, screenPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, screenPos.Y - scaledPortSize / 2);
            }
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var port = node.Outputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var renderer = _portRendererRegistry.GetRenderer(port);
                var portSize = renderer.GetSize(port, node, _renderContext.Settings) ?? _renderContext.Settings.PortSize;
                var scaledPortSize = portSize * scale;

                var canvasPos = _model.GetPortPositionByIndex(node, i, node.Outputs.Count, true);
                var screenPos = _renderContext.CanvasToScreen(canvasPos.X, canvasPos.Y);
                Canvas.SetLeft(portVisual, screenPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, screenPos.Y - scaledPortSize / 2);
            }
        }
    }

    /// <summary>
    /// Updates the selection visual state of a node.
    /// </summary>
    /// <param name="node">The node to update.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void UpdateNodeSelection(Node node, ThemeResources theme)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var scale = _renderContext.Scale;
            var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
            var context = new NodeRenderContext
            {
                Theme = theme,
                Settings = _renderContext.Settings,
                Scale = scale
            };

            renderer.UpdateSelection(control, node, context);
        }
    }

    /// <summary>
    /// Updates the size of a node visual.
    /// </summary>
    /// <param name="node">The node to update.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void UpdateNodeSize(Node node, ThemeResources theme)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var scale = _renderContext.Scale;
            var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
            var context = new NodeRenderContext
            {
                Theme = theme,
                Settings = _renderContext.Settings,
                Scale = scale
            };

            var bounds = _model.GetNodeBounds(node);
            renderer.UpdateSize(control, node, context, bounds.Width, bounds.Height);
        }
    }

    #region Inline Editing

    /// <summary>
    /// Begins inline editing for a node's label.
    /// </summary>
    /// <param name="node">The node to edit.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onCommit">Callback when editing is committed with the new label value.</param>
    /// <param name="onCancel">Callback when editing is cancelled.</param>
    /// <returns>True if edit mode was started successfully.</returns>
    public bool BeginEditLabel(Node node, ThemeResources theme, Action<string> onCommit, Action onCancel)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var control))
            return false;

        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        if (renderer is not IEditableNodeRenderer editableRenderer)
            return false;

        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = _renderContext.Scale
        };

        editableRenderer.BeginEdit(control, node, context, onCommit, onCancel);
        return true;
    }

    /// <summary>
    /// Ends inline editing for a node's label.
    /// </summary>
    /// <param name="node">The node being edited.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void EndEditLabel(Node node, ThemeResources theme)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var control))
            return;

        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        if (renderer is not IEditableNodeRenderer editableRenderer)
            return;

        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _renderContext.Settings,
            Scale = _renderContext.Scale
        };

        editableRenderer.EndEdit(control, node, context);
    }

    /// <summary>
    /// Gets whether a node is currently in edit mode.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is being edited.</returns>
    public bool IsEditingLabel(Node node)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var control))
            return false;

        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        if (renderer is not IEditableNodeRenderer editableRenderer)
            return false;

        return editableRenderer.IsEditing(control);
    }

    /// <summary>
    /// Checks if a node's renderer supports inline editing.
    /// </summary>
    /// <param name="nodeType">The node type to check.</param>
    /// <returns>True if the renderer supports editing.</returns>
    public bool SupportsEditing(string? nodeType)
    {
        var renderer = _nodeRendererRegistry.GetRenderer(nodeType);
        return renderer is IEditableNodeRenderer;
    }

    #endregion

    /// <summary>
    /// Gets the screen position of a port using GraphRenderModel.
    /// </summary>
    /// <param name="node">The parent node.</param>
    /// <param name="port">The port.</param>
    /// <param name="isOutput">True if this is an output port.</param>
    /// <returns>The port position in screen coordinates.</returns>
    public AvaloniaPoint GetPortPosition(Node node, Port port, bool isOutput)
    {
        var canvasPos = _model.GetPortPosition(node, port, isOutput);
        return _renderContext.CanvasToScreen(canvasPos.X, canvasPos.Y);
    }

    /// <summary>
    /// Gets the dimensions for a node using GraphRenderModel.
    /// </summary>
    /// <param name="node">The node to get dimensions for.</param>
    /// <returns>The width and height of the node.</returns>
    public (double width, double height) GetNodeDimensions(Node node)
    {
        var bounds = _model.GetNodeBounds(node);
        return (bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Calculates the Y position for a port in canvas coordinates using GraphRenderModel.
    /// </summary>
    /// <param name="nodeY">Y position of the node.</param>
    /// <param name="portIndex">Index of the port.</param>
    /// <param name="totalPorts">Total number of ports.</param>
    /// <param name="nodeHeight">Optional node height override.</param>
    /// <returns>The Y position for the port.</returns>
    public double GetPortYCanvas(double nodeY, int portIndex, int totalPorts, double? nodeHeight = null)
    {
        var height = nodeHeight ?? _renderContext.Settings.NodeHeight;

        if (totalPorts == 1)
        {
            return nodeY + height / 2;
        }

        var spacing = height / (totalPorts + 1);
        return nodeY + spacing * (portIndex + 1);
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
        return GraphRenderModel.IsNodeVisible(graph, node);
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
