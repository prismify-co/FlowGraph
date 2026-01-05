using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Manages rendering and tracking of node and port visuals.
/// Responsible for creating, updating, and removing node/port UI elements.
/// </summary>
public class NodeVisualManager
{
    private readonly RenderContext _renderContext;
    private readonly NodeRendererRegistry _nodeRendererRegistry;
    
    // Visual tracking
    private readonly Dictionary<string, Control> _nodeVisuals = new();
    private readonly Dictionary<(string nodeId, string portId), Ellipse> _portVisuals = new();

    /// <summary>
    /// Creates a new node visual manager.
    /// </summary>
    /// <param name="renderContext">Shared render context.</param>
    /// <param name="nodeRendererRegistry">Registry for custom node renderers. If null, a default registry is created.</param>
    public NodeVisualManager(RenderContext renderContext, NodeRendererRegistry? nodeRendererRegistry = null)
    {
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
        _nodeRendererRegistry = nodeRendererRegistry ?? new NodeRendererRegistry();
    }

    /// <summary>
    /// Gets the node renderer registry for registering custom node types.
    /// </summary>
    public NodeRendererRegistry NodeRenderers => _nodeRendererRegistry;

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
    /// Gets the visual ellipse for a port.
    /// </summary>
    /// <param name="nodeId">The parent node ID.</param>
    /// <param name="portId">The port ID.</param>
    /// <returns>The port's visual ellipse, or null if not found.</returns>
    public Ellipse? GetPortVisual(string nodeId, string portId)
    {
        return _portVisuals.TryGetValue((nodeId, portId), out var ellipse) ? ellipse : null;
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
        // Render groups first (they should be behind their children)
        // Order by hierarchy depth - outermost groups first
        var groups = graph.Nodes
            .Where(n => n.IsGroup && IsNodeVisible(graph, n) && IsInVisibleBounds(n))
            .OrderBy(n => GetGroupDepth(graph, n))
            .ToList();

        foreach (var group in groups)
        {
            RenderNode(canvas, group, theme, onNodeCreated);
        }

        // Then render non-group nodes that are visible
        foreach (var node in graph.Nodes.Where(n => !n.IsGroup && IsNodeVisible(graph, n) && IsInVisibleBounds(n)))
        {
            RenderNode(canvas, node, theme, onNodeCreated);
        }
    }

    /// <summary>
    /// Checks if a node is within the visible viewport bounds (with buffer for virtualization).
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is in visible bounds or virtualization is disabled.</returns>
    private bool IsInVisibleBounds(Node node)
    {
        var (width, height) = GetNodeDimensions(node);
        return _renderContext.IsInVisibleBounds(node.Position.X, node.Position.Y, width, height);
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
        Canvas.SetLeft(control, screenPos.X);
        Canvas.SetTop(control, screenPos.Y);

        canvas.Children.Add(control);
        _nodeVisuals[node.Id] = control;

        onNodeCreated?.Invoke(control, node);

        // Render ports
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            RenderPort(canvas, node, node.Inputs[i], i, node.Inputs.Count, false, theme);
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            RenderPort(canvas, node, node.Outputs[i], i, node.Outputs.Count, true, theme);
        }

        return control;
    }

    /// <summary>
    /// Renders a single port.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="node">The parent node.</param>
    /// <param name="port">The port to render.</param>
    /// <param name="index">Index of this port among siblings.</param>
    /// <param name="totalPorts">Total number of sibling ports.</param>
    /// <param name="isOutput">True if this is an output port.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onPortCreated">Optional callback when the port visual is created.</param>
    /// <returns>The created port visual ellipse.</returns>
    public Ellipse RenderPort(
        Canvas canvas,
        Node node,
        Port port,
        int index,
        int totalPorts,
        bool isOutput,
        ThemeResources theme,
        Action<Ellipse, Node, Port, bool>? onPortCreated = null)
    {
        var scale = _renderContext.Scale;
        var scaledPortSize = _renderContext.Settings.PortSize * scale;

        // Get the node dimensions
        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);

        // Determine port position - use explicit position or default based on input/output
        var position = port.Position ?? (isOutput ? PortPosition.Right : PortPosition.Left);

        // Calculate port position in canvas coordinates
        var (portX, portY) = CalculatePortCanvasPosition(
            node.Position.X, node.Position.Y,
            nodeWidth, nodeHeight,
            position, index, totalPorts);

        // Transform to screen coordinates
        var screenPos = _renderContext.CanvasToScreen(portX, portY);

        var portVisual = new Ellipse
        {
            Width = scaledPortSize,
            Height = scaledPortSize,
            Fill = theme.PortBackground,
            Stroke = theme.PortBorder,
            StrokeThickness = 2,
            Cursor = new Cursor(StandardCursorType.Cross),
            Tag = (node, port, isOutput)
        };

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
    /// Updates the positions of all ports for a node.
    /// </summary>
    /// <param name="node">The node whose ports need updating.</param>
    public void UpdatePortPositions(Node node)
    {
        var scale = _renderContext.Scale;
        var scaledPortSize = _renderContext.Settings.PortSize * scale;
        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);

        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var position = port.Position ?? PortPosition.Left;
                var (portX, portY) = CalculatePortCanvasPosition(
                    node.Position.X, node.Position.Y,
                    nodeWidth, nodeHeight,
                    position, i, node.Inputs.Count);
                var screenPos = _renderContext.CanvasToScreen(portX, portY);
                Canvas.SetLeft(portVisual, screenPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, screenPos.Y - scaledPortSize / 2);
            }
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var port = node.Outputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var position = port.Position ?? PortPosition.Right;
                var (portX, portY) = CalculatePortCanvasPosition(
                    node.Position.X, node.Position.Y,
                    nodeWidth, nodeHeight,
                    position, i, node.Outputs.Count);
                var screenPos = _renderContext.CanvasToScreen(portX, portY);
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

            var (width, height) = GetNodeDimensions(node);
            renderer.UpdateSize(control, node, context, width, height);
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
    /// Gets the screen position of a port.
    /// </summary>
    /// <param name="node">The parent node.</param>
    /// <param name="port">The port.</param>
    /// <param name="isOutput">True if this is an output port.</param>
    /// <returns>The port position in screen coordinates.</returns>
    public AvaloniaPoint GetPortPosition(Node node, Port port, bool isOutput)
    {
        var portIndex = isOutput
            ? node.Outputs.IndexOf(port)
            : node.Inputs.IndexOf(port);
        var totalPorts = isOutput ? node.Outputs.Count : node.Inputs.Count;

        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);

        // Determine port position - use explicit position or default based on input/output
        var position = port.Position ?? (isOutput ? PortPosition.Right : PortPosition.Left);

        var (portX, portY) = CalculatePortCanvasPosition(
            node.Position.X, node.Position.Y,
            nodeWidth, nodeHeight,
            position, portIndex, totalPorts);

        return _renderContext.CanvasToScreen(portX, portY);
    }

    /// <summary>
    /// Gets the dimensions for a node, considering custom renderer sizes and node-specific overrides.
    /// </summary>
    /// <param name="node">The node to get dimensions for.</param>
    /// <returns>The width and height of the node.</returns>
    public (double width, double height) GetNodeDimensions(Node node)
    {
        // First check if the node has explicit dimensions
        if (node.Width.HasValue && node.Height.HasValue)
        {
            return (node.Width.Value, node.Height.Value);
        }

        // Fall back to renderer-specified or default dimensions
        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        var width = node.Width ?? renderer.GetWidth(node, _renderContext.Settings) ?? _renderContext.Settings.NodeWidth;
        var height = node.Height ?? renderer.GetHeight(node, _renderContext.Settings) ?? _renderContext.Settings.NodeHeight;
        return (width, height);
    }

    /// <summary>
    /// Calculates the Y position for a port in canvas coordinates.
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
    /// </summary>
    /// <param name="graph">The graph containing the node.</param>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is visible.</returns>
    public static bool IsNodeVisible(Graph graph, Node node)
    {
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            var parent = graph.Nodes.FirstOrDefault(n => n.Id == currentParentId);
            if (parent == null) break;

            if (parent.IsCollapsed)
                return false;

            currentParentId = parent.ParentGroupId;
        }
        return true;
    }

    /// <summary>
    /// Gets the nesting depth of a group (0 = top level).
    /// </summary>
    private static int GetGroupDepth(Graph graph, Node node)
    {
        int depth = 0;
        var current = node;
        while (!string.IsNullOrEmpty(current.ParentGroupId))
        {
            depth++;
            current = graph.Nodes.FirstOrDefault(n => n.Id == current.ParentGroupId);
            if (current == null) break;
        }
        return depth;
    }

    /// <summary>
    /// Calculates port canvas position based on port position.
    /// </summary>
    private (double x, double y) CalculatePortCanvasPosition(
        double nodeX, double nodeY,
        double nodeWidth, double nodeHeight,
        PortPosition position, int portIndex, int totalPorts)
    {
        return position switch
        {
            PortPosition.Left => (nodeX, GetPortAlongEdge(nodeY, nodeHeight, portIndex, totalPorts)),
            PortPosition.Right => (nodeX + nodeWidth, GetPortAlongEdge(nodeY, nodeHeight, portIndex, totalPorts)),
            PortPosition.Top => (GetPortAlongEdge(nodeX, nodeWidth, portIndex, totalPorts), nodeY),
            PortPosition.Bottom => (GetPortAlongEdge(nodeX, nodeWidth, portIndex, totalPorts), nodeY + nodeHeight),
            _ => (nodeX, nodeY + nodeHeight / 2)
        };
    }

    /// <summary>
    /// Calculates port position along an edge (distributes ports evenly).
    /// </summary>
    private static double GetPortAlongEdge(double edgeStart, double edgeLength, int portIndex, int totalPorts)
    {
        if (totalPorts == 1)
        {
            return edgeStart + edgeLength / 2;
        }

        var spacing = edgeLength / (totalPorts + 1);
        return edgeStart + spacing * (portIndex + 1);
    }
}
