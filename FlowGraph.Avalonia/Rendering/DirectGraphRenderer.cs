using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
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
/// </remarks>
public class DirectGraphRenderer : Control
{
    private FlowCanvasSettings _settings;
    private readonly GraphRenderModel _model;
    private NodeRendererRegistry? _nodeRenderers;
    private Graph? _graph;
    private ViewportState? _viewport;
    private ThemeResources? _theme;

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

    // Port hover state tracking
    private (string nodeId, string portId)? _hoveredPort;

    // Edge endpoint handle hover state
    private (string edgeId, bool isSource)? _hoveredEndpointHandle;

    // Inline editing state
    private string? _editingNodeId;
    private string? _editingEdgeId;

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
        _model = new GraphRenderModel(settings);
        _nodeRenderers = nodeRenderers;
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
        set => _nodeRenderers = value;
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
        _graph = graph;
        _viewport = viewport;

        if (_theme != theme)
        {
            _theme = theme;
            InvalidateBrushes();
        }

        _indexDirty = true; // Rebuild index on next hit test
        InvalidateVisual();
    }

    /// <summary>
    /// Marks the spatial index as dirty, forcing rebuild on next hit test.
    /// Call this when nodes are added, removed, or moved.
    /// </summary>
    public void InvalidateIndex()
    {
        _indexDirty = true;
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

    #region Inline Editing Support

    /// <summary>
    /// Begins editing a node's label. The label will not be drawn while editing.
    /// </summary>
    /// <param name="nodeId">ID of the node to edit.</param>
    public void BeginEditNode(string nodeId)
    {
        _editingNodeId = nodeId;
        InvalidateVisual();
    }

    /// <summary>
    /// Ends editing a node's label.
    /// </summary>
    public void EndEditNode()
    {
        _editingNodeId = null;
        InvalidateVisual();
    }

    /// <summary>
    /// Begins editing an edge's label. The label will not be drawn while editing.
    /// </summary>
    /// <param name="edgeId">ID of the edge to edit.</param>
    public void BeginEditEdge(string edgeId)
    {
        _editingEdgeId = edgeId;
        InvalidateVisual();
    }

    /// <summary>
    /// Ends editing an edge's label.
    /// </summary>
    public void EndEditEdge()
    {
        _editingEdgeId = null;
        InvalidateVisual();
    }

    #endregion

    private void RebuildSpatialIndex()
    {
        if (_graph == null)
        {
            _nodeIndex = null;
            return;
        }

        _nodeIndex = new List<(Node, double, double, double, double)>(_graph.Elements.Nodes.Count());

        foreach (var node in _graph.Elements.Nodes)
        {
            if (node.IsGroup) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;

            var bounds = _model.GetNodeBounds(node);
            _nodeIndex.Add((node, bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }

        _indexDirty = false;
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

        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        // Draw groups first (behind everything)
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsGroup) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;
            if (node.IsCollapsed) continue; // Don't draw collapsed groups' background

            DrawGroup(context, node, zoom, offsetX, offsetY);
        }

        // Draw edges (behind nodes)
        foreach (var edge in _graph.Elements.Edges)
        {
            DrawEdge(context, edge, zoom, offsetX, offsetY, bounds);
        }

        // Draw regular nodes
        foreach (var node in _graph.Elements.Nodes)
        {
            if (node.IsGroup) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;
            if (!IsInVisibleBounds(node, zoom, offsetX, offsetY, bounds)) continue;

            DrawNode(context, node, zoom, offsetX, offsetY);
        }

        // Draw collapsed groups on top (they appear as small nodes)
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsGroup || !node.IsCollapsed) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;

            DrawCollapsedGroup(context, node, zoom, offsetX, offsetY);
        }

        // Draw resize handles for selected nodes (on top of everything)
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsSelected || !node.IsResizable) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;

            DrawResizeHandles(context, node, zoom, offsetX, offsetY);
        }

        // Draw edge endpoint handles for selected edges (on top of everything)
        if (_settings.ShowEdgeEndpointHandles)
        {
            foreach (var edge in _graph.Elements.Edges)
            {
                if (!edge.IsSelected) continue;
                DrawEdgeEndpointHandles(context, edge, zoom, offsetX, offsetY);
            }
        }
    }

    #region Node Rendering

    private void DrawNode(DrawingContext context, Node node, double zoom, double offsetX, double offsetY)
    {
        var canvasBounds = _model.GetNodeBounds(node);
        var screenBounds = CanvasToScreen(canvasBounds, zoom, offsetX, offsetY);

        // Check if custom renderer exists and implements IDirectNodeRenderer
        if (_nodeRenderers != null)
        {
            var renderer = _nodeRenderers.GetRenderer(node.Type);
            if (renderer is IDirectNodeRenderer directRenderer)
            {
                var background = GetNodeBackground(node);
                var borderPen = node.IsSelected ? _nodeSelectedPen : _nodeBorderPen;

                var renderContext = new DirectNodeRenderContext
                {
                    ScreenBounds = screenBounds,
                    Zoom = zoom,
                    IsSelected = node.IsSelected,
                    IsEditing = _editingNodeId == node.Id,
                    Background = background,
                    BorderPen = borderPen,
                    TextBrush = _theme!.NodeText,
                    Theme = _theme,
                    Settings = _settings,
                    Model = _model
                };

                directRenderer.DrawNode(context, node, renderContext);
                DrawNodePorts(context, node, canvasBounds, zoom, offsetX, offsetY);
                return;
            }
        }

        // Default drawing
        var cornerRadius = GraphRenderModel.NodeCornerRadius * zoom;
        var defaultBackground = GetNodeBackground(node);

        // Draw rounded rectangle
        var geometry = CreateRoundedRectGeometry(screenBounds, cornerRadius);
        context.DrawGeometry(defaultBackground, node.IsSelected ? _nodeSelectedPen : _nodeBorderPen, geometry);

        // Draw label (skip if being edited)
        if (_editingNodeId != node.Id)
        {
            var label = node.Label ?? node.Type ?? node.Id;
            if (!string.IsNullOrEmpty(label))
            {
                DrawCenteredText(context, label, screenBounds, 10 * zoom, _theme!.NodeText);
            }
        }

        // Draw ports
        DrawNodePorts(context, node, canvasBounds, zoom, offsetX, offsetY);
    }

    private IBrush? GetNodeBackground(Node node)
    {
        return node.Type?.ToLowerInvariant() switch
        {
            "input" => _nodeInputBackground,
            "output" => _nodeOutputBackground,
            _ => _nodeBackground
        };
    }

    private void DrawNodePorts(DrawingContext context, Node node, Rect canvasBounds, double zoom, double offsetX, double offsetY)
    {
        var portSize = _settings.PortSize * zoom;

        // Input ports
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            var canvasPos = _model.GetPortPositionByIndex(node, i, node.Inputs.Count, false);
            var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

            var isHovered = _hoveredPort.HasValue &&
                           _hoveredPort.Value.nodeId == node.Id &&
                           _hoveredPort.Value.portId == port.Id;

            var brush = isHovered ? _theme!.PortHover : _portBrush;
            context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
        }

        // Output ports
        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var port = node.Outputs[i];
            var canvasPos = _model.GetPortPositionByIndex(node, i, node.Outputs.Count, true);
            var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

            var isHovered = _hoveredPort.HasValue &&
                           _hoveredPort.Value.nodeId == node.Id &&
                           _hoveredPort.Value.portId == port.Id;

            var brush = isHovered ? _theme!.PortHover : _portBrush;
            context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
        }
    }

    #endregion

    #region Edge Rendering

    private void DrawEdge(DrawingContext context, Edge edge, double zoom, double offsetX, double offsetY, Rect viewBounds)
    {
        var sourceNode = _graph!.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = _graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null) return;
        if (!GraphRenderModel.IsNodeVisible(_graph, sourceNode) || !GraphRenderModel.IsNodeVisible(_graph, targetNode)) return;

        var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, _graph);
        var startScreen = CanvasToScreen(startCanvas, zoom, offsetX, offsetY);
        var endScreen = CanvasToScreen(endCanvas, zoom, offsetX, offsetY);

        // Cull edges outside visible bounds
        var margin = 100 * zoom;
        var edgeMinX = Math.Min(startScreen.X, endScreen.X) - margin;
        var edgeMaxX = Math.Max(startScreen.X, endScreen.X) + margin;
        var edgeMinY = Math.Min(startScreen.Y, endScreen.Y) - margin;
        var edgeMaxY = Math.Max(startScreen.Y, endScreen.Y) + margin;

        if (edgeMaxX < 0 || edgeMinX > viewBounds.Width || edgeMaxY < 0 || edgeMinY > viewBounds.Height)
            return;

        var pen = edge.IsSelected ? _edgeSelectedPen : _edgePen;

        // Get control points (scaled to screen)
        var (cp1Canvas, cp2Canvas) = _model.GetBezierControlPoints(startCanvas, endCanvas);
        var cp1Screen = CanvasToScreen(cp1Canvas, zoom, offsetX, offsetY);
        var cp2Screen = CanvasToScreen(cp2Canvas, zoom, offsetX, offsetY);

        // Draw bezier curve
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(startScreen, false);
            ctx.CubicBezierTo(cp1Screen, cp2Screen, endScreen);
            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);

        // Draw arrow at end
        if (edge.MarkerEnd != EdgeMarker.None)
        {
            var angle = Math.Atan2(endScreen.Y - cp2Screen.Y, endScreen.X - cp2Screen.X);
            DrawArrow(context, endScreen, angle, pen!.Brush, zoom, edge.MarkerEnd == EdgeMarker.ArrowClosed);
        }

        // Draw edge label (skip if being edited)
        if (!string.IsNullOrEmpty(edge.Label) && _editingEdgeId != edge.Id)
        {
            var midCanvas = _model.GetEdgeMidpoint(startCanvas, endCanvas);
            var midScreen = CanvasToScreen(midCanvas, zoom, offsetX, offsetY);
            DrawEdgeLabel(context, edge.Label, midScreen, zoom);
        }
    }

    private void DrawEdgeEndpointHandles(DrawingContext context, Edge edge, double zoom, double offsetX, double offsetY)
    {
        var sourceNode = _graph!.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = _graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null) return;

        var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, _graph);
        var startScreen = CanvasToScreen(startCanvas, zoom, offsetX, offsetY);
        var endScreen = CanvasToScreen(endCanvas, zoom, offsetX, offsetY);

        var handleSize = _settings.EdgeEndpointHandleSize * zoom;
        var halfSize = handleSize / 2;

        // Source handle
        var isSourceHovered = _hoveredEndpointHandle.HasValue &&
                              _hoveredEndpointHandle.Value.edgeId == edge.Id &&
                              _hoveredEndpointHandle.Value.isSource;
        var sourceFill = isSourceHovered ? _theme!.PortHover : _endpointHandleFill;
        context.DrawEllipse(sourceFill, _endpointHandlePen, startScreen, halfSize, halfSize);

        // Target handle
        var isTargetHovered = _hoveredEndpointHandle.HasValue &&
                              _hoveredEndpointHandle.Value.edgeId == edge.Id &&
                              !_hoveredEndpointHandle.Value.isSource;
        var targetFill = isTargetHovered ? _theme!.PortHover : _endpointHandleFill;
        context.DrawEllipse(targetFill, _endpointHandlePen, endScreen, halfSize, halfSize);
    }

    private void DrawEdgeLabel(DrawingContext context, string label, AvaloniaPoint midScreen, double zoom)
    {
        var fontSize = 12 * zoom;
        var formattedText = new FormattedText(
            label,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            fontSize,
            _theme!.NodeText);

        var padding = 4 * zoom;
        var bgRect = new Rect(
            midScreen.X - padding,
            midScreen.Y - 10 * zoom - padding,
            formattedText.Width + padding * 2,
            formattedText.Height + padding * 2);

        context.DrawRectangle(_theme.NodeBackground, null, bgRect, 3 * zoom, 3 * zoom);
        context.DrawText(formattedText, new AvaloniaPoint(midScreen.X, midScreen.Y - 10 * zoom));
    }

    private void DrawArrow(DrawingContext context, AvaloniaPoint tip, double angle, IBrush? brush, double zoom, bool filled)
    {
        var arrowSize = 10 * zoom;

        var p1 = new AvaloniaPoint(
            tip.X - arrowSize * Math.Cos(angle - Math.PI / 6),
            tip.Y - arrowSize * Math.Sin(angle - Math.PI / 6));
        var p2 = new AvaloniaPoint(
            tip.X - arrowSize * Math.Cos(angle + Math.PI / 6),
            tip.Y - arrowSize * Math.Sin(angle + Math.PI / 6));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(tip, filled);
            ctx.LineTo(p1);
            ctx.LineTo(p2);
            ctx.EndFigure(true);
        }

        var pen = filled ? null : new Pen(brush, 2 * zoom);
        context.DrawGeometry(filled ? brush : null, pen, geometry);
    }

    #endregion

    #region Resize Handles

    private void DrawResizeHandles(DrawingContext context, Node node, double zoom, double offsetX, double offsetY)
    {
        var handleSize = GraphRenderModel.ResizeHandleSize * zoom;

        foreach (var (_, center) in _model.GetResizeHandlePositions(node))
        {
            var screenCenter = CanvasToScreen(center, zoom, offsetX, offsetY);
            var rect = new Rect(
                screenCenter.X - handleSize / 2,
                screenCenter.Y - handleSize / 2,
                handleSize,
                handleSize);
            context.DrawRectangle(_resizeHandleFill, _resizeHandlePen, rect);
        }
    }

    #endregion

    #region Group Rendering

    private void DrawGroup(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
    {
        var canvasBounds = _model.GetNodeBounds(group);
        var screenBounds = CanvasToScreen(canvasBounds, zoom, offsetX, offsetY);
        var cornerRadius = GraphRenderModel.GroupBorderRadius * zoom;

        // Background fill
        var bgGeometry = CreateRoundedRectGeometry(screenBounds, cornerRadius);
        context.DrawGeometry(_theme!.GroupBackground, null, bgGeometry);

        // Border
        var borderBrush = group.IsSelected ? _theme.NodeSelectedBorder : _theme.GroupBorder;
        var borderPen = new Pen(borderBrush, GraphRenderModel.GroupDashedStrokeThickness * zoom);
        if (!group.IsSelected)
        {
            borderPen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
        }

        var borderGeometry = CreateRoundedRectGeometry(screenBounds, cornerRadius);
        context.DrawGeometry(null, borderPen, borderGeometry);

        // Header
        DrawGroupHeader(context, group, zoom, offsetX, offsetY);

        // Ports
        DrawGroupPorts(context, group, canvasBounds, zoom, offsetX, offsetY);
    }

    private void DrawCollapsedGroup(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
    {
        // Same as DrawGroup but with collapsed height
        DrawGroup(context, group, zoom, offsetX, offsetY);
    }

    private void DrawGroupHeader(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
    {
        // Collapse button
        var buttonBounds = _model.GetGroupCollapseButtonBounds(group);
        var screenButtonBounds = CanvasToScreen(buttonBounds, zoom, offsetX, offsetY);

        var buttonBrush = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
        context.DrawRectangle(buttonBrush, null, screenButtonBounds, 3 * zoom, 3 * zoom);

        // Collapse indicator
        var indicatorText = group.IsCollapsed ? "+" : "-";
        var indicatorFontSize = 12 * zoom;
        var indicatorFormatted = new FormattedText(
            indicatorText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
            indicatorFontSize,
            _theme!.GroupLabelText);

        var indicatorX = screenButtonBounds.X + (screenButtonBounds.Width - indicatorFormatted.Width) / 2;
        var indicatorY = screenButtonBounds.Y + (screenButtonBounds.Height - indicatorFormatted.Height) / 2;
        context.DrawText(indicatorFormatted, new AvaloniaPoint(indicatorX, indicatorY));

        // Label (skip if being edited)
        if (_editingNodeId != group.Id)
        {
            var label = group.Label ?? "Group";
            var fontSize = 11 * zoom;

            // Get color from theme, with fallback for non-SolidColorBrush
            var labelBrush = _theme.GroupLabelText;
            if (labelBrush is SolidColorBrush solidBrush)
            {
                labelBrush = new SolidColorBrush(solidBrush.Color, 0.9);
            }

            var formattedText = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Medium, FontStretch.Normal),
                fontSize,
                labelBrush);

            var labelPos = _model.GetGroupLabelPosition(group);
            var screenLabelPos = CanvasToScreen(labelPos, zoom, offsetX, offsetY);

            // Adjust Y to center with button
            var textY = screenButtonBounds.Y + (screenButtonBounds.Height - formattedText.Height) / 2;

            context.DrawText(formattedText, new AvaloniaPoint(screenLabelPos.X, textY));
        }
    }

    private void DrawGroupPorts(DrawingContext context, Node group, Rect canvasBounds, double zoom, double offsetX, double offsetY)
    {
        var portSize = _settings.PortSize * zoom;

        // Input ports
        for (int i = 0; i < group.Inputs.Count; i++)
        {
            var port = group.Inputs[i];
            var canvasPos = _model.GetPortPositionByIndex(group, i, group.Inputs.Count, false);
            var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

            var isHovered = _hoveredPort.HasValue &&
                           _hoveredPort.Value.nodeId == group.Id &&
                           _hoveredPort.Value.portId == port.Id;

            var brush = isHovered ? _theme!.PortHover : _portBrush;
            context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
        }

        // Output ports
        for (int i = 0; i < group.Outputs.Count; i++)
        {
            var port = group.Outputs[i];
            var canvasPos = _model.GetPortPositionByIndex(group, i, group.Outputs.Count, true);
            var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

            var isHovered = _hoveredPort.HasValue &&
                           _hoveredPort.Value.nodeId == group.Id &&
                           _hoveredPort.Value.portId == port.Id;

            var brush = isHovered ? _theme!.PortHover : _portBrush;
            context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
        }
    }

    #endregion

    #region Hit Testing

    /// <summary>
    /// Performs hit testing to find an edge endpoint handle at the given screen coordinates.
    /// </summary>
    /// <returns>Tuple of (edge, isSource) or null if no handle hit.</returns>
    public (Edge edge, bool isSource)? HitTestEdgeEndpointHandle(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null || !_settings.ShowEdgeEndpointHandles) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        var handleRadius = _settings.EdgeEndpointHandleSize / 2 + 4; // Extra padding for easier clicking

        foreach (var edge in _graph.Elements.Edges)
        {
            if (!edge.IsSelected) continue;

            var sourceNode = _graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = _graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

            if (sourceNode == null || targetNode == null) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, sourceNode) || !GraphRenderModel.IsNodeVisible(_graph, targetNode)) continue;

            var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, _graph);

            // Check source handle
            var dxSource = canvasPoint.X - startCanvas.X;
            var dySource = canvasPoint.Y - startCanvas.Y;
            if (dxSource * dxSource + dySource * dySource <= handleRadius * handleRadius)
            {
                return (edge, true);
            }

            // Check target handle
            var dxTarget = canvasPoint.X - endCanvas.X;
            var dyTarget = canvasPoint.Y - endCanvas.Y;
            if (dxTarget * dxTarget + dyTarget * dyTarget <= handleRadius * handleRadius)
            {
                return (edge, false);
            }
        }

        return null;
    }

    /// <summary>
    /// Performs hit testing to find a resize handle at the given screen coordinates.
    /// </summary>
    public (Node node, ResizeHandlePosition position)? HitTestResizeHandle(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);

        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsSelected || !node.IsResizable) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;

            foreach (var (pos, center) in _model.GetResizeHandlePositions(node))
            {
                if (_model.IsPointInResizeHandle(canvasPoint, center))
                {
                    return (node, pos);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Performs hit testing to find a node at the given screen coordinates.
    /// </summary>
    public Node? HitTestNode(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null) return null;

        if (_indexDirty) RebuildSpatialIndex();
        if (_nodeIndex == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);

        // Check regular nodes first (they're on top)
        for (int i = _nodeIndex.Count - 1; i >= 0; i--)
        {
            var (node, nx, ny, nw, nh) = _nodeIndex[i];
            var bounds = new Rect(nx, ny, nw, nh);

            if (bounds.Contains(canvasPoint))
            {
                return node;
            }
        }

        // Check groups (they're behind regular nodes)
        foreach (var group in _graph.Elements.Nodes.Where(n => n.IsGroup))
        {
            if (!GraphRenderModel.IsNodeVisible(_graph, group)) continue;

            var bounds = _model.GetNodeBounds(group);
            if (bounds.Contains(canvasPoint))
            {
                return group;
            }
        }

        return null;
    }

    /// <summary>
    /// Performs hit testing to find a port at the given screen coordinates.
    /// </summary>
    public (Node node, Port port, bool isOutput)? HitTestPort(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null) return null;

        if (_indexDirty) RebuildSpatialIndex();
        if (_nodeIndex == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);

        // Check regular nodes
        foreach (var (node, _, _, _, _) in _nodeIndex)
        {
            // Check input ports
            for (int i = 0; i < node.Inputs.Count; i++)
            {
                var portPos = _model.GetPortPositionByIndex(node, i, node.Inputs.Count, false);
                if (_model.IsPointInPort(canvasPoint, portPos))
                {
                    return (node, node.Inputs[i], false);
                }
            }

            // Check output ports
            for (int i = 0; i < node.Outputs.Count; i++)
            {
                var portPos = _model.GetPortPositionByIndex(node, i, node.Outputs.Count, true);
                if (_model.IsPointInPort(canvasPoint, portPos))
                {
                    return (node, node.Outputs[i], true);
                }
            }
        }

        // Check group ports
        foreach (var group in _graph.Elements.Nodes.Where(n => n.IsGroup))
        {
            if (!GraphRenderModel.IsNodeVisible(_graph, group)) continue;

            // Check input ports
            for (int i = 0; i < group.Inputs.Count; i++)
            {
                var portPos = _model.GetPortPositionByIndex(group, i, group.Inputs.Count, false);
                if (_model.IsPointInPort(canvasPoint, portPos))
                {
                    return (group, group.Inputs[i], false);
                }
            }

            // Check output ports
            for (int i = 0; i < group.Outputs.Count; i++)
            {
                var portPos = _model.GetPortPositionByIndex(group, i, group.Outputs.Count, true);
                if (_model.IsPointInPort(canvasPoint, portPos))
                {
                    return (group, group.Outputs[i], true);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Performs hit testing to find an edge at the given screen coordinates.
    /// </summary>
    public Edge? HitTestEdge(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        var hitDistance = _settings.EdgeHitAreaWidth / _viewport.Zoom;

        foreach (var edge in _graph.Elements.Edges)
        {
            var sourceNode = _graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = _graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

            if (sourceNode == null || targetNode == null) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, sourceNode) || !GraphRenderModel.IsNodeVisible(_graph, targetNode)) continue;

            var (start, end) = _model.GetEdgeEndpoints(edge, _graph);

            if (_model.IsPointNearEdge(canvasPoint, start, end, hitDistance))
            {
                return edge;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the screen position for a port (for creating connection temp lines, etc.)
    /// </summary>
    public AvaloniaPoint GetPortScreenPosition(Node node, Port port, bool isOutput)
    {
        if (_viewport == null) return default;

        var canvasPos = _model.GetPortPosition(node, port, isOutput);
        return CanvasToScreen(canvasPos, _viewport.Zoom, _viewport.OffsetX, _viewport.OffsetY);
    }

    /// <summary>
    /// Gets the screen position for an edge endpoint (source or target).
    /// </summary>
    public AvaloniaPoint GetEdgeEndpointScreenPosition(Edge edge, bool isSource)
    {
        if (_graph == null || _viewport == null) return default;

        var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, _graph);
        var canvasPos = isSource ? startCanvas : endCanvas;
        return CanvasToScreen(canvasPos, _viewport.Zoom, _viewport.OffsetX, _viewport.OffsetY);
    }

    #endregion

    #region Coordinate Transforms

    private AvaloniaPoint ScreenToCanvas(double screenX, double screenY)
    {
        if (_viewport == null) return new AvaloniaPoint(screenX, screenY);
        return new AvaloniaPoint(
            (screenX - _viewport.OffsetX) / _viewport.Zoom,
            (screenY - _viewport.OffsetY) / _viewport.Zoom);
    }

    private AvaloniaPoint CanvasToScreen(AvaloniaPoint canvasPoint, double zoom, double offsetX, double offsetY)
    {
        return new AvaloniaPoint(
            canvasPoint.X * zoom + offsetX,
            canvasPoint.Y * zoom + offsetY);
    }

    private Rect CanvasToScreen(Rect canvasRect, double zoom, double offsetX, double offsetY)
    {
        return new Rect(
            canvasRect.X * zoom + offsetX,
            canvasRect.Y * zoom + offsetY,
            canvasRect.Width * zoom,
            canvasRect.Height * zoom);
    }

    private bool IsInVisibleBounds(Node node, double zoom, double offsetX, double offsetY, Rect viewBounds)
    {
        var canvasBounds = _model.GetNodeBounds(node);
        var screenBounds = CanvasToScreen(canvasBounds, zoom, offsetX, offsetY);
        var buffer = _settings.PortSize * zoom;

        return screenBounds.X + screenBounds.Width + buffer >= 0 &&
               screenBounds.X - buffer <= viewBounds.Width &&
               screenBounds.Y + screenBounds.Height + buffer >= 0 &&
               screenBounds.Y - buffer <= viewBounds.Height;
    }

    #endregion

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
