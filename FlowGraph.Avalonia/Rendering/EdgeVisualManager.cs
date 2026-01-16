using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Manages rendering and tracking of edge visuals including paths, markers, and labels.
/// Responsible for creating, updating, and removing edge UI elements.
/// </summary>
public partial class EdgeVisualManager
{
    private readonly RenderContext _renderContext;
    private readonly NodeVisualManager _nodeVisualManager;
    private readonly EdgeRenderers.EdgeRendererRegistry? _edgeRendererRegistry;

    // Custom render results tracked separately
    private readonly Dictionary<string, EdgeRenderers.EdgeRenderResult> _customRenderResults = new();

    // Visual tracking
    private readonly Dictionary<string, AvaloniaPath> _edgeVisuals = new();  // Hit area paths
    private readonly Dictionary<string, AvaloniaPath> _edgeVisiblePaths = new();  // Visible paths
    private readonly Dictionary<string, AvaloniaPath> _edgeGlowPaths = new();  // Glow effect paths (background)
    private readonly Dictionary<string, List<AvaloniaPath>> _edgeMarkers = new();  // Edge markers (arrows)
    private readonly Dictionary<string, TextBlock> _edgeLabels = new();  // Edge labels
    private readonly Dictionary<string, (Ellipse source, Ellipse target)> _edgeEndpointHandles = new();  // Edge endpoint handles

    /// <summary>
    /// Creates a new edge visual manager.
    /// </summary>
    /// <param name="renderContext">Shared render context.</param>
    /// <param name="nodeVisualManager">Node visual manager for port position calculations.</param>
    /// <param name="edgeRendererRegistry">Optional registry for custom edge renderers.</param>
    public EdgeVisualManager(RenderContext renderContext, NodeVisualManager nodeVisualManager, EdgeRenderers.EdgeRendererRegistry? edgeRendererRegistry = null)
    {
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
        _nodeVisualManager = nodeVisualManager ?? throw new ArgumentNullException(nameof(nodeVisualManager));
        _edgeRendererRegistry = edgeRendererRegistry;
    }

    /// <summary>
    /// Gets the hit area path for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's hit area path, or null if not found.</returns>
    public AvaloniaPath? GetEdgeVisual(string edgeId)
    {
        return _edgeVisuals.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Gets the visible path for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's visible path, or null if not found.</returns>
    public AvaloniaPath? GetEdgeVisiblePath(string edgeId)
    {
        return _edgeVisiblePaths.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Gets the glow path for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's glow path, or null if not found.</returns>
    public AvaloniaPath? GetEdgeGlowPath(string edgeId)
    {
        return _edgeGlowPaths.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Gets the markers (arrows) for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's markers, or null if not found.</returns>
    public IReadOnlyList<AvaloniaPath>? GetEdgeMarkers(string edgeId)
    {
        return _edgeMarkers.TryGetValue(edgeId, out var markers) ? markers : null;
    }

    /// <summary>
    /// Gets the label for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>The edge's label, or null if not found.</returns>
    public TextBlock? GetEdgeLabel(string edgeId)
    {
        return _edgeLabels.TryGetValue(edgeId, out var label) ? label : null;
    }

    /// <summary>
    /// Gets the endpoint handles for an edge.
    /// </summary>
    /// <param name="edgeId">The edge ID.</param>
    /// <returns>Tuple of source and target endpoint handles.</returns>
    public (Ellipse? source, Ellipse? target) GetEdgeEndpointHandles(string edgeId)
    {
        return _edgeEndpointHandles.TryGetValue(edgeId, out var handles) ? handles : (null, null);
    }

    /// <summary>
    /// Clears all tracked edge visuals.
    /// Note: This does not remove them from the canvas.
    /// </summary>
    public void Clear()
    {
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
        _edgeMarkers.Clear();
        _edgeLabels.Clear();
        _edgeEndpointHandles.Clear();
        _customRenderResults.Clear();
    }

    /// <summary>
    /// Removes an edge visual and all its components from the canvas and tracking.
    /// </summary>
    /// <param name="canvas">The canvas containing the visual.</param>
    /// <param name="edge">The edge to remove.</param>
    /// <returns>True if the visual was found and removed.</returns>
    public bool RemoveEdgeVisual(Canvas canvas, Edge edge)
    {
        bool removed = false;

        // Remove hit area path
        if (_edgeVisuals.TryGetValue(edge.Id, out var hitPath))
        {
            canvas.Children.Remove(hitPath);
            _edgeVisuals.Remove(edge.Id);
            removed = true;
        }

        // Remove visible path
        if (_edgeVisiblePaths.TryGetValue(edge.Id, out var visiblePath))
        {
            canvas.Children.Remove(visiblePath);
            _edgeVisiblePaths.Remove(edge.Id);
            removed = true;
        }

        // Remove markers (arrows)
        if (_edgeMarkers.TryGetValue(edge.Id, out var markers))
        {
            foreach (var marker in markers)
            {
                canvas.Children.Remove(marker);
            }
            _edgeMarkers.Remove(edge.Id);
            removed = true;
        }

        // Remove label
        if (_edgeLabels.TryGetValue(edge.Id, out var label))
        {
            canvas.Children.Remove(label);
            _edgeLabels.Remove(edge.Id);
            removed = true;
        }

        // Remove endpoint handles
        if (_edgeEndpointHandles.TryGetValue(edge.Id, out var handles))
        {
            canvas.Children.Remove(handles.source);
            canvas.Children.Remove(handles.target);
            _edgeEndpointHandles.Remove(edge.Id);
            removed = true;
        }

        // Remove custom render results
        _customRenderResults.Remove(edge.Id);

        return removed;
    }

    /// <summary>
    /// Checks if an edge visual exists in tracking.
    /// </summary>
    /// <param name="edgeId">The edge ID to check.</param>
    /// <returns>True if the edge visual exists.</returns>
    public bool HasEdgeVisual(string edgeId) => _edgeVisuals.ContainsKey(edgeId);

    // Edge rendering methods (RenderEdges, RenderEdge, RenderCustomEdge, UpdateEdgeSelection)
    // are in EdgeVisualManager.Rendering.cs
    // Marker rendering methods are in EdgeVisualManager.Markers.cs
    // Label rendering methods are in EdgeVisualManager.Labels.cs

    /// <summary>
    /// Calculates the canvas position for a port based on its Position property.
    /// </summary>
    /// <param name="node">The node containing the port.</param>
    /// <param name="port">The port (can be null for default behavior).</param>
    /// <param name="nodeWidth">Width of the node.</param>
    /// <param name="nodeHeight">Height of the node.</param>
    /// <param name="isOutput">True if this is an output port, false for input.</param>
    /// <returns>Canvas coordinates (X, Y) for the port connection point.</returns>
    private (double X, double Y) GetPortCanvasPosition(
        Node node, Port? port, double nodeWidth, double nodeHeight, bool isOutput)
    {
        var nodeX = node.Position.X;
        var nodeY = node.Position.Y;

        var ports = isOutput ? node.Outputs : node.Inputs;
        var position = port?.Position ?? (isOutput ? PortPosition.Right : PortPosition.Left);

        // Distribute ports along the edge like GraphRenderModel does.
        var totalPorts = Math.Max(1, ports.Count);
        var portIndex = 0;
        if (port != null)
        {
            var idx = ports.IndexOf(port);
            if (idx >= 0) portIndex = idx;
        }

        static double Along(double start, double length, int index, int total)
        {
            if (total <= 1) return start + length / 2;
            var spacing = length / (total + 1);
            return start + spacing * (index + 1);
        }

        return position switch
        {
            PortPosition.Right => (nodeX + nodeWidth, Along(nodeY, nodeHeight, portIndex, totalPorts)),
            PortPosition.Left => (nodeX, Along(nodeY, nodeHeight, portIndex, totalPorts)),
            PortPosition.Top => (Along(nodeX, nodeWidth, portIndex, totalPorts), nodeY),
            PortPosition.Bottom => (Along(nodeX, nodeWidth, portIndex, totalPorts), nodeY + nodeHeight),
            _ => isOutput
                ? (nodeX + nodeWidth, Along(nodeY, nodeHeight, portIndex, totalPorts))
                : (nodeX, Along(nodeY, nodeHeight, portIndex, totalPorts))
        };
    }

    /// <summary>
    /// Checks if an edge should be rendered based on whether at least one endpoint is in visible bounds.
    /// </summary>
    private bool IsEdgeInVisibleBounds(Node sourceNode, Node targetNode)
    {
        var (sourceWidth, sourceHeight) = _nodeVisualManager.GetNodeDimensions(sourceNode);
        var (targetWidth, targetHeight) = _nodeVisualManager.GetNodeDimensions(targetNode);

        var sourceInBounds = _renderContext.IsInVisibleBounds(
            sourceNode.Position.X, sourceNode.Position.Y, sourceWidth, sourceHeight);
        var targetInBounds = _renderContext.IsInVisibleBounds(
            targetNode.Position.X, targetNode.Position.Y, targetWidth, targetHeight);

        // Render if either endpoint is visible
        return sourceInBounds || targetInBounds;
    }
}
