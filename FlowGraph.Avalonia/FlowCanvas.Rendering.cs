using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core;
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
        RenderGrid();
        RenderCustomBackgrounds();
        RenderGraph();
    }

    private void RenderGrid()
    {
        if (_gridCanvas == null || _theme == null) return;

        // Skip grid rendering if ShowGrid is disabled (e.g., when using FlowBackground)
        if (!Settings.ShowGrid)
        {
            _gridCanvas.Children.Clear();
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
            CanvasToScreen = (x, y) => new global::Avalonia.Point(
                (x - _viewport.OffsetX) * _viewport.Zoom,
                (y - _viewport.OffsetY) * _viewport.Zoom),
            ScreenToCanvas = (x, y) => new global::Avalonia.Point(
                x / _viewport.Zoom + _viewport.OffsetX,
                y / _viewport.Zoom + _viewport.OffsetY)
        };

        registry.Render(_gridCanvas, context);
    }

    private void RenderGraph()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;

        // Auto-switch to direct rendering mode based on node count threshold
        var nodeCount = Graph.Nodes.Count;
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
            return; // DisableDirectRendering calls RenderGraph
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
        var totalNodes = Graph.Nodes.Count;
        var totalEdges = Graph.Edges.Count;

        _mainCanvas.Children.Clear();
        _graphRenderer.Clear();

        var clearTime = sw?.ElapsedMilliseconds ?? 0;

        // Render order for proper z-index:
        // 1. Groups (bottom) - rendered first in RenderNodes
        // 2. Edges (middle) - rendered after groups, before regular nodes  
        // 3. Regular nodes (top) - rendered last in RenderNodes
        // 4. Ports are rendered with their nodes

        // Render groups first (they go behind everything)
        RenderGroupNodes();
        var groupTime = sw?.ElapsedMilliseconds ?? 0;

        // Render edges (on top of groups)
        RenderEdges();
        var edgeTime = sw?.ElapsedMilliseconds ?? 0;

        // Render regular nodes and ports (on top of edges)
        RenderRegularNodes();
        var nodeTime = sw?.ElapsedMilliseconds ?? 0;

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            var renderedNodes = _mainCanvas.Children.OfType<Control>().Count(c => c.Tag is Node);
            LogDebug($"[RenderGraph] Total: {sw.ElapsedMilliseconds}ms | " +
                $"Clear: {clearTime}ms, Groups: {groupTime - clearTime}ms, " +
                $"Edges: {edgeTime - groupTime}ms, Nodes: {nodeTime - edgeTime}ms | " +
                $"Graph: {totalNodes}n/{totalEdges}e, Rendered: ~{renderedNodes} visuals");
        }
    }

    private void RenderGroupNodes()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;

        // Render groups ordered by depth (outermost first)
        var groups = Graph.Nodes
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
        if (_mainCanvas == null || Graph == null || _theme == null) return;

        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;

        var nodesToRender = Graph.Nodes
            .Where(n => !n.IsGroup && _graphRenderer.IsNodeVisible(Graph, n))
            .ToList();

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
            current = Graph?.Nodes.FirstOrDefault(n => n.Id == current.ParentGroupId);
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
