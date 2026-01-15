using Avalonia;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// DirectGraphRenderer partial - Hit testing methods.
/// </summary>
public partial class DirectGraphRenderer
{
    /// <summary>
    /// Performs hit testing to find an edge endpoint handle at the given screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <returns>Tuple of (edge, isSource) or null if no handle hit.</returns>
    public (Edge edge, bool isSource)? HitTestEdgeEndpointHandle(double screenX, double screenY)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (_graph == null || _viewport == null || !_settings.ShowEdgeEndpointHandles) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        var handleRadius = _settings.EdgeEndpointHandleSize / 2 + 4; // Extra padding for easier clicking
        // Use actual view dimensions, not Bounds which includes parent-relative position
        var viewBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        int edgesChecked = 0;
        int edgesSkippedViewport = 0;
        foreach (var edge in _graph.Elements.Edges)
        {
            if (!edge.IsSelected) continue;

            if (_nodeById == null) continue;
            if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) continue;
            if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) continue;

            if (!IsNodeVisibleFast(sourceNode) || !IsNodeVisibleFast(targetNode)) continue;

            // VIEWPORT CULLING: Skip edges where BOTH endpoints are outside viewport
            var sourceInViewport = IsInVisibleBounds(sourceNode, zoom, offsetX, offsetY, viewBounds);
            var targetInViewport = IsInVisibleBounds(targetNode, zoom, offsetX, offsetY, viewBounds);
            if (!sourceInViewport && !targetInViewport)
            {
                edgesSkippedViewport++;
                continue;
            }

            edgesChecked++;
            // Use optimized overload with pre-looked-up nodes
            var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);

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

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HitTestEdgeEndpointHandle] No hit in {sw.ElapsedMilliseconds}ms | EdgesChecked:{edgesChecked}, SkippedViewport:{edgesSkippedViewport}");
        return null;
    }

    /// <summary>
    /// Performs hit testing to find a resize handle at the given screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <returns>Tuple of (node, position) or null if no handle hit.</returns>
    public (Node node, ResizeHandlePosition position)? HitTestResizeHandle(double screenX, double screenY)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (_graph == null || _viewport == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        // Use actual view dimensions, not Bounds which includes parent-relative position
        var viewBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        int nodesChecked = 0;
        int nodesSkippedViewport = 0;
        foreach (var node in _graph.Elements.Nodes)
        {
            if (!node.IsSelected || !node.IsResizable) continue;
            if (!GraphRenderModel.IsNodeVisible(_graph, node)) continue;

            // VIEWPORT CULLING: Skip nodes outside visible area
            if (!IsInVisibleBounds(node, zoom, offsetX, offsetY, viewBounds))
            {
                nodesSkippedViewport++;
                continue;
            }

            nodesChecked++;
            foreach (var (pos, center) in _model.GetResizeHandlePositions(node))
            {
                if (_model.IsPointInResizeHandle(canvasPoint, center))
                {
                    return (node, pos);
                }
            }
        }

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HitTestResizeHandle] No hit in {sw.ElapsedMilliseconds}ms | NodesChecked:{nodesChecked}, SkippedViewport:{nodesSkippedViewport}");
        return null;
    }

    /// <summary>
    /// Performs hit testing to find a node at the given screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <returns>The hit node, or null if no node was hit.</returns>
    public Node? HitTestNode(double screenX, double screenY)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (_graph == null || _viewport == null) return null;

        long rebuildTime = 0;
        if (_indexDirty)
        {
            var rebuildSw = System.Diagnostics.Stopwatch.StartNew();
            RebuildSpatialIndex();
            rebuildTime = rebuildSw.ElapsedMilliseconds;
        }

        if (_nodeIndex == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);

        // Use actual view dimensions, not Bounds which includes parent-relative position
        var viewBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        // Check regular nodes first (they're on top)
        var regularCheckStart = sw.ElapsedMilliseconds;
        int nodesChecked = 0;
        int nodesSkippedViewport = 0;
        for (int i = _nodeIndex.Count - 1; i >= 0; i--)
        {
            var (node, nx, ny, nw, nh) = _nodeIndex[i];

            // VIEWPORT CULLING: Skip nodes outside visible area
            // This prevents clicking on off-screen nodes from blocking canvas interactions
            var screenX1 = nx * zoom + offsetX;
            var screenY1 = ny * zoom + offsetY;
            var screenW = nw * zoom;
            var screenH = nh * zoom;

            if (screenX1 + screenW < 0 || screenX1 > viewBounds.Width ||
                screenY1 + screenH < 0 || screenY1 > viewBounds.Height)
            {
                nodesSkippedViewport++;
                continue;
            }

            nodesChecked++;
            var bounds = new Rect(nx, ny, nw, nh);

            if (bounds.Contains(canvasPoint))
            {
                // AT LOW ZOOM: Check if the screen-pixel size is too small for accurate clicking
                // When nodes appear tiny (< 60px), require clicks closer to center
                const double MinClickableScreenSize = 60.0; // Below this, restrict clickable area
                const double MinCenterProximity = 0.4;      // At tiny sizes, only inner 40% is clickable

                var smallestScreenDim = Math.Min(screenW, screenH);

                // Calculate how far from center (as percentage of dimensions, 0=center, 1=edge)
                var centerX = nx + nw / 2;
                var centerY = ny + nh / 2;
                var distFromCenterX = Math.Abs(canvasPoint.X - centerX) / (nw / 2);
                var distFromCenterY = Math.Abs(canvasPoint.Y - centerY) / (nh / 2);

                // Scale clickable area based on screen pixel size
                // >= 60px screen size: full bounds clickable (maxDist = 1.0)
                // < 60px: linearly reduce from 1.0 to 0.4 as size approaches 0
                // This prevents accidental clicks on tiny nodes that appear as dots
                double maxDistFromCenter;
                if (smallestScreenDim >= MinClickableScreenSize)
                {
                    maxDistFromCenter = 1.0; // Full bounds clickable
                }
                else
                {
                    // Linear interpolation: at 60px=1.0, at 0px=0.4
                    var t = smallestScreenDim / MinClickableScreenSize; // 0 to 1
                    maxDistFromCenter = MinCenterProximity + t * (1.0 - MinCenterProximity);
                }

                if (distFromCenterX > maxDistFromCenter || distFromCenterY > maxDistFromCenter)
                {
                    // Click is too close to edge for this screen size - don't count as hit
                    System.Diagnostics.Debug.WriteLine($"[HitTest] Node {node.Id} skipped: click at edge ({distFromCenterX:P0},{distFromCenterY:P0}) > maxDist {maxDistFromCenter:P0} (screenSize={smallestScreenDim:F0}px)");
                    continue;
                }

                sw.Stop();
                // Log where the node is on screen for debugging
                System.Diagnostics.Debug.WriteLine($"[HitTest] Regular node found in {sw.ElapsedMilliseconds}ms (rebuild:{rebuildTime}ms, checked:{nodesChecked}, skippedViewport:{nodesSkippedViewport})");

                // Calculate screen distance from screen center
                var screenCenterX = screenX1 + screenW / 2;
                var screenCenterY = screenY1 + screenH / 2;
                var screenDistX = Math.Abs(screenX - screenCenterX);
                var screenDistY = Math.Abs(screenY - screenCenterY);

                System.Diagnostics.Debug.WriteLine($"[HitTest]   Node {node.Id}: canvasClick=({canvasPoint.X:F0},{canvasPoint.Y:F0}) nodeBounds=({nx:F0},{ny:F0},{nw:F0}x{nh:F0}) screenBounds=({screenX1:F0},{screenY1:F0},{screenW:F0}x{screenH:F0})");
                System.Diagnostics.Debug.WriteLine($"[HitTest]   DistFromCenter: X={distFromCenterX:P0} Y={distFromCenterY:P0} | ScreenDist: ({screenDistX:F0},{screenDistY:F0}) px from center");
                return node;
            }
        }
        var regularCheckTime = sw.ElapsedMilliseconds - regularCheckStart;

        // Check groups (they're behind regular nodes) - OPTIMIZED to avoid LINQ iteration
        var groupCheckStart = sw.ElapsedMilliseconds;
        int groupsChecked = 0;
        if (_nodeById != null)
        {
            foreach (var kvp in _nodeById)
            {
                var node = kvp.Value;
                if (!node.IsGroup) continue;

                groupsChecked++;
                if (!IsNodeVisibleFast(node)) continue;

                var bounds = _model.GetNodeBounds(node);
                if (bounds.Contains(canvasPoint))
                {
                    sw.Stop();
                    System.Diagnostics.Debug.WriteLine($"[HitTest] Group found in {sw.ElapsedMilliseconds}ms | Rebuild:{rebuildTime}ms, Regular:{regularCheckTime}ms, Groups:{sw.ElapsedMilliseconds - groupCheckStart}ms, GroupsChecked:{groupsChecked}");
                    return node;
                }
            }
        }
        var groupCheckTime = sw.ElapsedMilliseconds - groupCheckStart;

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HitTest] No hit in {sw.ElapsedMilliseconds}ms | Rebuild:{rebuildTime}ms, Regular:{regularCheckTime}ms, Groups:{groupCheckTime}ms, GroupsChecked:{groupsChecked}");
        return null;
    }

    /// <summary>
    /// Performs hit testing to find a port at the given screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <returns>Tuple of (node, port, isOutput) or null if no port hit.</returns>
    public (Node node, Port port, bool isOutput)? HitTestPort(double screenX, double screenY)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (_graph == null || _viewport == null) return null;

        if (_indexDirty) RebuildSpatialIndex();
        if (_nodeIndex == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        // Use actual view dimensions, not Bounds which includes parent-relative position
        var viewBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        // Check regular nodes - with viewport culling
        var regularStart = sw.ElapsedMilliseconds;
        int nodesChecked = 0;
        int nodesSkippedViewport = 0;
        foreach (var (node, nx, ny, nw, nh) in _nodeIndex)
        {
            // VIEWPORT CULLING: Quick bounds check using cached spatial index data
            var screenX1 = nx * zoom + offsetX;
            var screenY1 = ny * zoom + offsetY;
            var screenW = nw * zoom;
            var screenH = nh * zoom;
            var buffer = _settings.PortSize * zoom;

            if (screenX1 + screenW + buffer < 0 || screenX1 - buffer > viewBounds.Width ||
                screenY1 + screenH + buffer < 0 || screenY1 - buffer > viewBounds.Height)
            {
                nodesSkippedViewport++;
                continue;
            }

            nodesChecked++;
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
        var regularTime = sw.ElapsedMilliseconds - regularStart;
        var groupStart = sw.ElapsedMilliseconds;
        int groupsChecked = 0;
        int groupsSkippedViewport = 0;
        foreach (var group in _graph.Elements.Nodes.Where(n => n.IsGroup))
        {
            if (!GraphRenderModel.IsNodeVisible(_graph, group)) continue;

            // VIEWPORT CULLING: Skip groups outside visible area
            if (!IsInVisibleBounds(group, zoom, offsetX, offsetY, viewBounds))
            {
                groupsSkippedViewport++;
                continue;
            }

            groupsChecked++;
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

        sw.Stop();
        var groupTime = sw.ElapsedMilliseconds - groupStart;
        System.Diagnostics.Debug.WriteLine($"[HitTestPort] No hit in {sw.ElapsedMilliseconds}ms | NodesChecked:{nodesChecked}, SkippedViewport:{nodesSkippedViewport}, Groups:{groupsChecked}, GroupsSkipped:{groupsSkippedViewport}");
        return null;
    }

    /// <summary>
    /// Performs hit testing to find an edge at the given screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <param name="screenY">Y coordinate relative to the root panel (not canvas). Will be converted internally via ScreenToCanvas.</param>
    /// <returns>The hit edge, or null if no edge was hit.</returns>
    public Edge? HitTestEdge(double screenX, double screenY)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        if (_graph == null || _viewport == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        // Edge hit distance: When zoomed OUT, cap at base value for tight screen pixels.
        // When zoomed IN, scale to maintain consistent screen pixel distance.
        // At zoom 0.30: Min(50, 15) = 15 canvas units = 4.5 screen pixels (tight!)
        // At zoom 1.0:  Min(15, 15) = 15 canvas units = 15 screen pixels (normal)
        // At zoom 2.0:  Min(7.5, 15) = 7.5 canvas units = 15 screen pixels (scaled correctly)
        // This prevents edges from being "sticky" when zoomed out.
        var hitDistance = Math.Min(_settings.EdgeHitAreaWidth / _viewport.Zoom, (double)_settings.EdgeHitAreaWidth);
        // Use actual view dimensions, not Bounds which includes parent-relative position
        var viewBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var zoom = _viewport.Zoom;
        var offsetX = _viewport.OffsetX;
        var offsetY = _viewport.OffsetY;

        int edgesChecked = 0;
        int edgesSkippedViewport = 0;
        if (_nodeById == null)
        {
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[HitTestEdge] No nodeById dict in {sw.ElapsedMilliseconds}ms");
            return null;
        }

        foreach (var edge in _graph.Elements.Edges)
        {
            if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) continue;
            if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) continue;

            if (!IsNodeVisibleFast(sourceNode) || !IsNodeVisibleFast(targetNode)) continue;

            // VIEWPORT CULLING: Skip edges where BOTH endpoints are far outside viewport
            // (edges can still cross viewport even if both endpoints are outside, so we use larger margin)
            var sourceInViewport = IsInVisibleBounds(sourceNode, zoom, offsetX, offsetY, viewBounds);
            var targetInViewport = IsInVisibleBounds(targetNode, zoom, offsetX, offsetY, viewBounds);
            if (!sourceInViewport && !targetInViewport)
            {
                // Both endpoints outside - check if edge could potentially cross viewport
                // For simplicity, we skip only if both are outside (edge could still cross)
                // A more sophisticated check would verify the line segment doesn't intersect viewport
                edgesSkippedViewport++;
                continue;
            }

            edgesChecked++;
            // CRITICAL: Use optimized overload that accepts pre-looked-up nodes
            // The original GetEdgeEndpoints(edge, graph) does FirstOrDefault twice = O(n) per edge!
            var (start, end) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);

            if (_model.IsPointNearEdge(canvasPoint, start, end, hitDistance))
            {
                sw.Stop();
                // Log detailed info about the hit for debugging
                var screenStart = CanvasToScreen(start, zoom, offsetX, offsetY);
                var screenEnd = CanvasToScreen(end, zoom, offsetX, offsetY);
                System.Diagnostics.Debug.WriteLine($"[HitTestEdge] Hit in {sw.ElapsedMilliseconds}ms | EdgesChecked:{edgesChecked}, SkippedViewport:{edgesSkippedViewport}");
                System.Diagnostics.Debug.WriteLine($"[HitTestEdge]   ScreenClick=({screenX:F0},{screenY:F0}) CanvasClick=({canvasPoint.X:F0},{canvasPoint.Y:F0}) HitDist={hitDistance:F1}");
                System.Diagnostics.Debug.WriteLine($"[HitTestEdge]   EdgeStart: canvas=({start.X:F0},{start.Y:F0}) screen=({screenStart.X:F0},{screenStart.Y:F0})");
                System.Diagnostics.Debug.WriteLine($"[HitTestEdge]   EdgeEnd: canvas=({end.X:F0},{end.Y:F0}) screen=({screenEnd.X:F0},{screenEnd.Y:F0})");
                return edge;
            }
        }

        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[HitTestEdge] No hit in {sw.ElapsedMilliseconds}ms | EdgesChecked:{edgesChecked}, SkippedViewport:{edgesSkippedViewport}");
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
        if (_graph == null || _viewport == null || _nodeById == null) return default;

        // Use O(1) dictionary lookup instead of O(n) FirstOrDefault
        if (!_nodeById.TryGetValue(edge.Source, out var sourceNode)) return default;
        if (!_nodeById.TryGetValue(edge.Target, out var targetNode)) return default;

        // Use optimized overload with pre-looked-up nodes
        var (startCanvas, endCanvas) = _model.GetEdgeEndpoints(edge, sourceNode, targetNode);
        var canvasPos = isSource ? startCanvas : endCanvas;
        return CanvasToScreen(canvasPos, _viewport.Zoom, _viewport.OffsetX, _viewport.OffsetY);
    }

    /// <summary>
    /// Hit tests for shapes at the given screen coordinates.
    /// Shapes with higher ZIndex are tested first (topmost shapes have priority).
    /// </summary>
    /// <param name="screenX">Screen X coordinate.</param>
    /// <param name="screenY">Screen Y coordinate.</param>
    /// <returns>The hit shape, or null if no shape was hit.</returns>
    public Core.Elements.Shapes.ShapeElement? HitTestShape(double screenX, double screenY)
    {
        if (_graph == null || _viewport == null) return null;

        var canvasPoint = ScreenToCanvas(screenX, screenY);
        var shapes = _graph.Elements.Shapes;
        if (shapes.Count == 0) return null;

        // Test shapes in reverse ZIndex order (highest first = topmost)
        // Filter to only visible and selectable shapes
        var testOrder = shapes
            .Where(s => s.IsVisible && s.IsSelectable)
            .OrderByDescending(s => s.ZIndex)
            .ToList();

        foreach (var shape in testOrder)
        {
            var bounds = shape.GetBounds();
            if (bounds.Contains(new Core.Point(canvasPoint.X, canvasPoint.Y)))
            {
                System.Diagnostics.Debug.WriteLine($"[HitTestShape] Hit shape {shape.Id} (type: {shape.Type}) at canvas ({canvasPoint.X:F0},{canvasPoint.Y:F0})");
                return shape;
            }
        }

        return null;
    }
}
