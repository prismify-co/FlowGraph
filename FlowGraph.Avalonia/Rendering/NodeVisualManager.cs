using Avalonia.Controls;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Avalonia.Rendering.PortRenderers;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Manages rendering and tracking of node and port visuals.
/// Responsible for creating, updating, and removing node/port UI elements.
/// Uses GraphRenderModel for all geometry calculations to ensure visual parity with DirectCanvasRenderer.
/// </summary>
/// <remarks>
/// This class is split across multiple files for organization:
/// <list type="bullet">
/// <item><see cref="NodeVisualManager"/> - Core fields, constructor, basic accessors</item>
/// <item><c>NodeVisualManager.Ports.cs</c> - Port state management and animations</item>
/// <item><c>NodeVisualManager.Rendering.cs</c> - Node and port rendering methods</item>
/// <item><c>NodeVisualManager.Positioning.cs</c> - Position updates and coordinate calculations</item>
/// <item><c>NodeVisualManager.Editing.cs</c> - Inline label editing</item>
/// </list>
/// </remarks>
public partial class NodeVisualManager
{
    private readonly RenderContext _renderContext;
    private readonly NodeRendererRegistry _nodeRendererRegistry;
    private readonly PortRendererRegistry _portRendererRegistry;
    private readonly CanvasRenderModel _model;

    // Visual tracking
    private readonly Dictionary<string, Control> _nodeVisuals = new();
    private readonly Dictionary<(string nodeId, string portId), Control> _portVisuals = new();

    // State tracking for detecting changes (enables one-shot animations)
    private readonly Dictionary<(string nodeId, string portId), PortVisualState> _previousPortStates = new();

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
        _model = new CanvasRenderModel(renderContext.Settings, _nodeRendererRegistry);
    }

    #region Public Properties

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
    public CanvasRenderModel Model => _model;

    #endregion

    #region Settings and Visual Accessors

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
    /// Checks if a node visual exists in tracking.
    /// </summary>
    /// <param name="nodeId">The node ID to check.</param>
    /// <returns>True if the node visual exists.</returns>
    public bool HasNodeVisual(string nodeId) => _nodeVisuals.ContainsKey(nodeId);

    #endregion

    #region Visual Management

    /// <summary>
    /// Clears all tracked node and port visuals.
    /// Note: This does not remove them from the canvas.
    /// </summary>
    public void Clear()
    {
        _nodeVisuals.Clear();
        _portVisuals.Clear();
        _previousPortStates.Clear();
    }

    /// <summary>
    /// Removes a node visual and its port visuals from the canvas and tracking.
    /// </summary>
    /// <param name="canvas">The canvas containing the visual.</param>
    /// <param name="node">The node to remove.</param>
    /// <returns>True if the visual was found and removed.</returns>
    public bool RemoveNodeVisual(Canvas canvas, Node node)
    {
        if (!_nodeVisuals.TryGetValue(node.Id, out var visual))
            return false;

        // Remove the node visual from canvas
        canvas.Children.Remove(visual);
        _nodeVisuals.Remove(node.Id);

        // Remove port visuals
        foreach (var port in node.Inputs)
        {
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                canvas.Children.Remove(portVisual);
                _portVisuals.Remove((node.Id, port.Id));
            }
        }
        foreach (var port in node.Outputs)
        {
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                canvas.Children.Remove(portVisual);
                _portVisuals.Remove((node.Id, port.Id));
            }
        }

        return true;
    }

    #endregion
}
