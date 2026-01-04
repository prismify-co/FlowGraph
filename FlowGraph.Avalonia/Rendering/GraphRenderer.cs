using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Renders nodes, ports, and edges on the canvas.
/// </summary>
public class GraphRenderer
{
    private readonly FlowCanvasSettings _settings;
    private readonly Dictionary<string, Control> _nodeVisuals = new();
    private readonly Dictionary<(string nodeId, string portId), Ellipse> _portVisuals = new();
    private readonly Dictionary<string, AvaloniaPath> _edgeVisuals = new();  // Hit area paths
    private readonly Dictionary<string, AvaloniaPath> _edgeVisiblePaths = new();  // Visible paths
    private readonly Dictionary<string, List<Rectangle>> _resizeHandles = new();  // Resize handles per node
    
    // Current viewport state for transforming positions
    private ViewportState? _viewport;
    
    // Node renderer registry for custom node types
    private readonly NodeRendererRegistry _nodeRendererRegistry;

    public GraphRenderer(FlowCanvasSettings? settings = null)
        : this(settings, null)
    {
    }

    public GraphRenderer(FlowCanvasSettings? settings, NodeRendererRegistry? nodeRendererRegistry)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
        _nodeRendererRegistry = nodeRendererRegistry ?? new NodeRendererRegistry();
    }

    /// <summary>
    /// Gets the node renderer registry for registering custom node types.
    /// </summary>
    public NodeRendererRegistry NodeRenderers => _nodeRendererRegistry;

    /// <summary>
    /// Sets the viewport state to use for coordinate transformations.
    /// </summary>
    public void SetViewport(ViewportState? viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Transforms a canvas coordinate to screen coordinate.
    /// </summary>
    private AvaloniaPoint TransformToScreen(double canvasX, double canvasY)
    {
        if (_viewport == null)
        {
            if (_settings.DebugCoordinateTransforms)
            {
                System.Diagnostics.Debug.WriteLine($"TransformToScreen: NO VIEWPORT! Returning ({canvasX}, {canvasY})");
            }
            return new AvaloniaPoint(canvasX, canvasY);
        }
        
        var result = _viewport.CanvasToScreen(new AvaloniaPoint(canvasX, canvasY));
        
        if (_settings.DebugCoordinateTransforms)
        {
            System.Diagnostics.Debug.WriteLine($"TransformToScreen: ({canvasX}, {canvasY}) -> ({result.X}, {result.Y}) [zoom={_viewport.Zoom}, offset=({_viewport.OffsetX}, {_viewport.OffsetY})]");
        }
        
        return result;
    }

    /// <summary>
    /// Gets the current scale factor for sizing elements.
    /// </summary>
    private double GetScale() => _viewport?.Zoom ?? 1.0;

    /// <summary>
    /// Gets the visual element for a node.
    /// </summary>
    public Control? GetNodeVisual(string nodeId)
    {
        return _nodeVisuals.TryGetValue(nodeId, out var control) ? control : null;
    }

    /// <summary>
    /// Gets the visual element for a port.
    /// </summary>
    public Ellipse? GetPortVisual(string nodeId, string portId)
    {
        return _portVisuals.TryGetValue((nodeId, portId), out var ellipse) ? ellipse : null;
    }

    /// <summary>
    /// Gets the visual element for an edge.
    /// </summary>
    public AvaloniaPath? GetEdgeVisual(string edgeId)
    {
        return _edgeVisuals.TryGetValue(edgeId, out var path) ? path : null;
    }

    /// <summary>
    /// Clears all rendered visuals.
    /// </summary>
    public void Clear()
    {
        _nodeVisuals.Clear();
        _portVisuals.Clear();
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
        _resizeHandles.Clear();
    }

    /// <summary>
    /// Renders all nodes in the graph.
    /// Groups are rendered first (behind), then regular nodes.
    /// </summary>
    public void RenderNodes(
        Canvas canvas, 
        Graph graph, 
        ThemeResources theme,
        Action<Control, Node> onNodeCreated)
    {
        // Render groups first (they should be behind their children)
        // Order by hierarchy depth - outermost groups first
        var groups = graph.Nodes
            .Where(n => n.IsGroup)
            .OrderBy(n => GetGroupDepth(graph, n))
            .ToList();

        foreach (var group in groups)
        {
            RenderNode(canvas, group, theme, onNodeCreated);
        }

        // Then render non-group nodes
        foreach (var node in graph.Nodes.Where(n => !n.IsGroup))
        {
            RenderNode(canvas, node, theme, onNodeCreated);
        }
    }

    /// <summary>
    /// Gets the nesting depth of a group (0 = top level).
    /// </summary>
    private int GetGroupDepth(Graph graph, Node node)
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
    /// Renders a single node using the appropriate renderer for its type.
    /// </summary>
    public Control RenderNode(
        Canvas canvas, 
        Node node, 
        ThemeResources theme,
        Action<Control, Node>? onNodeCreated = null)
    {
        var scale = GetScale();
        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        
        var context = new NodeRenderContext
        {
            Theme = theme,
            Settings = _settings,
            Scale = scale
        };

        // Create the node visual using the renderer
        var control = renderer.CreateNodeVisual(node, context);
        control.Tag = node;

        // Transform position to screen coordinates
        var screenPos = TransformToScreen(node.Position.X, node.Position.Y);
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
        var scale = GetScale();
        var scaledPortSize = _settings.PortSize * scale;
        
        // Get the node dimensions (may be custom per node type)
        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
        
        // Calculate port position in canvas coordinates
        var portY = GetPortYCanvas(node.Position.Y, index, totalPorts, nodeHeight);
        var portX = isOutput ? node.Position.X + nodeWidth : node.Position.X;
        
        // Transform to screen coordinates
        var screenPos = TransformToScreen(portX, portY);

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
    /// Gets the dimensions for a node, considering custom renderer sizes and node-specific overrides.
    /// </summary>
    public (double width, double height) GetNodeDimensions(Node node)
    {
        // First check if the node has explicit dimensions
        if (node.Width.HasValue && node.Height.HasValue)
        {
            return (node.Width.Value, node.Height.Value);
        }

        // Fall back to renderer-specified or default dimensions
        var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
        var width = node.Width ?? renderer.GetWidth(node, _settings) ?? _settings.NodeWidth;
        var height = node.Height ?? renderer.GetHeight(node, _settings) ?? _settings.NodeHeight;
        return (width, height);
    }

    /// <summary>
    /// Renders all edges in the graph.
    /// </summary>
    public void RenderEdges(Canvas canvas, Graph graph, ThemeResources theme, AvaloniaPath? excludePath = null)
    {
        // Clear edge visuals dictionaries
        _edgeVisuals.Clear();
        _edgeVisiblePaths.Clear();
        
        // Remove existing edges, markers, labels, and hit areas
        var elementsToRemove = canvas.Children
            .Where(c => 
                (c is AvaloniaPath p && p != excludePath && p.Tag is string tag && (tag == "edge" || tag == "marker")) ||
                (c is AvaloniaPath p2 && p2 != excludePath && p2.Tag is Edge) ||
                (c is AvaloniaPath p3 && p3 != excludePath && !p3.IsHitTestVisible && p3.Tag == null) ||  // Visible paths with no hit test
                (c is TextBlock tb && tb.Tag is string tbTag && tbTag == "edgeLabel"))
            .ToList();
        
        foreach (var element in elementsToRemove)
        {
            canvas.Children.Remove(element);
        }
        
        // Also remove old edges without tags (backward compatibility)
        var oldEdges = canvas.Children.OfType<AvaloniaPath>()
            .Where(p => p != excludePath && p.Tag == null && p.IsHitTestVisible)
            .ToList();
        foreach (var edge in oldEdges)
        {
            canvas.Children.Remove(edge);
        }

        // Render new edges
        foreach (var edge in graph.Edges)
        {
            RenderEdge(canvas, edge, graph, theme);
        }
    }

    /// <summary>
    /// Renders a single edge with its markers and optional label.
    /// </summary>
    public AvaloniaPath? RenderEdge(Canvas canvas, Edge edge, Graph graph, ThemeResources theme)
    {
        var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return null;

        var sourcePortIndex = sourceNode.Outputs.FindIndex(p => p.Id == edge.SourcePort);
        var targetPortIndex = targetNode.Inputs.FindIndex(p => p.Id == edge.TargetPort);

        if (sourcePortIndex < 0) sourcePortIndex = 0;
        if (targetPortIndex < 0) targetPortIndex = 0;

        // Get node dimensions for proper port positioning
        var (sourceWidth, sourceHeight) = GetNodeDimensions(sourceNode);
        var (_, targetHeight) = GetNodeDimensions(targetNode);

        // Get canvas coordinates
        var sourceY = GetPortYCanvas(sourceNode.Position.Y, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count), sourceHeight);
        var targetY = GetPortYCanvas(targetNode.Position.Y, targetPortIndex, Math.Max(1, targetNode.Inputs.Count), targetHeight);
        var sourceX = sourceNode.Position.X + sourceWidth;
        var targetX = targetNode.Position.X;

        // Transform to screen coordinates
        var startPoint = TransformToScreen(sourceX, sourceY);
        var endPoint = TransformToScreen(targetX, targetY);

        var scale = GetScale();
        
        // Create path based on edge type
        var pathGeometry = EdgePathHelper.CreatePath(startPoint, endPoint, edge.Type);

        var strokeBrush = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;
        
        // Create invisible hit area path (wider, transparent stroke for easier clicking)
        var hitAreaPath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = Brushes.Transparent,
            StrokeThickness = _settings.EdgeHitAreaWidth * scale,
            Tag = edge,  // Store the Edge object for click detection
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        // Create visible edge path
        var visiblePath = new AvaloniaPath
        {
            Data = pathGeometry,
            Stroke = strokeBrush,
            StrokeThickness = (edge.IsSelected ? 3 : 2) * scale,
            IsHitTestVisible = false  // Hit testing is handled by the hit area path
        };

        // Insert visible path first (at bottom), then hit area on top
        canvas.Children.Insert(0, visiblePath);
        canvas.Children.Insert(1, hitAreaPath);
        
        // Track both paths - we use the hit area path for events, but need to update the visible path
        _edgeVisuals[edge.Id] = hitAreaPath;
        _edgeVisiblePaths[edge.Id] = visiblePath;

        // Render end marker (arrow)
        if (edge.MarkerEnd != EdgeMarker.None)
        {
            RenderEdgeMarker(canvas, endPoint, startPoint, edge.MarkerEnd, strokeBrush, scale);
        }

        // Render start marker
        if (edge.MarkerStart != EdgeMarker.None)
        {
            RenderEdgeMarker(canvas, startPoint, endPoint, edge.MarkerStart, strokeBrush, scale);
        }

        // Render label if present
        if (!string.IsNullOrEmpty(edge.Label))
        {
            RenderEdgeLabel(canvas, startPoint, endPoint, edge.Label, theme, scale);
        }

        return hitAreaPath;
    }

    /// <summary>
    /// Renders a marker (arrow) at an edge endpoint.
    /// </summary>
    private void RenderEdgeMarker(Canvas canvas, AvaloniaPoint point, AvaloniaPoint fromPoint, EdgeMarker marker, IBrush stroke, double scale)
    {
        var angle = EdgePathHelper.CalculateAngle(fromPoint, point);
        var markerSize = 10 * scale;
        var isClosed = marker == EdgeMarker.ArrowClosed;
        
        var markerGeometry = EdgePathHelper.CreateArrowMarker(point, angle, markerSize, isClosed);
        
        var markerPath = new AvaloniaPath
        {
            Data = markerGeometry,
            Stroke = stroke,
            StrokeThickness = 2 * scale,
            Fill = isClosed ? stroke : null,
            Tag = "marker"
        };

        canvas.Children.Insert(0, markerPath);
    }

    /// <summary>
    /// Renders a label on an edge.
    /// </summary>
    private void RenderEdgeLabel(Canvas canvas, AvaloniaPoint start, AvaloniaPoint end, string label, ThemeResources theme, double scale)
    {
        var midPoint = new AvaloniaPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
        
        var textBlock = new TextBlock
        {
            Text = label,
            FontSize = 12 * scale,
            Foreground = theme.NodeText,
            Background = theme.NodeBackground,
            Padding = new Thickness(4 * scale, 2 * scale, 4 * scale, 2 * scale),
            Tag = "edgeLabel"  // Tag for cleanup
        };

        // Position at midpoint (slightly offset up)
        Canvas.SetLeft(textBlock, midPoint.X);
        Canvas.SetTop(textBlock, midPoint.Y - 10 * scale);

        canvas.Children.Add(textBlock);
    }

    /// <summary>
    /// Updates the position of a node visual.
    /// </summary>
    public void UpdateNodePosition(Node node)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var border))
        {
            var screenPos = TransformToScreen(node.Position.X, node.Position.Y);
            Canvas.SetLeft(border, screenPos.X);
            Canvas.SetTop(border, screenPos.Y);
        }

        UpdatePortPositions(node);
    }

    /// <summary>
    /// Updates port positions for a node.
    /// </summary>
    public void UpdatePortPositions(Node node)
    {
        var scale = GetScale();
        var scaledPortSize = _settings.PortSize * scale;
        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
        
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var portY = GetPortYCanvas(node.Position.Y, i, node.Inputs.Count, nodeHeight);
                var screenPos = TransformToScreen(node.Position.X, portY);
                Canvas.SetLeft(portVisual, screenPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, screenPos.Y - scaledPortSize / 2);
            }
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var port = node.Outputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var portY = GetPortYCanvas(node.Position.Y, i, node.Outputs.Count, nodeHeight);
                var screenPos = TransformToScreen(node.Position.X + nodeWidth, portY);
                Canvas.SetLeft(portVisual, screenPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, screenPos.Y - scaledPortSize / 2);
            }
        }
    }

    /// <summary>
    /// Updates the selection visual of a node.
    /// </summary>
    public void UpdateNodeSelection(Node node, ThemeResources theme)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var scale = GetScale();
            var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
            var context = new NodeRenderContext
            {
                Theme = theme,
                Settings = _settings,
                Scale = scale
            };
            
            renderer.UpdateSelection(control, node, context);
        }
    }

    /// <summary>
    /// Updates the size of a node visual.
    /// </summary>
    public void UpdateNodeSize(Node node, ThemeResources theme)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var scale = GetScale();
            var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
            var context = new NodeRenderContext
            {
                Theme = theme,
                Settings = _settings,
                Scale = scale
            };
            
            var (width, height) = GetNodeDimensions(node);
            renderer.UpdateSize(control, node, context, width, height);
        }
    }

    /// <summary>
    /// Renders resize handles for a selected node.
    /// </summary>
    public void RenderResizeHandles(
        Canvas canvas, 
        Node node, 
        ThemeResources theme,
        Action<Rectangle, Node, ResizeHandlePosition>? onHandleCreated = null)
    {
        // Remove existing handles for this node
        RemoveResizeHandles(canvas, node.Id);

        if (!node.IsSelected || !node.IsResizable)
            return;

        var scale = GetScale();
        var handleSize = 8 * scale;
        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
        var screenPos = TransformToScreen(node.Position.X, node.Position.Y);
        var scaledWidth = nodeWidth * scale;
        var scaledHeight = nodeHeight * scale;

        var handles = new List<Rectangle>();
        var positions = new[]
        {
            ResizeHandlePosition.TopLeft,
            ResizeHandlePosition.TopRight,
            ResizeHandlePosition.BottomLeft,
            ResizeHandlePosition.BottomRight,
            ResizeHandlePosition.Top,
            ResizeHandlePosition.Bottom,
            ResizeHandlePosition.Left,
            ResizeHandlePosition.Right
        };

        foreach (var position in positions)
        {
            var handle = CreateResizeHandle(handleSize, theme, node, position);
            PositionResizeHandle(handle, screenPos, scaledWidth, scaledHeight, handleSize, position);
            
            canvas.Children.Add(handle);
            handles.Add(handle);
            
            onHandleCreated?.Invoke(handle, node, position);
        }

        _resizeHandles[node.Id] = handles;
    }

    /// <summary>
    /// Removes resize handles for a node.
    /// </summary>
    public void RemoveResizeHandles(Canvas canvas, string nodeId)
    {
        if (_resizeHandles.TryGetValue(nodeId, out var handles))
        {
            foreach (var handle in handles)
            {
                canvas.Children.Remove(handle);
            }
            _resizeHandles.Remove(nodeId);
        }
    }

    /// <summary>
    /// Removes all resize handles from the canvas.
    /// </summary>
    public void RemoveAllResizeHandles(Canvas canvas)
    {
        foreach (var (nodeId, handles) in _resizeHandles)
        {
            foreach (var handle in handles)
            {
                canvas.Children.Remove(handle);
            }
        }
        _resizeHandles.Clear();
    }

    /// <summary>
    /// Updates the position of resize handles for a node.
    /// </summary>
    public void UpdateResizeHandlePositions(Node node)
    {
        if (!_resizeHandles.TryGetValue(node.Id, out var handles))
            return;

        var scale = GetScale();
        var handleSize = 8 * scale;
        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
        var screenPos = TransformToScreen(node.Position.X, node.Position.Y);
        var scaledWidth = nodeWidth * scale;
        var scaledHeight = nodeHeight * scale;

        foreach (var handle in handles)
        {
            if (handle.Tag is (Node _, ResizeHandlePosition position))
            {
                PositionResizeHandle(handle, screenPos, scaledWidth, scaledHeight, handleSize, position);
            }
        }
    }

    private Rectangle CreateResizeHandle(double size, ThemeResources theme, Node node, ResizeHandlePosition position)
    {
        var cursor = position switch
        {
            ResizeHandlePosition.TopLeft or ResizeHandlePosition.BottomRight => StandardCursorType.TopLeftCorner,
            ResizeHandlePosition.TopRight or ResizeHandlePosition.BottomLeft => StandardCursorType.TopRightCorner,
            ResizeHandlePosition.Top or ResizeHandlePosition.Bottom => StandardCursorType.SizeNorthSouth,
            ResizeHandlePosition.Left or ResizeHandlePosition.Right => StandardCursorType.SizeWestEast,
            _ => StandardCursorType.Arrow
        };

        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = theme.NodeSelectedBorder,
            Stroke = Brushes.White,
            StrokeThickness = 1,
            Cursor = new Cursor(cursor),
            Tag = (node, position)
        };
    }

    private void PositionResizeHandle(
        Rectangle handle, 
        AvaloniaPoint nodeScreenPos, 
        double scaledWidth, 
        double scaledHeight, 
        double handleSize,
        ResizeHandlePosition position)
    {
        var halfHandle = handleSize / 2;
        
        var (left, top) = position switch
        {
            ResizeHandlePosition.TopLeft => (nodeScreenPos.X - halfHandle, nodeScreenPos.Y - halfHandle),
            ResizeHandlePosition.TopRight => (nodeScreenPos.X + scaledWidth - halfHandle, nodeScreenPos.Y - halfHandle),
            ResizeHandlePosition.BottomLeft => (nodeScreenPos.X - halfHandle, nodeScreenPos.Y + scaledHeight - halfHandle),
            ResizeHandlePosition.BottomRight => (nodeScreenPos.X + scaledWidth - halfHandle, nodeScreenPos.Y + scaledHeight - halfHandle),
            ResizeHandlePosition.Top => (nodeScreenPos.X + scaledWidth / 2 - halfHandle, nodeScreenPos.Y - halfHandle),
            ResizeHandlePosition.Bottom => (nodeScreenPos.X + scaledWidth / 2 - halfHandle, nodeScreenPos.Y + scaledHeight - halfHandle),
            ResizeHandlePosition.Left => (nodeScreenPos.X - halfHandle, nodeScreenPos.Y + scaledHeight / 2 - halfHandle),
            ResizeHandlePosition.Right => (nodeScreenPos.X + scaledWidth - halfHandle, nodeScreenPos.Y + scaledHeight / 2 - halfHandle),
            _ => (0.0, 0.0)
        };

        Canvas.SetLeft(handle, left);
        Canvas.SetTop(handle, top);
    }

    /// <summary>
    /// Updates the selection visual of an edge.
    /// </summary>
    public void UpdateEdgeSelection(Edge edge, ThemeResources theme)
    {
        // Update the visible path (the one that shows the stroke)
        if (_edgeVisiblePaths.TryGetValue(edge.Id, out var visiblePath))
        {
            var scale = GetScale();
            visiblePath.Stroke = edge.IsSelected ? theme.NodeSelectedBorder : theme.EdgeStroke;
            visiblePath.StrokeThickness = (edge.IsSelected ? 3 : 2) * scale;
        }
    }

    /// <summary>
    /// Calculates the Y position for a port in canvas coordinates.
    /// </summary>
    public double GetPortYCanvas(double nodeY, int portIndex, int totalPorts, double? nodeHeight = null)
    {
        var height = nodeHeight ?? _settings.NodeHeight;
        
        if (totalPorts == 1)
        {
            return nodeY + height / 2;
        }

        var spacing = height / (totalPorts + 1);
        return nodeY + spacing * (portIndex + 1);
    }

    /// <summary>
    /// Calculates the Y position for a port (legacy, returns canvas coordinates).
    /// </summary>
    public double GetPortY(double nodeY, int portIndex, int totalPorts)
    {
        return GetPortYCanvas(nodeY, portIndex, totalPorts);
    }

    /// <summary>
    /// Gets the source point for a connection from a node/port in screen coordinates.
    /// </summary>
    public AvaloniaPoint GetPortPosition(Node node, Port port, bool isOutput)
    {
        var portIndex = isOutput
            ? node.Outputs.IndexOf(port)
            : node.Inputs.IndexOf(port);
        var totalPorts = isOutput ? node.Outputs.Count : node.Inputs.Count;

        var (nodeWidth, nodeHeight) = GetNodeDimensions(node);
        var portY = GetPortYCanvas(node.Position.Y, portIndex, totalPorts, nodeHeight);
        var portX = isOutput ? node.Position.X + nodeWidth : node.Position.X;

        // Return screen coordinates
        return TransformToScreen(portX, portY);
    }
}
