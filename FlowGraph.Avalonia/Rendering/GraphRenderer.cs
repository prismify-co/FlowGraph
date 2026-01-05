using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Coordinates rendering of all graph elements (nodes, ports, edges, handles).
/// Acts as a facade over specialized visual managers for clean separation of concerns.
/// </summary>
public class GraphRenderer
{
    private readonly RenderContext _renderContext;
    private readonly NodeVisualManager _nodeVisualManager;
    private readonly EdgeVisualManager _edgeVisualManager;
    private readonly ResizeHandleManager _resizeHandleManager;

    /// <summary>
    /// Creates a new graph renderer with default settings.
    /// </summary>
    public GraphRenderer()
        : this(null, null)
    {
    }

    /// <summary>
    /// Creates a new graph renderer with the specified settings.
    /// </summary>
    /// <param name="settings">Canvas settings. If null, default settings are used.</param>
    public GraphRenderer(FlowCanvasSettings? settings)
        : this(settings, null)
    {
    }

    /// <summary>
    /// Creates a new graph renderer with the specified settings and node renderer registry.
    /// </summary>
    /// <param name="settings">Canvas settings. If null, default settings are used.</param>
    /// <param name="nodeRendererRegistry">Registry for custom node renderers. If null, a default registry is created.</param>
    public GraphRenderer(FlowCanvasSettings? settings, NodeRendererRegistry? nodeRendererRegistry)
    {
        _renderContext = new RenderContext(settings);
        _nodeVisualManager = new NodeVisualManager(_renderContext, nodeRendererRegistry);
        _edgeVisualManager = new EdgeVisualManager(_renderContext, _nodeVisualManager);
        _resizeHandleManager = new ResizeHandleManager(_renderContext, _nodeVisualManager);
    }

    #region Public Properties

    /// <summary>
    /// Gets the node renderer registry for registering custom node types.
    /// </summary>
    public NodeRendererRegistry NodeRenderers => _nodeVisualManager.NodeRenderers;

    /// <summary>
    /// Gets the render context for coordinate transformations.
    /// </summary>
    public RenderContext RenderContext => _renderContext;

    /// <summary>
    /// Gets the node visual manager.
    /// </summary>
    public NodeVisualManager Nodes => _nodeVisualManager;

    /// <summary>
    /// Gets the edge visual manager.
    /// </summary>
    public EdgeVisualManager Edges => _edgeVisualManager;

    /// <summary>
    /// Gets the resize handle manager.
    /// </summary>
    public ResizeHandleManager ResizeHandles => _resizeHandleManager;

    #endregion

    #region Viewport Management

    /// <summary>
    /// Sets the viewport state to use for coordinate transformations.
    /// </summary>
    /// <param name="viewport">The viewport state.</param>
    public void SetViewport(ViewportState? viewport)
    {
        _renderContext.SetViewport(viewport);
    }

    #endregion

    #region Visual Retrieval (Delegated)

    /// <summary>
    /// Gets the visual element for a node.
    /// </summary>
    public Control? GetNodeVisual(string nodeId) => _nodeVisualManager.GetNodeVisual(nodeId);

    /// <summary>
    /// Gets the visual element for a port.
    /// </summary>
    public Ellipse? GetPortVisual(string nodeId, string portId) => _nodeVisualManager.GetPortVisual(nodeId, portId);

    /// <summary>
    /// Gets the visual element for an edge (hit area path).
    /// </summary>
    public AvaloniaPath? GetEdgeVisual(string edgeId) => _edgeVisualManager.GetEdgeVisual(edgeId);

    /// <summary>
    /// Gets the visible path for an edge (the actual rendered stroke).
    /// </summary>
    public AvaloniaPath? GetEdgeVisiblePath(string edgeId) => _edgeVisualManager.GetEdgeVisiblePath(edgeId);

    /// <summary>
    /// Gets the markers (arrows) for an edge.
    /// </summary>
    public IReadOnlyList<AvaloniaPath>? GetEdgeMarkers(string edgeId) => _edgeVisualManager.GetEdgeMarkers(edgeId);

    /// <summary>
    /// Gets the edge label for an edge.
    /// </summary>
    public TextBlock? GetEdgeLabel(string edgeId) => _edgeVisualManager.GetEdgeLabel(edgeId);

    /// <summary>
    /// Gets the endpoint handles for an edge.
    /// </summary>
    public (Ellipse? source, Ellipse? target) GetEdgeEndpointHandles(string edgeId) => _edgeVisualManager.GetEdgeEndpointHandles(edgeId);

    #endregion

    #region Clear

    /// <summary>
    /// Clears all rendered visuals from tracking.
    /// </summary>
    public void Clear()
    {
        _nodeVisualManager.Clear();
        _edgeVisualManager.Clear();
        _resizeHandleManager.Clear();
    }

    #endregion

    #region Node Rendering (Delegated)

    /// <summary>
    /// Renders all nodes in the graph.
    /// Groups are rendered first (behind), then regular nodes.
    /// Nodes hidden by collapsed groups are not rendered.
    /// </summary>
    public void RenderNodes(
        Canvas canvas,
        Graph graph,
        ThemeResources theme,
        Action<Control, Node> onNodeCreated)
    {
        _nodeVisualManager.RenderNodes(canvas, graph, theme, onNodeCreated);
    }

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed ancestor group).
    /// </summary>
    public bool IsNodeVisible(Graph graph, Node node) => NodeVisualManager.IsNodeVisible(graph, node);

    /// <summary>
    /// Renders a single node using the appropriate renderer for its type.
    /// </summary>
    public Control RenderNode(
        Canvas canvas,
        Node node,
        ThemeResources theme,
        Action<Control, Node>? onNodeCreated = null)
    {
        return _nodeVisualManager.RenderNode(canvas, node, theme, onNodeCreated);
    }

    /// <summary>
    /// Renders a single port.
    /// </summary>
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
        return _nodeVisualManager.RenderPort(canvas, node, port, index, totalPorts, isOutput, theme, onPortCreated);
    }

    /// <summary>
    /// Updates the position of a node visual.
    /// </summary>
    public void UpdateNodePosition(Node node) => _nodeVisualManager.UpdateNodePosition(node);

    /// <summary>
    /// Updates port positions for a node.
    /// </summary>
    public void UpdatePortPositions(Node node) => _nodeVisualManager.UpdatePortPositions(node);

    /// <summary>
    /// Updates the selection visual of a node.
    /// </summary>
    public void UpdateNodeSelection(Node node, ThemeResources theme) => _nodeVisualManager.UpdateNodeSelection(node, theme);

    /// <summary>
    /// Updates the size of a node visual.
    /// </summary>
    public void UpdateNodeSize(Node node, ThemeResources theme) => _nodeVisualManager.UpdateNodeSize(node, theme);

    /// <summary>
    /// Gets the dimensions for a node, considering custom renderer sizes and node-specific overrides.
    /// </summary>
    public (double width, double height) GetNodeDimensions(Node node) => _nodeVisualManager.GetNodeDimensions(node);

    /// <summary>
    /// Gets the source point for a connection from a node/port in screen coordinates.
    /// </summary>
    public AvaloniaPoint GetPortPosition(Node node, Port port, bool isOutput) => _nodeVisualManager.GetPortPosition(node, port, isOutput);

    /// <summary>
    /// Calculates the Y position for a port in canvas coordinates.
    /// </summary>
    public double GetPortYCanvas(double nodeY, int portIndex, int totalPorts, double? nodeHeight = null)
        => _nodeVisualManager.GetPortYCanvas(nodeY, portIndex, totalPorts, nodeHeight);

    /// <summary>
    /// Calculates the Y position for a port (legacy, returns canvas coordinates).
    /// </summary>
    public double GetPortY(double nodeY, int portIndex, int totalPorts)
        => _nodeVisualManager.GetPortYCanvas(nodeY, portIndex, totalPorts);

    #endregion

    #region Edge Rendering (Delegated)

    /// <summary>
    /// Renders all edges in the graph.
    /// Edges with hidden endpoints (due to collapsed groups) are not rendered.
    /// </summary>
    public void RenderEdges(Canvas canvas, Graph graph, ThemeResources theme, AvaloniaPath? excludePath = null)
    {
        _edgeVisualManager.RenderEdges(canvas, graph, theme, excludePath);
    }

    /// <summary>
    /// Renders a single edge with its markers and optional label.
    /// </summary>
    public AvaloniaPath? RenderEdge(Canvas canvas, Edge edge, Graph graph, ThemeResources theme)
    {
        return _edgeVisualManager.RenderEdge(canvas, edge, graph, theme);
    }

    /// <summary>
    /// Updates the selection visual of an edge.
    /// </summary>
    public void UpdateEdgeSelection(Edge edge, ThemeResources theme) => _edgeVisualManager.UpdateEdgeSelection(edge, theme);

    #endregion

    #region Resize Handle Rendering (Delegated)

    /// <summary>
    /// Renders resize handles for a selected node.
    /// </summary>
    public void RenderResizeHandles(
        Canvas canvas,
        Node node,
        ThemeResources theme,
        Action<Rectangle, Node, ResizeHandlePosition>? onHandleCreated = null)
    {
        _resizeHandleManager.RenderResizeHandles(canvas, node, theme, onHandleCreated);
    }

    /// <summary>
    /// Removes resize handles for a node.
    /// </summary>
    public void RemoveResizeHandles(Canvas canvas, string nodeId) => _resizeHandleManager.RemoveResizeHandles(canvas, nodeId);

    /// <summary>
    /// Removes all resize handles from the canvas.
    /// </summary>
    public void RemoveAllResizeHandles(Canvas canvas) => _resizeHandleManager.RemoveAllResizeHandles(canvas);

    /// <summary>
    /// Updates the position of resize handles for a node.
    /// </summary>
    public void UpdateResizeHandlePositions(Node node) => _resizeHandleManager.UpdateResizeHandlePositions(node);

    #endregion
}
