using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using FlowGraph.Core.Rendering;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// A high-performance graph renderer that draws directly to a DrawingContext,
/// bypassing the Avalonia visual tree for nodes and edges.
/// This is optimized for large graphs (500+ nodes) where per-element Controls are too slow.
/// 
/// Uses GraphRenderModel for all geometry calculations to ensure 100% visual parity
/// with the normal VisualTree renderer.
/// </summary>
/// <remarks>
/// <para>
/// DirectGraphRenderer supports custom node rendering via the <see cref="IDirectNodeRenderer"/> interface.
/// When a node's renderer implements IDirectNodeRenderer, it delegates drawing to that implementation.
/// </para>
/// <para>
/// Edge endpoint handles are rendered when <see cref="FlowCanvasSettings.ShowEdgeEndpointHandles"/> is true
/// and an edge is selected, allowing reconnection/disconnection.
/// </para>
/// <para>
/// <b>Transform Mode:</b> This renderer uses <see cref="LayerTransformMode.SelfTransformed"/> mode.
/// It performs its own coordinate transforms internally and MUST be placed in an untransformed
/// container (e.g., RootPanel, not MainCanvas) to avoid double-transformation.
/// </para>
/// </remarks>
public partial class DirectGraphRenderer : Control, IRenderLayer
{
    private FlowCanvasSettings _settings;
    private readonly GraphRenderModel _model;
    private NodeRendererRegistry? _nodeRenderers;
    private Graph? _graph;
    private ViewportState? _viewport;
    private ThemeResources? _theme;
    private Dictionary<string, Node>? _nodeById;

    // Cached pens and brushes (reused across renders)
    private Pen? _edgePen;
    private Pen? _edgeSelectedPen;
    private Pen? _nodeBorderPen;
    private Pen? _nodeSelectedPen;
    private IBrush? _nodeBackground;
    private IBrush? _nodeInputBackground;
    private IBrush? _nodeOutputBackground;
    private IBrush? _portBrush;
    private Pen? _portPen;
    private IBrush? _resizeHandleFill;
    private Pen? _resizeHandlePen;
    private IBrush? _endpointHandleFill;
    private Pen? _endpointHandlePen;

    // Typeface for labels
    private readonly Typeface _typeface = new("Segoe UI");

    // Spatial index for fast hit testing
    private List<(Node node, double x, double y, double width, double height)>? _nodeIndex;
    private bool _indexDirty = true;
    private Graph? _lastIndexedGraph; // Track which graph instance was indexed

    // Port hover state tracking
    private (string nodeId, string portId)? _hoveredPort;

    // Edge endpoint handle hover state
    private (string edgeId, bool isSource)? _hoveredEndpointHandle;

    // Inline editing state
    private string? _editingNodeId;
    private string? _editingEdgeId;

    #region IRenderLayer Implementation

    /// <inheritdoc />
    public string LayerId => "direct-renderer";

    /// <inheritdoc />
    public string DisplayName => "Direct Graph Renderer";

    /// <inheritdoc />
    /// <remarks>
    /// Note: This is separate from <see cref="Visual.ZIndex"/> which controls the
    /// Avalonia visual tree ordering. This property is for the <see cref="IRenderLayer"/>
    /// abstraction and represents the logical layer order.
    /// </remarks>
    int IRenderLayer.ZIndex => 200; // Same level as nodes

    /// <inheritdoc />
    /// <remarks>
    /// DirectGraphRenderer uses <see cref="LayerTransformMode.SelfTransformed"/> because it
    /// performs its own coordinate transforms internally via <see cref="ScreenToCanvas"/>.
    /// It MUST be placed outside any transformed container (e.g., as a child of RootPanel,
    /// not MainCanvas) to avoid double-transformation bugs.
    /// </remarks>
    public LayerTransformMode TransformMode => LayerTransformMode.SelfTransformed;

    /// <inheritdoc />
    bool IRenderLayer.IsVisible
    {
        get => IsVisible;
        set => IsVisible = value;
    }

    /// <inheritdoc />
    bool IRenderLayer.IsHitTestVisible
    {
        get => IsHitTestVisible;
        set => IsHitTestVisible = value;
    }

    #endregion

    /// <summary>
    /// Creates a new DirectGraphRenderer with the specified settings.
    /// </summary>
    /// <param name="settings">Canvas settings for rendering configuration.</param>
    public DirectGraphRenderer(FlowCanvasSettings settings)
        : this(settings, null)
    {
    }

    /// <summary>
    /// Creates a new DirectGraphRenderer with the specified settings and node renderer registry.
    /// </summary>
    /// <param name="settings">Canvas settings for rendering configuration.</param>
    /// <param name="nodeRenderers">Optional registry of custom node renderers.</param>
    public DirectGraphRenderer(FlowCanvasSettings settings, NodeRendererRegistry? nodeRenderers)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _nodeRenderers = nodeRenderers;
        _model = new GraphRenderModel(settings, nodeRenderers);
        IsHitTestVisible = false; // Hit testing handled separately
    }

    /// <summary>
    /// Gets the render model used for geometry calculations.
    /// This model is shared with other renderers to ensure visual parity.
    /// </summary>
    public GraphRenderModel Model => _model;

    /// <summary>
    /// Updates the settings and propagates to the render model.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    public void UpdateSettings(FlowCanvasSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _model.UpdateSettings(settings);
        _indexDirty = true; // Force spatial index rebuild with new dimensions
        InvalidateVisual();
    }

    /// <summary>
    /// Gets or sets the node renderer registry for custom node types.
    /// </summary>
    public NodeRendererRegistry? NodeRenderers
    {
        get => _nodeRenderers;
        set
        {
            _nodeRenderers = value;
            _model.NodeRenderers = value;
        }
    }

    /// <summary>
    /// Gets whether inline editing is currently active.
    /// </summary>
    public bool IsEditing => _editingNodeId != null || _editingEdgeId != null;

    /// <summary>
    /// Gets the ID of the node currently being edited, or null if no node is being edited.
    /// </summary>
    public string? EditingNodeId => _editingNodeId;

    /// <summary>
    /// Gets the ID of the edge currently being edited, or null if no edge is being edited.
    /// </summary>
    public string? EditingEdgeId => _editingEdgeId;

    /// <summary>
    /// Updates the renderer with the current graph state and triggers a redraw.
    /// </summary>
    /// <param name="graph">The graph to render.</param>
    /// <param name="viewport">Current viewport state for zoom/pan.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void Update(Graph? graph, ViewportState viewport, ThemeResources theme)
    {
        // Only rebuild spatial index if graph instance changed (not on every viewport/selection change)
        if (_graph != graph)
        {
            _indexDirty = true;
            _lastIndexedGraph = null;

            // Build node lookup dictionary for O(1) edge endpoint resolution (only when graph changes!)
            _nodeById = graph?.Elements.Nodes.ToDictionary(n => n.Id);
        }

        _graph = graph;
        _viewport = viewport;

        if (_theme != theme)
        {
            _theme = theme;
            InvalidateBrushes();
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Marks the spatial index as dirty, forcing rebuild on next hit test.
    /// Call this when nodes are added, removed, or moved.
    /// </summary>
    public void InvalidateIndex()
    {
        _indexDirty = true;
        // Also invalidate the node lookup dictionary since nodes may have been added/removed
        _nodeById = null;
    }

    /// <summary>
    /// Sets the currently hovered port for visual feedback.
    /// </summary>
    /// <param name="nodeId">ID of the node containing the port, or null to clear.</param>
    /// <param name="portId">ID of the port, or null to clear.</param>
    public void SetHoveredPort(string? nodeId, string? portId)
    {
        var newHovered = (nodeId != null && portId != null) ? (nodeId, portId) : ((string, string)?)null;
        if (_hoveredPort != newHovered)
        {
            _hoveredPort = newHovered;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Clears the hovered port state.
    /// </summary>
    public void ClearHoveredPort()
    {
        if (_hoveredPort != null)
        {
            _hoveredPort = null;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Sets the currently hovered edge endpoint handle for visual feedback.
    /// </summary>
    /// <param name="edgeId">ID of the edge, or null to clear.</param>
    /// <param name="isSource">True if hovering the source handle, false for target.</param>
    public void SetHoveredEndpointHandle(string? edgeId, bool isSource)
    {
        var newHovered = edgeId != null ? (edgeId, isSource) : ((string, bool)?)null;
        if (_hoveredEndpointHandle != newHovered)
        {
            _hoveredEndpointHandle = newHovered;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Clears the hovered endpoint handle state.
    /// </summary>
    public void ClearHoveredEndpointHandle()
    {
        if (_hoveredEndpointHandle != null)
        {
            _hoveredEndpointHandle = null;
            InvalidateVisual();
        }
    }

    // Inline editing methods are in DirectGraphRenderer.InlineEditing.cs

    private void RebuildSpatialIndex()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (_graph == null)
        {
            _nodeIndex = null;
            _lastIndexedGraph = null;
            return;
        }

        _nodeIndex = new List<(Node, double, double, double, double)>(_graph.Elements.NodeCount);

        int totalNodes = 0;
        int visibleNodes = 0;
        foreach (var node in _graph.Elements.Nodes)
        {
            totalNodes++;
            if (node.IsGroup) continue;
            if (!IsNodeVisibleFast(node)) continue;

            visibleNodes++;
            var bounds = _model.GetNodeBounds(node);
            _nodeIndex.Add((node, bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }

        _lastIndexedGraph = _graph;
        _indexDirty = false;

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[SpatialIndex] Rebuilt in {sw.ElapsedMilliseconds}ms | Total:{totalNodes}, Indexed:{visibleNodes}");
    }

    /// <summary>
    /// Fast visibility check using dictionary lookup instead of LINQ FirstOrDefault.
    /// Checks if node is visible (not inside a collapsed group).
    /// </summary>
    private bool IsNodeVisibleFast(Node node)
    {
        if (_nodeById == null) return true;

        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            if (!_nodeById.TryGetValue(currentParentId, out var parent)) break;
            if (parent.IsCollapsed) return false;
            currentParentId = parent.ParentGroupId;
        }
        return true;
    }

    private void InvalidateBrushes()
    {
        if (_theme == null) return;

        _edgePen = new Pen(_theme.EdgeStroke, 2);
        _edgeSelectedPen = new Pen(_theme.NodeSelectedBorder, 3);
        _nodeBorderPen = new Pen(_theme.NodeBorder, 2);
        _nodeSelectedPen = new Pen(_theme.NodeSelectedBorder, 3);
        _nodeBackground = _theme.NodeBackground;
        _nodeInputBackground = _theme.InputNodeBackground;
        _nodeOutputBackground = _theme.OutputNodeBackground;
        _portBrush = _theme.PortBackground;
        _portPen = new Pen(_theme.PortBorder, 2);
        _resizeHandleFill = _theme.NodeSelectedBorder;
        _resizeHandlePen = new Pen(Brushes.White, 1);
        _endpointHandleFill = _theme.PortBackground;
        _endpointHandlePen = new Pen(_theme.NodeSelectedBorder, 2);
    }

    public override void Render(DrawingContext context)
    {
        if (_graph == null || _viewport == null || _theme == null)
            return;

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        // Ensure node lookup dictionary is built (lazy rebuild after InvalidateIndex)
        if (_nodeById == null)
        {
            _nodeById = _graph.Elements.Nodes.ToDictionary(n => n.Id);
        }

        // For viewport culling, we need the render area size (0,0,width,height)
        // not the control's position in its parent
        var viewBounds = new Rect(0, 0, bounds.Width, bounds.Height);

        var nodeCount = _graph.Elements.NodeCount;

        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        _debugFrameCount++;
        var logThisFrame = _debugFrameCount % 60 == 1; // Log every 60 frames (roughly every second)

        if (logThisFrame)
        {
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] Frame={_debugFrameCount} Viewport: Offset=({offsetX:F1},{offsetY:F1}) Zoom={zoom:F2} Nodes={nodeCount} viewBounds=(0,0,{viewBounds.Width:F0}x{viewBounds.Height:F0})");

            // Calculate what canvas area is visible
            var visibleCanvasMinX = (0 - offsetX) / zoom;
            var visibleCanvasMaxX = (viewBounds.Width - offsetX) / zoom;
            var visibleCanvasMinY = (0 - offsetY) / zoom;
            var visibleCanvasMaxY = (viewBounds.Height - offsetY) / zoom;
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] Visible canvas area: X=[{visibleCanvasMinX:F0} to {visibleCanvasMaxX:F0}] Y=[{visibleCanvasMinY:F0} to {visibleCanvasMaxY:F0}]");
        }

        // Level-of-Detail thresholds
        var showPorts = _settings.ShowPorts && zoom >= 0.4;  // Skip ports when zoomed out or disabled
        var showLabels = zoom >= 0.3;      // Skip labels when very zoomed out
        var useSimplifiedNodes = zoom < 0.5; // Simplified rendering at low zoom

        // Build visible node set for edge culling (only render edges with visible endpoints)
        var visibleNodeIds = new HashSet<string>();

        // Draw groups first (behind everything)
        var groupsDrawn = 0;
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsGroup) continue;
            if (!IsNodeVisibleFast(node)) continue; // Use O(1) lookup instead of O(n)
            if (node.IsCollapsed) continue; // Don't draw collapsed groups' background

            DrawGroup(context, node, zoom, offsetX, offsetY);
            groupsDrawn++;
        }

        // Collect visible regular nodes for edge culling
        var visibleNodeCullStats_Total = 0;
        var visibleNodeCullStats_SkippedGroup = 0;
        var visibleNodeCullStats_SkippedVisibility = 0;
        var visibleNodeCullStats_SkippedBounds = 0;
        foreach (var node in _graph.Elements.Nodes)
        {
            visibleNodeCullStats_Total++;
            if (node.IsGroup) { visibleNodeCullStats_SkippedGroup++; continue; }
            if (!IsNodeVisibleFast(node)) { visibleNodeCullStats_SkippedVisibility++; continue; } // Use O(1) lookup instead of O(n)
            if (!IsInVisibleBounds(node, zoom, offsetX, offsetY, viewBounds)) { visibleNodeCullStats_SkippedBounds++; continue; }

            visibleNodeIds.Add(node.Id);
        }

        if (logThisFrame)
        {
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] VisibleNodes: {visibleNodeIds.Count} of {visibleNodeCullStats_Total} (groups={visibleNodeCullStats_SkippedGroup}, visibility={visibleNodeCullStats_SkippedVisibility}, bounds={visibleNodeCullStats_SkippedBounds})");
        }

        // Draw edges (behind nodes) - ONLY edges with at least one visible endpoint
        var edgesDrawn = 0;
        var edgesSkipped_BothOutside = 0;
        var edgesMissingSource = 0;
        var edgesMissingTarget = 0;
        foreach (var edge in _graph.Elements.Edges)
        {
            // Early culling: skip edges with both endpoints outside viewport
            var sourceVisible = visibleNodeIds.Contains(edge.Source);
            var targetVisible = visibleNodeIds.Contains(edge.Target);

            if (!sourceVisible && !targetVisible)
            {
                edgesSkipped_BothOutside++;
                continue;
            }

            // Track edges where one endpoint's node is missing
            if (_nodeById != null)
            {
                if (!_nodeById.ContainsKey(edge.Source)) edgesMissingSource++;
                if (!_nodeById.ContainsKey(edge.Target)) edgesMissingTarget++;
            }

            DrawEdge(context, edge, zoom, offsetX, offsetY, viewBounds, useSimplifiedNodes);
            edgesDrawn++;
        }

        if (logThisFrame)
        {
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] Edges: drew={edgesDrawn}, skippedBothOutside={edgesSkipped_BothOutside}, missingSource={edgesMissingSource}, missingTarget={edgesMissingTarget}");
        }

        // Draw regular nodes
        var nodesDrawn = 0;
        var nodesSkippedGroup = 0;
        var nodesSkippedVisibility = 0;
        var nodesSkippedBounds = 0;
        foreach (var node in _graph.Elements.Nodes)
        {
            if (node.IsGroup) { nodesSkippedGroup++; continue; }
            if (!IsNodeVisibleFast(node)) { nodesSkippedVisibility++; continue; } // Use O(1) lookup instead of O(n)
            if (!IsInVisibleBounds(node, zoom, offsetX, offsetY, viewBounds)) { nodesSkippedBounds++; continue; }

            DrawNode(context, node, zoom, offsetX, offsetY, showLabels, showPorts, useSimplifiedNodes);
            nodesDrawn++;
        }

        // Draw collapsed groups on top (they appear as small nodes)
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsGroup || !node.IsCollapsed) continue;
            if (!IsNodeVisibleFast(node)) continue; // Use O(1) lookup instead of O(n)

            DrawCollapsedGroup(context, node, zoom, offsetX, offsetY);
        }

        // Draw resize handles for selected nodes (on top of everything)
        var handlesDrawn = 0;
        var handlesSkippedNotSelected = 0;
        var handlesSkippedNotResizable = 0;
        var handlesSkippedVisibility = 0;
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsSelected) { handlesSkippedNotSelected++; continue; }
            if (!node.IsResizable) { handlesSkippedNotResizable++; continue; }
            if (!IsNodeVisibleFast(node)) { handlesSkippedVisibility++; continue; }

            DrawResizeHandles(context, node, zoom, offsetX, offsetY);
            handlesDrawn++;
        }

        // Always log handle status to debug selection issues
        if (logThisFrame || handlesDrawn > 0 || _graph.Elements.Nodes.Any(n => n.IsSelected))
        {
            var selectedCount = _graph.Elements.Nodes.Count(n => n.IsSelected);
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] Handles: drawn={handlesDrawn}, selected={selectedCount}, skipped(notSelected={handlesSkippedNotSelected}, notResizable={handlesSkippedNotResizable}, visibility={handlesSkippedVisibility})");
        }

        // Draw edge endpoint handles for selected edges (on top of everything)
        var edgeHandlesDrawn = 0;
        if (_settings.ShowEdgeEndpointHandles)
        {
            foreach (var edge in _graph.Elements.Edges)
            {
                if (!edge.IsSelected) continue;
                DrawEdgeEndpointHandles(context, edge, zoom, offsetX, offsetY);
                edgeHandlesDrawn++;
            }
        }

        if (logThisFrame)
        {
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] Drew: nodes={nodesDrawn}, groups={groupsDrawn}, edges=?, handles={handlesDrawn}, edgeHandles={edgeHandlesDrawn}");
            System.Diagnostics.Debug.WriteLine($"[DirectRenderer.Render] Skipped: groups={nodesSkippedGroup}, visibility={nodesSkippedVisibility}, bounds={nodesSkippedBounds}");
        }
    }

    // Node rendering methods are in DirectGraphRenderer.NodeRendering.cs
    // Edge rendering methods are in DirectGraphRenderer.EdgeRendering.cs
    // Group rendering methods are in DirectGraphRenderer.GroupRendering.cs
    // Hit testing methods are in DirectGraphRenderer.HitTesting.cs
    // Helper methods are in DirectGraphRenderer.Helpers.cs
    // Inline editing methods are in DirectGraphRenderer.InlineEditing.cs
    // Coordinate transforms are in DirectGraphRenderer.CoordinateTransforms.cs

    #region Drawing Helpers

    private static StreamGeometry CreateRoundedRectGeometry(Rect rect, double radius)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new AvaloniaPoint(rect.Left + radius, rect.Top), true);
            ctx.LineTo(new AvaloniaPoint(rect.Right - radius, rect.Top));
            ctx.ArcTo(new AvaloniaPoint(rect.Right, rect.Top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new AvaloniaPoint(rect.Right, rect.Bottom - radius));
            ctx.ArcTo(new AvaloniaPoint(rect.Right - radius, rect.Bottom), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new AvaloniaPoint(rect.Left + radius, rect.Bottom));
            ctx.ArcTo(new AvaloniaPoint(rect.Left, rect.Bottom - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new AvaloniaPoint(rect.Left, rect.Top + radius));
            ctx.ArcTo(new AvaloniaPoint(rect.Left + radius, rect.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            ctx.EndFigure(true);
        }
        return geometry;
    }

    private void DrawCenteredText(DrawingContext context, string text, Rect bounds, double fontSize, IBrush brush)
    {
        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            fontSize,
            brush);

        var textX = bounds.X + (bounds.Width - formattedText.Width) / 2;
        var textY = bounds.Y + (bounds.Height - formattedText.Height) / 2;
        context.DrawText(formattedText, new AvaloniaPoint(textX, textY));
    }

    #endregion
}
