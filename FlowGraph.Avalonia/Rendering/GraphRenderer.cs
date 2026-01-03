using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
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
    private readonly Dictionary<string, Border> _nodeVisuals = new();
    private readonly Dictionary<(string nodeId, string portId), Ellipse> _portVisuals = new();
    private readonly Dictionary<string, AvaloniaPath> _edgeVisuals = new();  // Hit area paths
    private readonly Dictionary<string, AvaloniaPath> _edgeVisiblePaths = new();  // Visible paths
    
    // Current viewport state for transforming positions
    private ViewportState? _viewport;

    public GraphRenderer(FlowCanvasSettings? settings = null)
    {
        _settings = settings ?? FlowCanvasSettings.Default;
    }

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
    public Border? GetNodeVisual(string nodeId)
    {
        return _nodeVisuals.TryGetValue(nodeId, out var border) ? border : null;
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
    }

    /// <summary>
    /// Renders all nodes in the graph.
    /// </summary>
    public void RenderNodes(
        Canvas canvas, 
        Graph graph, 
        ThemeResources theme,
        Action<Border, Node> onNodeCreated)
    {
        foreach (var node in graph.Nodes)
        {
            RenderNode(canvas, node, theme, onNodeCreated);
        }
    }

    /// <summary>
    /// Renders a single node.
    /// </summary>
    public Border RenderNode(
        Canvas canvas, 
        Node node, 
        ThemeResources theme,
        Action<Border, Node>? onNodeCreated = null)
    {
        var nodeBackground = theme.NodeBackground;
        var nodeBorder = node.IsSelected ? theme.NodeSelectedBorder : theme.NodeBorder;
        var nodeText = theme.NodeText;
        
        var scale = GetScale();
        var scaledWidth = _settings.NodeWidth * scale;
        var scaledHeight = _settings.NodeHeight * scale;

        var border = new Border
        {
            Width = scaledWidth,
            Height = scaledHeight,
            Background = nodeBackground,
            BorderBrush = nodeBorder,
            BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2),
            CornerRadius = new CornerRadius(8 * scale),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2 * scale,
                OffsetY = 2 * scale,
                Blur = 8 * scale,
                Color = Color.FromArgb(60, 0, 0, 0)
            }),
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = node,
            Child = new TextBlock
            {
                Text = $"{node.Type}\n{node.Id[..8]}",
                Foreground = nodeText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeight.Medium,
                FontSize = 14 * scale,
                IsHitTestVisible = false
            }
        };

        // Transform position to screen coordinates
        var screenPos = TransformToScreen(node.Position.X, node.Position.Y);
        Canvas.SetLeft(border, screenPos.X);
        Canvas.SetTop(border, screenPos.Y);

        canvas.Children.Add(border);
        _nodeVisuals[node.Id] = border;

        onNodeCreated?.Invoke(border, node);

        // Render ports
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            RenderPort(canvas, node, node.Inputs[i], i, node.Inputs.Count, false, theme);
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            RenderPort(canvas, node, node.Outputs[i], i, node.Outputs.Count, true, theme);
        }

        return border;
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
        var scaledNodeWidth = _settings.NodeWidth * scale;
        var scaledNodeHeight = _settings.NodeHeight * scale;
        var scaledPortSize = _settings.PortSize * scale;
        
        // Calculate port position in canvas coordinates
        var portY = GetPortYCanvas(node.Position.Y, index, totalPorts);
        var portX = isOutput ? node.Position.X + _settings.NodeWidth : node.Position.X;
        
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

        // Get canvas coordinates
        var sourceY = GetPortYCanvas(sourceNode.Position.Y, sourcePortIndex, Math.Max(1, sourceNode.Outputs.Count));
        var targetY = GetPortYCanvas(targetNode.Position.Y, targetPortIndex, Math.Max(1, targetNode.Inputs.Count));
        var sourceX = sourceNode.Position.X + _settings.NodeWidth;
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
        
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var portY = GetPortYCanvas(node.Position.Y, i, node.Inputs.Count);
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
                var portY = GetPortYCanvas(node.Position.Y, i, node.Outputs.Count);
                var screenPos = TransformToScreen(node.Position.X + _settings.NodeWidth, portY);
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
        if (_nodeVisuals.TryGetValue(node.Id, out var border))
        {
            border.BorderBrush = node.IsSelected ? theme.NodeSelectedBorder : theme.NodeBorder;
            border.BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2);
        }
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
    /// Renders the selection indicator for a collection of nodes.
    /// </summary>
    public void RenderSelection(Canvas canvas, IEnumerable<Node> selectedNodes, ThemeResources theme)
    {
        // Remove old selection indicators
        var oldIndicators = canvas.Children.OfType<Rectangle>().Where(r => r.Tag is string tag && tag == "selectionIndicator").ToList();
        foreach (var indicator in oldIndicators)
        {
            canvas.Children.Remove(indicator);
        }

        // Render new indicators
        foreach (var node in selectedNodes)
        {
            if (_nodeVisuals.TryGetValue(node.Id, out var nodeBorder))
            {
                var indicator = new Rectangle
                {
                    Stroke = theme.NodeSelectedBorder,
                    StrokeThickness = 2,
                    Tag = "selectionIndicator"
                };

                // Position and size the indicator
                Canvas.SetLeft(indicator, Canvas.GetLeft(nodeBorder) - 2);
                Canvas.SetTop(indicator, Canvas.GetTop(nodeBorder) - 2);
                indicator.Width = nodeBorder.Width + 4;
                indicator.Height = nodeBorder.Height + 4;

                canvas.Children.Add(indicator);
            }
        }
    }

    /// <summary>
    /// Calculates the Y position for a port in canvas coordinates.
    /// </summary>
    public double GetPortYCanvas(double nodeY, int portIndex, int totalPorts)
    {
        if (totalPorts == 1)
        {
            return nodeY + _settings.NodeHeight / 2;
        }

        var spacing = _settings.NodeHeight / (totalPorts + 1);
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

        var portY = GetPortYCanvas(node.Position.Y, portIndex, totalPorts);
        var portX = isOutput ? node.Position.X + _settings.NodeWidth : node.Position.X;

        // Return screen coordinates
        return TransformToScreen(portX, portY);
    }
}
