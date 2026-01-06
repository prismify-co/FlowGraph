using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Core;
using System.Diagnostics;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// A high-performance graph renderer that draws directly to a DrawingContext,
/// bypassing the Avalonia visual tree for nodes and edges.
/// This is optimized for large graphs (500+ nodes) where per-element Controls are too slow.
/// </summary>
public class DirectGraphRenderer : Control
{
    private readonly FlowCanvasSettings _settings;
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

    // Typeface for labels
    private Typeface _typeface = new Typeface("Segoe UI");

    // Spatial index for fast hit testing
    private List<(Node node, double x, double y, double width, double height)>? _nodeIndex;
    private bool _indexDirty = true;

    // Group rendering constants (must match GroupNodeRenderer for 100% parity)
    private const double GroupHeaderHeight = 28;
    private const double MinGroupWidth = 200;
    private const double MinGroupHeight = 100;
    private const double GroupCollapseButtonSize = 18;
    private const double GroupBorderRadius = 8;
    private const double GroupDashedStrokeThickness = 2;

    public DirectGraphRenderer(FlowCanvasSettings settings)
    {
        _settings = settings;
        IsHitTestVisible = false; // Hit testing handled separately
    }

    /// <summary>
    /// Updates the renderer with current graph state and triggers a redraw.
    /// </summary>
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
    /// </summary>
    public void InvalidateIndex()
    {
        _indexDirty = true;
    }

    private void RebuildSpatialIndex()
    {
        if (_graph == null)
        {
            _nodeIndex = null;
            return;
        }

        _nodeIndex = new List<(Node, double, double, double, double)>(_graph.Nodes.Count);
        
        foreach (var node in _graph.Nodes)
        {
            if (node.IsGroup) continue;
            if (!IsNodeVisible(_graph, node)) continue;

            var width = node.Width ?? _settings.NodeWidth;
            var height = node.Height ?? _settings.NodeHeight;
            
            _nodeIndex.Add((node, node.Position.X, node.Position.Y, width, height));
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
        _nodeInputBackground = new SolidColorBrush(Color.FromRgb(200, 230, 200));
        _nodeOutputBackground = new SolidColorBrush(Color.FromRgb(230, 200, 200));
        _portBrush = _theme.PortBackground;
        _portPen = new Pen(_theme.PortBorder, 2);
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
        foreach (var node in _graph.Nodes)
        {
            if (!node.IsGroup) continue;
            if (!IsNodeVisible(_graph, node)) continue;
            if (node.IsCollapsed) continue; // Don't draw collapsed groups' background
            
            DrawGroup(context, node, zoom, offsetX, offsetY);
        }

        // Draw edges (behind nodes)
        foreach (var edge in _graph.Edges)
        {
            DrawEdge(context, edge, zoom, offsetX, offsetY, bounds);
        }

        // Draw regular nodes
        foreach (var node in _graph.Nodes)
        {
            if (node.IsGroup) continue;
            if (!IsNodeVisible(_graph, node)) continue;
            if (!IsInVisibleBounds(node, zoom, offsetX, offsetY, bounds)) continue;

            DrawNode(context, node, zoom, offsetX, offsetY);
        }

        // Draw collapsed groups on top (they appear as small nodes)
        foreach (var node in _graph.Nodes)
        {
            if (!node.IsGroup || !node.IsCollapsed) continue;
            if (!IsNodeVisible(_graph, node)) continue;
            
            DrawCollapsedGroup(context, node, zoom, offsetX, offsetY);
        }
    }

    private void DrawNode(DrawingContext context, Node node, double zoom, double offsetX, double offsetY)
    {
        var width = (node.Width ?? _settings.NodeWidth) * zoom;
        var height = (node.Height ?? _settings.NodeHeight) * zoom;
        var x = node.Position.X * zoom + offsetX;
        var y = node.Position.Y * zoom + offsetY;

        var rect = new Rect(x, y, width, height);
        var cornerRadius = 6 * zoom;

        // Choose background based on type
        var background = node.Type?.ToLowerInvariant() switch
        {
            "input" => _nodeInputBackground,
            "output" => _nodeOutputBackground,
            _ => _nodeBackground
        };

        // Draw rounded rectangle
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            DrawRoundedRect(ctx, rect, cornerRadius);
        }

        context.DrawGeometry(background, node.IsSelected ? _nodeSelectedPen : _nodeBorderPen, geometry);

        // Draw label
        var label = node.Label ?? node.Type ?? node.Id;
        if (!string.IsNullOrEmpty(label))
        {
            var fontSize = 10 * zoom;
            var formattedText = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                fontSize,
                _theme!.NodeText);

            // Center text in node
            var textX = x + (width - formattedText.Width) / 2;
            var textY = y + (height - formattedText.Height) / 2;
            context.DrawText(formattedText, new AvaloniaPoint(textX, textY));
        }

        // Draw ports
        var portSize = _settings.PortSize * zoom;

        // Input ports (left side)
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var portY = GetPortY(y, height, i, node.Inputs.Count);
            var portX = x;
            context.DrawEllipse(_portBrush, _portPen, new AvaloniaPoint(portX, portY), portSize / 2, portSize / 2);
        }

        // Output ports (right side)
        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var portY = GetPortY(y, height, i, node.Outputs.Count);
            var portX = x + width;
            context.DrawEllipse(_portBrush, _portPen, new AvaloniaPoint(portX, portY), portSize / 2, portSize / 2);
        }
    }

    private void DrawEdge(DrawingContext context, Edge edge, double zoom, double offsetX, double offsetY, Rect bounds)
    {
        var sourceNode = _graph!.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = _graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null) return;
        if (!IsNodeVisible(_graph, sourceNode) || !IsNodeVisible(_graph, targetNode)) return;

        var sourceWidth = (sourceNode.Width ?? _settings.NodeWidth) * zoom;
        var sourceHeight = (sourceNode.Height ?? _settings.NodeHeight) * zoom;
        var targetHeight = (targetNode.Height ?? _settings.NodeHeight) * zoom;

        var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
        var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);
        if (sourcePortIndex < 0) sourcePortIndex = 0;
        if (targetPortIndex < 0) targetPortIndex = 0;

        // Calculate start point (right side of source node, at port position)
        var startX = sourceNode.Position.X * zoom + offsetX + sourceWidth;
        var startY = GetPortY(
            sourceNode.Position.Y * zoom + offsetY,
            sourceHeight,
            sourcePortIndex,
            Math.Max(1, sourceNode.Outputs.Count));

        // Calculate end point (left side of target node, at port position)
        var endX = targetNode.Position.X * zoom + offsetX;
        var endY = GetPortY(
            targetNode.Position.Y * zoom + offsetY,
            targetHeight,
            targetPortIndex,
            Math.Max(1, targetNode.Inputs.Count));

        // Cull edges that are completely outside visible bounds (with generous margin for curves)
        var margin = 100 * zoom;
        var edgeMinX = Math.Min(startX, endX) - margin;
        var edgeMaxX = Math.Max(startX, endX) + margin;
        var edgeMinY = Math.Min(startY, endY) - margin;
        var edgeMaxY = Math.Max(startY, endY) + margin;
        
        if (edgeMaxX < 0 || edgeMinX > bounds.Width || edgeMaxY < 0 || edgeMinY > bounds.Height)
            return;

        var pen = edge.IsSelected ? _edgeSelectedPen : _edgePen;

        // Calculate bezier control point offset based on horizontal distance
        var dx = endX - startX;
        
        // Use larger control offset for longer horizontal distances, scaled by zoom
        var controlOffset = Math.Max(50 * zoom, Math.Abs(dx) * 0.5);
        
        // Cap the control offset to prevent extremely wide curves
        controlOffset = Math.Min(controlOffset, 150 * zoom);

        // Draw bezier curve
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new AvaloniaPoint(startX, startY), false);
            
            // For left-to-right connections, control points extend horizontally
            var cp1 = new AvaloniaPoint(startX + controlOffset, startY);
            var cp2 = new AvaloniaPoint(endX - controlOffset, endY);

            ctx.CubicBezierTo(cp1, cp2, new AvaloniaPoint(endX, endY));
            ctx.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);

        // Draw arrow at end (pointing left, into the input port)
        if (edge.MarkerEnd != EdgeMarker.None)
        {
            DrawArrow(context, new AvaloniaPoint(endX, endY), new AvaloniaPoint(endX + controlOffset * 0.3, endY), pen!.Brush, zoom, edge.MarkerEnd == EdgeMarker.ArrowClosed);
        }
    }

    private void DrawArrow(DrawingContext context, AvaloniaPoint tip, AvaloniaPoint from, IBrush? brush, double zoom, bool filled)
    {
        var arrowSize = 10 * zoom;
        var angle = Math.Atan2(tip.Y - from.Y, tip.X - from.X);

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

    private static void DrawRoundedRect(StreamGeometryContext ctx, Rect rect, double radius)
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

    private static double GetPortY(double nodeY, double nodeHeight, int portIndex, int totalPorts)
    {
        if (totalPorts == 1)
            return nodeY + nodeHeight / 2;

        var spacing = nodeHeight / (totalPorts + 1);
        return nodeY + spacing * (portIndex + 1);
    }

    private bool IsInVisibleBounds(Node node, double zoom, double offsetX, double offsetY, Rect bounds)
    {
        var width = (node.Width ?? _settings.NodeWidth) * zoom;
        var height = (node.Height ?? _settings.NodeHeight) * zoom;
        var x = node.Position.X * zoom + offsetX;
        var y = node.Position.Y * zoom + offsetY;

        // Add buffer for ports
        var buffer = _settings.PortSize * zoom;
        return x + width + buffer >= 0 && x - buffer <= bounds.Width &&
               y + height + buffer >= 0 && y - buffer <= bounds.Height;
    }

    private static bool IsNodeVisible(Graph graph, Node node)
    {
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            var parent = graph.Nodes.FirstOrDefault(n => n.Id == currentParentId);
            if (parent == null) break;
            if (parent.IsCollapsed) return false;
            currentParentId = parent.ParentGroupId;
        }
        return true;
    }

    #region Hit Testing

    /// <summary>
    /// Performs hit testing to find a node at the given screen coordinates.
    /// </summary>
    public Node? HitTestNode(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null) return null;

        // Rebuild index if needed
        if (_indexDirty)
        {
            RebuildSpatialIndex();
        }
        if (_nodeIndex == null) return null;

        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        // Convert screen coords to canvas coords
        var canvasX = (screenX - offsetX) / zoom;
        var canvasY = (screenY - offsetY) / zoom;

        // Check regular nodes first (they're on top)
        for (int i = _nodeIndex.Count - 1; i >= 0; i--)
        {
            var (node, nx, ny, nw, nh) = _nodeIndex[i];
            
            if (canvasX >= nx && canvasX <= nx + nw &&
                canvasY >= ny && canvasY <= ny + nh)
            {
                return node;
            }
        }

        // Check groups (they're behind regular nodes)
        foreach (var group in _graph.Nodes.Where(n => n.IsGroup))
        {
            if (!IsNodeVisible(_graph, group)) continue;
            
            var gw = group.Width ?? MinGroupWidth;
            var gh = group.IsCollapsed ? GroupHeaderHeight : (group.Height ?? MinGroupHeight);
            var gx = group.Position.X;
            var gy = group.Position.Y;
            
            if (canvasX >= gx && canvasX <= gx + gw &&
                canvasY >= gy && canvasY <= gy + gh)
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

        // Rebuild index if needed
        if (_indexDirty) RebuildSpatialIndex();
        if (_nodeIndex == null) return null;

        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;
        var portRadius = (_settings.PortSize / 2 + 4); // Add padding for easier clicking

        // Convert screen coords to canvas coords
        var canvasX = (screenX - offsetX) / zoom;
        var canvasY = (screenY - offsetY) / zoom;
        var portRadiusSq = portRadius * portRadius;

        // Check regular nodes
        foreach (var (node, nx, ny, nw, nh) in _nodeIndex)
        {
            // Quick bounds check with port radius
            if (canvasX < nx - portRadius || canvasX > nx + nw + portRadius ||
                canvasY < ny - portRadius || canvasY > ny + nh + portRadius)
                continue;

            // Check input ports (left side)
            for (int i = 0; i < node.Inputs.Count; i++)
            {
                var portX = nx;
                var portY = GetPortYCanvas(ny, nh, i, node.Inputs.Count);

                var dx = canvasX - portX;
                var dy = canvasY - portY;
                if (dx * dx + dy * dy <= portRadiusSq)
                {
                    return (node, node.Inputs[i], false);
                }
            }

            // Check output ports (right side)
            for (int i = 0; i < node.Outputs.Count; i++)
            {
                var portX = nx + nw;
                var portY = GetPortYCanvas(ny, nh, i, node.Outputs.Count);

                var dx = canvasX - portX;
                var dy = canvasY - portY;
                if (dx * dx + dy * dy <= portRadiusSq)
                {
                    return (node, node.Outputs[i], true);
                }
            }
        }

        // Check group ports (groups can have external connection ports)
        foreach (var group in _graph.Nodes.Where(n => n.IsGroup))
        {
            if (!IsNodeVisible(_graph, group)) continue;
            
            var gw = group.Width ?? MinGroupWidth;
            var gh = group.IsCollapsed ? GroupHeaderHeight : (group.Height ?? MinGroupHeight);
            var gx = group.Position.X;
            var gy = group.Position.Y;

            // Quick bounds check
            if (canvasX < gx - portRadius || canvasX > gx + gw + portRadius ||
                canvasY < gy - portRadius || canvasY > gy + gh + portRadius)
                continue;

            // Check group input ports (left side)
            for (int i = 0; i < group.Inputs.Count; i++)
            {
                var portX = gx;
                var portY = GetPortYCanvas(gy, gh, i, group.Inputs.Count);

                var dx = canvasX - portX;
                var dy = canvasY - portY;
                if (dx * dx + dy * dy <= portRadiusSq)
                {
                    return (group, group.Inputs[i], false);
                }
            }

            // Check group output ports (right side)
            for (int i = 0; i < group.Outputs.Count; i++)
            {
                var portX = gx + gw;
                var portY = GetPortYCanvas(gy, gh, i, group.Outputs.Count);

                var dx = canvasX - portX;
                var dy = canvasY - portY;
                if (dx * dx + dy * dy <= portRadiusSq)
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

        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;
        
        // Hit distance should be constant in screen pixels, not affected by zoom
        // This matches how the invisible hit area path works in normal rendering
        var hitDistanceScreen = _settings.EdgeHitAreaWidth;

        // Convert screen coords to canvas coords
        var canvasX = (screenX - offsetX) / zoom;
        var canvasY = (screenY - offsetY) / zoom;
        
        // Convert hit distance to canvas coords for comparison
        var hitDistanceCanvas = hitDistanceScreen / zoom;

        foreach (var edge in _graph.Edges)
        {
            var sourceNode = _graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
            var targetNode = _graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

            if (sourceNode == null || targetNode == null) continue;
            if (!IsNodeVisible(_graph, sourceNode) || !IsNodeVisible(_graph, targetNode)) continue;

            var sourceWidth = sourceNode.Width ?? _settings.NodeWidth;
            var sourceHeight = sourceNode.Height ?? _settings.NodeHeight;
            var targetHeight = targetNode.Height ?? _settings.NodeHeight;

            var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
            var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);
            if (sourcePortIndex < 0) sourcePortIndex = 0;
            if (targetPortIndex < 0) targetPortIndex = 0;

            var startX = sourceNode.Position.X + sourceWidth;
            var startY = GetPortYCanvas(sourceNode.Position.Y, sourceHeight, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count));
            var endX = targetNode.Position.X;
            var endY = GetPortYCanvas(targetNode.Position.Y, targetHeight, targetPortIndex, Math.Max(1, targetNode.Inputs.Count));

            // Quick bounding box check first (with generous margin for bezier curves)
            var curveMargin = Math.Max(50, Math.Abs(endX - startX) * 0.5); // Account for control point offset
            var minX = Math.Min(startX, endX) - hitDistanceCanvas - curveMargin;
            var maxX = Math.Max(startX, endX) + hitDistanceCanvas + curveMargin;
            var minY = Math.Min(startY, endY) - hitDistanceCanvas - curveMargin;
            var maxY = Math.Max(startY, endY) + hitDistanceCanvas + curveMargin;
            
            if (canvasX < minX || canvasX > maxX || canvasY < minY || canvasY > maxY)
                continue;

            // Check distance to bezier curve
            if (IsPointNearBezier(canvasX, canvasY, startX, startY, endX, endY, hitDistanceCanvas))
            {
                return edge;
            }
        }

        return null;
    }

    private static double GetPortYCanvas(double nodeY, double nodeHeight, int portIndex, int totalPorts)
    {
        if (totalPorts == 1)
            return nodeY + nodeHeight / 2;

        var spacing = nodeHeight / (totalPorts + 1);
        return nodeY + spacing * (portIndex + 1);
    }

    private static bool IsPointNearBezier(double px, double py, double x1, double y1, double x2, double y2, double threshold)
    {
        // Calculate bezier control points (must match DrawEdge exactly)
        var dx = x2 - x1;
        var controlOffset = Math.Max(50, Math.Abs(dx) * 0.5);
        controlOffset = Math.Min(controlOffset, 150);

        var cp1x = x1 + controlOffset;
        var cp1y = y1;
        var cp2x = x2 - controlOffset;
        var cp2y = y2;

        var thresholdSq = threshold * threshold;

        // Use distance to closest point on curve instead of sampling
        // This is more accurate and handles all curve shapes
        return DistanceToQuadraticBezierSquared(px, py, x1, y1, cp1x, cp1y, cp2x, cp2y, x2, y2) <= thresholdSq;
    }

    /// <summary>
    /// Calculates the minimum squared distance from a point to a cubic bezier curve.
    /// Uses subdivision for accuracy.
    /// </summary>
    private static double DistanceToQuadraticBezierSquared(
        double px, double py, 
        double x1, double y1, 
        double cp1x, double cp1y, 
        double cp2x, double cp2y, 
        double x2, double y2)
    {
        double minDistSq = double.MaxValue;
        
        // Sample the curve at many points and find minimum distance
        // Use 50 samples for good accuracy
        const int samples = 50;
        for (int i = 0; i <= samples; i++)
        {
            double t = i / (double)samples;
            var bx = BezierPoint(t, x1, cp1x, cp2x, x2);
            var by = BezierPoint(t, y1, cp1y, cp2y, y2);
            
            var distSq = (px - bx) * (px - bx) + (py - by) * (py - by);
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
            }
        }
        
        return minDistSq;
    }

    private static double BezierPoint(double t, double p0, double p1, double p2, double p3)
    {
        var mt = 1 - t;
        return mt * mt * mt * p0 + 3 * mt * mt * t * p1 + 3 * mt * t * t * p2 + t * t * t * p3;
    }

    private void DrawGroup(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
    {
        var width = (group.Width ?? MinGroupWidth) * zoom;
        var height = (group.Height ?? MinGroupHeight) * zoom;
        var x = group.Position.X * zoom + offsetX;
        var y = group.Position.Y * zoom + offsetY;

        var rect = new Rect(x, y, width, height);
        var cornerRadius = GroupBorderRadius * zoom;

        // Background fill - use theme's group background (translucent)
        var groupBackground = _theme!.GroupBackground;

        // Draw rounded rectangle background
        var bgGeometry = new StreamGeometry();
        using (var ctx = bgGeometry.Open())
        {
            DrawRoundedRect(ctx, rect, cornerRadius);
        }
        context.DrawGeometry(groupBackground, null, bgGeometry);

        // Border - dashed when not selected, solid when selected
        var borderBrush = group.IsSelected ? _theme.NodeSelectedBorder : _theme.GroupBorder;
        var borderPen = new Pen(borderBrush, GroupDashedStrokeThickness * zoom);
        
        if (!group.IsSelected)
        {
            // Dashed border
            borderPen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
        }

        // Draw border
        var borderGeometry = new StreamGeometry();
        using (var ctx = borderGeometry.Open())
        {
            DrawRoundedRect(ctx, rect, cornerRadius);
        }
        context.DrawGeometry(null, borderPen, borderGeometry);

        // Header area
        var headerMarginX = 8 * zoom;
        var headerMarginY = 6 * zoom;

        // Collapse/expand button
        var buttonSize = GroupCollapseButtonSize * zoom;
        var buttonX = x + headerMarginX;
        var buttonY = y + headerMarginY;
        var buttonRect = new Rect(buttonX, buttonY, buttonSize, buttonSize);
        
        var buttonBrush = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
        var buttonPen = new Pen(Brushes.Transparent, 0);
        
        context.DrawRectangle(buttonBrush, buttonPen, buttonRect, 3 * zoom, 3 * zoom);
        
        // Draw collapse indicator (- for expanded, + for collapsed)
        var indicatorText = group.IsCollapsed ? "+" : "-";
        var indicatorFontSize = 12 * zoom;
        var indicatorFormatted = new FormattedText(
            indicatorText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
            indicatorFontSize,
            _theme.GroupLabelText);
        
        var indicatorX = buttonX + (buttonSize - indicatorFormatted.Width) / 2;
        var indicatorY = buttonY + (buttonSize - indicatorFormatted.Height) / 2;
        context.DrawText(indicatorFormatted, new AvaloniaPoint(indicatorX, indicatorY));

        // Draw group label
        var label = group.Label ?? "Group";
        if (!string.IsNullOrEmpty(label))
        {
            var fontSize = 11 * zoom;
            var formattedText = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Medium, FontStretch.Normal),
                fontSize,
                _theme.GroupLabelText);

            // Label position: right of button with some margin
            var textX = buttonX + buttonSize + 4 * zoom;
            var textY = y + headerMarginY + (buttonSize - formattedText.Height) / 2;
            
            // Apply slight transparency like the normal renderer (Opacity = 0.9)
            formattedText.SetForegroundBrush(new SolidColorBrush(
                ((SolidColorBrush)_theme.GroupLabelText).Color, 0.9));
            
            context.DrawText(formattedText, new AvaloniaPoint(textX, textY));
        }

        // Draw group ports (groups can have external connection ports)
        var portSize = _settings.PortSize * zoom;

        // Input ports (left side of group)
        for (int i = 0; i < group.Inputs.Count; i++)
        {
            var portY = GetPortY(y, height, i, group.Inputs.Count);
            var portX = x;
            context.DrawEllipse(_portBrush, _portPen, new AvaloniaPoint(portX, portY), portSize / 2, portSize / 2);
        }

        // Output ports (right side of group)
        for (int i = 0; i < group.Outputs.Count; i++)
        {
            var portY = GetPortY(y, height, i, group.Outputs.Count);
            var portX = x + width;
            context.DrawEllipse(_portBrush, _portPen, new AvaloniaPoint(portX, portY), portSize / 2, portSize / 2);
        }
    }

    private void DrawCollapsedGroup(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
    {
        // Collapsed groups render with HeaderHeight only
        var width = (group.Width ?? MinGroupWidth) * zoom;
        var height = GroupHeaderHeight * zoom;
        var x = group.Position.X * zoom + offsetX;
        var y = group.Position.Y * zoom + offsetY;

        var rect = new Rect(x, y, width, height);
        var cornerRadius = GroupBorderRadius * zoom;

        // Background fill
        var groupBackground = _theme!.GroupBackground;
        var bgGeometry = new StreamGeometry();
        using (var ctx = bgGeometry.Open())
        {
            DrawRoundedRect(ctx, rect, cornerRadius);
        }
        context.DrawGeometry(groupBackground, null, bgGeometry);

        // Border - dashed when not selected
        var borderBrush = group.IsSelected ? _theme.NodeSelectedBorder : _theme.GroupBorder;
        var borderPen = new Pen(borderBrush, GroupDashedStrokeThickness * zoom);
        
        if (!group.IsSelected)
        {
            borderPen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
        }

        var borderGeometry = new StreamGeometry();
        using (var ctx = borderGeometry.Open())
        {
            DrawRoundedRect(ctx, rect, cornerRadius);
        }
        context.DrawGeometry(null, borderPen, borderGeometry);

        // Header area
        var headerMarginX = 8 * zoom;
        var headerMarginY = 6 * zoom;

        // Collapse/expand button
        var buttonSize = GroupCollapseButtonSize * zoom;
        var buttonX = x + headerMarginX;
        var buttonY = y + headerMarginY;
        var buttonRect = new Rect(buttonX, buttonY, buttonSize, buttonSize);
        
        var buttonBrush = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
        context.DrawRectangle(buttonBrush, null, buttonRect, 3 * zoom, 3 * zoom);
        
        // Draw expand indicator (+)
        var indicatorFontSize = 12 * zoom;
        var indicatorFormatted = new FormattedText(
            "+",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
            indicatorFontSize,
            _theme.GroupLabelText);
        
        var indicatorX = buttonX + (buttonSize - indicatorFormatted.Width) / 2;
        var indicatorY = buttonY + (buttonSize - indicatorFormatted.Height) / 2;
        context.DrawText(indicatorFormatted, new AvaloniaPoint(indicatorX, indicatorY));

        // Draw group label
        var label = group.Label ?? "Group";
        var fontSize = 11 * zoom;
        var formattedText = new FormattedText(
            label,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Medium, FontStretch.Normal),
            fontSize,
            _theme.GroupLabelText);

        var textX = buttonX + buttonSize + 4 * zoom;
        var textY = y + headerMarginY + (buttonSize - formattedText.Height) / 2;
        
        formattedText.SetForegroundBrush(new SolidColorBrush(
            ((SolidColorBrush)_theme.GroupLabelText).Color, 0.9));
        
        context.DrawText(formattedText, new AvaloniaPoint(textX, textY));

        // Draw group ports (even when collapsed, groups may have external ports)
        var portSize = _settings.PortSize * zoom;

        // Input ports (left side)
        for (int i = 0; i < group.Inputs.Count; i++)
        {
            var portY = GetPortY(y, height, i, group.Inputs.Count);
            var portX = x;
            context.DrawEllipse(_portBrush, _portPen, new AvaloniaPoint(portX, portY), portSize / 2, portSize / 2);
        }

        // Output ports (right side)
        for (int i = 0; i < group.Outputs.Count; i++)
        {
            var portY = GetPortY(y, height, i, group.Outputs.Count);
            var portX = x + width;
            context.DrawEllipse(_portBrush, _portPen, new AvaloniaPoint(portX, portY), portSize / 2, portSize / 2);
        }
    }

    #endregion
}
