using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core;
using FlowGraph.Core.Diagnostics;
using FlowGraph.Core.Elements.Shapes;
using System.Diagnostics;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Rendering methods.
/// </summary>
public partial class FlowCanvas
{
    #region Rendering

    /// <summary>
    /// Gets or sets whether to output rendering performance diagnostics.
    /// </summary>
    public bool DebugRenderingPerformance { get; set; } = false;

    /// <summary>
    /// Event raised when debug performance info is available.
    /// Subscribe to this to receive performance metrics.
    /// </summary>
    public event Action<string>? DebugOutput;

    private bool _inputHandlersAttached;

    private void EnsureCanvasInputHandlers()
    {
        if (_inputHandlersAttached || _mainCanvas == null) return;

        _inputHandlersAttached = true;

        // Centralized pointer handling to avoid attaching handlers per visual on each render.
        _mainCanvas.AddHandler(PointerPressedEvent, OnCanvasPointerPressed, handledEventsToo: true);
        _mainCanvas.AddHandler(PointerMovedEvent, OnCanvasPointerMoved, handledEventsToo: true);
        _mainCanvas.AddHandler(PointerReleasedEvent, OnCanvasPointerReleased, handledEventsToo: true);
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Control;

        if (source?.Tag is ValueTuple<Node, Port, bool> portHit)
        {
            OnPortPointerPressed(source, e);
            return;
        }

        if (source?.Tag is Edge)
        {
            OnEdgePointerPressed(source, e);
            return;
        }

        if (source?.Tag is Node)
        {
            OnNodePointerPressed(source, e);
            return;
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        var source = e.Source as Control;
        if (source?.Tag is Node)
        {
            OnNodePointerMoved(source, e);
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var source = e.Source as Control;
        if (source?.Tag is Node)
        {
            OnNodePointerReleased(source, e);
        }
    }

    private void LogDebug(string message)
    {
        if (DebugRenderingPerformance)
        {
            Debug.WriteLine(message);
            DebugOutput?.Invoke(message);
        }
    }

    private void RenderAll()
    {
        FlowGraphLogger.Debug(LogCategory.Rendering, "RenderAll called", "FlowCanvas.RenderAll");

        // Clear grid canvas once at the start of rendering
        _gridCanvas?.Children.Clear();

        RenderGrid();
        RenderCustomBackgrounds();
        // Note: Shapes are now rendered within RenderElements() for proper Z-order
        RenderElements();
    }

    private void RenderGrid()
    {
        if (_gridCanvas == null || _theme == null) return;

        // Skip grid rendering if ShowGrid is disabled (e.g., when using custom backgrounds)
        if (!Settings.ShowGrid)
        {
            return;
        }

        _gridRenderer.Render(_gridCanvas, Bounds.Size, _viewport, _theme.GridColor);
    }

    private void RenderCustomBackgrounds()
    {
        if (_gridCanvas == null || _theme == null) return;

        var registry = _graphRenderer.BackgroundRenderers;
        if (!registry.HasRenderers) return;

        var context = new Rendering.BackgroundRenderers.BackgroundRenderContext
        {
            Theme = _theme,
            Settings = Settings,
            Scale = _viewport.Zoom,
            Graph = Graph,
            VisibleBounds = new global::Avalonia.Rect(0, 0, Bounds.Width, Bounds.Height),
            Offset = new global::Avalonia.Point(_viewport.OffsetX, _viewport.OffsetY),
            // CanvasToScreen: converts canvas coordinates to screen coordinates
            // Same formula as ViewportState.CanvasToScreen: screenX = canvasX * Zoom + OffsetX
            CanvasToScreen = (x, y) => new global::Avalonia.Point(
                x * _viewport.Zoom + _viewport.OffsetX,
                y * _viewport.Zoom + _viewport.OffsetY),
            // ScreenToCanvas: converts screen coordinates to canvas coordinates  
            // Same formula as ViewportState.ScreenToCanvas: canvasX = (screenX - OffsetX) / Zoom
            ScreenToCanvas = (x, y) => new global::Avalonia.Point(
                (x - _viewport.OffsetX) / _viewport.Zoom,
                (y - _viewport.OffsetY) / _viewport.Zoom)
        };

        registry.Render(_gridCanvas, context);
    }

    private void RenderShapes()
    {
        if (_shapeVisualManager == null || Graph == null) return;

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;

        // Update the render context with current viewport state
        var renderContext = new Rendering.RenderContext(Settings);
        renderContext.SetViewport(_viewport);
        _shapeVisualManager.SetRenderContext(renderContext);

        // Get all shape elements from the graph
        var shapes = Graph.Elements.OfElementType<ShapeElement>().ToList();

        if (shapes.Count == 0)
        {
            // Clear any existing shape visuals if no shapes
            _shapeVisualManager.Clear();
            return;
        }

        // Track which shapes still exist for cleanup
        var existingShapeIds = _shapeVisualManager.GetShapeIds().ToHashSet();

        // Render shapes ordered by Z-index
        foreach (var shape in shapes.OrderBy(s => s.ZIndex))
        {
            _shapeVisualManager.AddOrUpdateShape(shape);
            existingShapeIds.Remove(shape.Id);
        }

        // Remove visuals for shapes that no longer exist
        foreach (var removedId in existingShapeIds)
        {
            _shapeVisualManager.RemoveShape(removedId);
        }

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            FlowGraphLogger.Debug(LogCategory.Rendering,
                $"[RenderShapes] {sw.ElapsedMilliseconds}ms | Rendered: {shapes.Count} shapes",
                "FlowCanvas.RenderShapes");
        }
    }

    private void RenderElements()
    {
        FlowGraphLogger.Debug(LogCategory.Rendering,
            $"RenderElements called - MainCanvas null: {_mainCanvas == null}, Graph null: {Graph == null}, Theme null: {_theme == null}",
            "FlowCanvas.RenderElements");

        if (_mainCanvas == null || Graph == null || _theme == null) return;

        FlowGraphLogger.Debug(LogCategory.Rendering,
            $"Graph has {Graph.Elements.Nodes.Count()} nodes, DirectRendering: {_useDirectRendering}",
            "FlowCanvas.RenderElements");

        // Auto-switch to direct rendering mode based on node count threshold
        var nodeCount = Graph.Elements.Nodes.Count();
        var threshold = Settings.DirectRenderingNodeThreshold;

        if (threshold > 0 && nodeCount >= threshold && !_useDirectRendering)
        {
            // Auto-enable direct rendering for large graphs
            EnableDirectRendering();
        }
        else if (threshold > 0 && nodeCount < threshold && _useDirectRendering)
        {
            // Auto-disable direct rendering when graph is small enough
            DisableDirectRendering();
            return; // DisableDirectRendering calls RenderElements
        }

        // Use direct rendering mode if enabled (bypasses visual tree for performance)
        if (_useDirectRendering && _directRenderer != null)
        {
            _directRenderer.Width = _mainCanvas.Bounds.Width;
            _directRenderer.Height = _mainCanvas.Bounds.Height;
            _directRenderer.Update(Graph, _viewport, _theme);
            return;
        }

        EnsureCanvasInputHandlers();

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;
        var totalNodes = Graph.Elements.Nodes.Count();
        var totalEdges = Graph.Elements.Edges.Count();
        var totalShapes = Graph.Elements.OfElementType<ShapeElement>().Count();

        _mainCanvas.Children.Clear();
        _graphRenderer.Clear();
        _shapeVisualManager?.Clear();

        var clearTime = sw?.ElapsedMilliseconds ?? 0;

        // Element-first rendering orchestrator:
        // Iterate all elements by ZIndex and dispatch to appropriate renderer.
        // This ensures consistent Z-order across nodes, edges, and shapes.
        //
        // Note: Edges are collected and batch-rendered for routing efficiency.
        // Shapes and nodes are rendered immediately in Z-order.

        var edgesToRender = new List<Edge>();
        int nodeCount_rendered = 0, shapeCount_rendered = 0;

        // Setup shape render context
        if (_shapeVisualManager != null)
        {
            var renderContext = new Rendering.RenderContext(Settings);
            renderContext.SetViewport(_viewport);
            _shapeVisualManager.SetRenderContext(renderContext);
        }

        foreach (var element in Graph.Elements.ByZIndex)
        {
            if (!element.IsVisible) continue;

            if (element is Node node)
            {
                // Skip nodes hidden by group collapse
                if (!_graphRenderer.IsNodeVisible(Graph, node)) continue;

                _graphRenderer.RenderNode(_mainCanvas, node, _theme, null);
                nodeCount_rendered++;
            }
            else if (element is Edge edge)
            {
                // Collect edges for batch rendering (routing algorithm needs all edges)
                edgesToRender.Add(edge);
            }
            else if (element is ShapeElement shape)
            {
                // Render shape immediately at its Z-index position
                _shapeVisualManager?.AddOrUpdateShape(shape);
                shapeCount_rendered++;
            }
        }

        // Batch-render all edges for routing efficiency
        // Edges are inserted at appropriate Z-index by the edge renderer
        if (edgesToRender.Any())
        {
            _graphRenderer.RenderEdges(_mainCanvas, Graph, _theme);
            ApplyEdgeOpacityOverrides();
        }

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            var renderedVisuals = _mainCanvas.Children.Count;
            LogDebug($"[RenderGraph] Total: {sw.ElapsedMilliseconds}ms | " +
                $"Clear: {clearTime}ms, Render: {sw.ElapsedMilliseconds - clearTime}ms | " +
                $"Graph: {totalNodes}n/{totalEdges}e/{totalShapes}s, Rendered: {nodeCount_rendered}n/{edgesToRender.Count}e/{shapeCount_rendered}s, Visuals: {renderedVisuals}");
        }
    }

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            var renderedVisuals = _mainCanvas.Children.Count;
            LogDebug($"[RenderGraph] Total: {sw.ElapsedMilliseconds}ms | " +
                $"Clear: {clearTime}ms, Render: {sw.ElapsedMilliseconds - clearTime}ms | " +
                $"Graph: {totalNodes}n/{totalEdges}e, Rendered: {nodeCount_rendered}n/{edgeCount_rendered}e, Visuals: {renderedVisuals}");
        }
    }

    private void RenderGroupNodes()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;

        // Render groups ordered by depth (outermost first)
        var groups = Graph.Elements.Nodes
            .Where(n => n.IsGroup && _graphRenderer.IsNodeVisible(Graph, n))
            .OrderBy(n => GetGroupDepth(n))
            .ToList();

        var filterTime = sw?.ElapsedMilliseconds ?? 0;
        var count = 0;

        foreach (var group in groups)
        {
            _graphRenderer.RenderNode(_mainCanvas, group, _theme, null);
            count++;
        }

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            LogDebug($"  [RenderGroupNodes] {sw.ElapsedMilliseconds}ms | Filter: {filterTime}ms, Rendered: {count} groups");
        }
    }

    private void RenderRegularNodes()
    {
        FlowGraphLogger.Debug(LogCategory.Rendering,
            $"RenderRegularNodes called - Graph null: {Graph == null}, MainCanvas null: {_mainCanvas == null}, Theme null: {_theme == null}",
            "FlowCanvas.RenderRegularNodes");

        if (_mainCanvas == null || Graph == null || _theme == null) return;

        FlowGraphLogger.Debug(LogCategory.Rendering,
            $"Graph has {Graph.Elements.Nodes.Count()} nodes",
            "FlowCanvas.RenderRegularNodes");

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;

        var nodesToRender = Graph.Elements.Nodes
            .Where(n => !n.IsGroup && _graphRenderer.IsNodeVisible(Graph, n))
            .ToList();

        var sequenceNodes = Graph.Elements.Nodes.Where(n => n.Type == "sequence-message").ToList();
        if (sequenceNodes.Any())
        {
            FlowGraphLogger.Debug(LogCategory.Nodes,
                $"Sequence diagram: Total={Graph.Elements.Nodes.Count()}, SeqNodes={sequenceNodes.Count}, ToRender={nodesToRender.Count}",
                "FlowCanvas.RenderRegularNodes");

            foreach (var node in sequenceNodes)
            {
                var isVisible = _graphRenderer.IsNodeVisible(Graph, node);
                FlowGraphLogger.Debug(LogCategory.Nodes,
                    $"Node '{node.Label}': Type={node.Type}, IsGroup={node.IsGroup}, Visible={isVisible}, Pos=({node.Position.X:F1},{node.Position.Y:F1})",
                    "FlowCanvas.RenderRegularNodes");
            }
        }

        var filterTime = sw?.ElapsedMilliseconds ?? 0;
        var count = 0;
        var portCount = 0;

        foreach (var node in nodesToRender)
        {
            _graphRenderer.RenderNode(_mainCanvas, node, _theme, null);
            count++;
            portCount += node.Inputs.Count + node.Outputs.Count;
        }

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            var avgPerNode = count > 0 ? (sw.ElapsedMilliseconds - filterTime) / (double)count : 0;
            LogDebug($"  [RenderRegularNodes] {sw.ElapsedMilliseconds}ms | Filter: {filterTime}ms, " +
                $"Rendered: {count} nodes, {portCount} ports, Avg: {avgPerNode:F2}ms/node");
        }
    }

    private int GetGroupDepth(Node node)
    {
        int depth = 0;
        var current = node;
        while (!string.IsNullOrEmpty(current.ParentGroupId))
        {
            depth++;
            current = Graph?.Elements.Nodes.FirstOrDefault(n => n.Id == current.ParentGroupId);
            if (current == null) break;
        }
        return depth;
    }

    private void RenderEdges()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;

        // Skip if using direct rendering - edges are drawn by DirectGraphRenderer
        if (_useDirectRendering) return;

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;

        _graphRenderer.RenderEdges(_mainCanvas, Graph, _theme);

        var renderTime = sw?.ElapsedMilliseconds ?? 0;

        // Re-apply any active opacity overrides (important if edges were re-rendered during an animation)
        ApplyEdgeOpacityOverrides();

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            LogDebug($"  [RenderEdges] {sw.ElapsedMilliseconds}ms | Render: {renderTime}ms");
        }
    }

    #endregion
}
