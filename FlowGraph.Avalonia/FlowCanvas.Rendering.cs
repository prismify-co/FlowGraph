using Avalonia.Controls;
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

    private void RenderGraph()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;

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

        AttachPortEventHandlers();
        var portHandlerTime = sw?.ElapsedMilliseconds ?? 0;

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            var renderedNodes = _mainCanvas.Children.OfType<Control>().Count(c => c.Tag is Node);
            LogDebug($"[RenderGraph] Total: {sw.ElapsedMilliseconds}ms | " +
                $"Clear: {clearTime}ms, Groups: {groupTime - clearTime}ms, " +
                $"Edges: {edgeTime - groupTime}ms, Nodes: {nodeTime - edgeTime}ms, " +
                $"Handlers: {portHandlerTime - nodeTime}ms | " +
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
            var control = _graphRenderer.RenderNode(_mainCanvas, group, _theme, null);
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;
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
            var control = _graphRenderer.RenderNode(_mainCanvas, node, _theme, null);
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;
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
        
        var sw = DebugRenderingPerformance ? Stopwatch.StartNew() : null;
        
        _graphRenderer.RenderEdges(_mainCanvas, Graph, _theme);

        var renderTime = sw?.ElapsedMilliseconds ?? 0;

        // Re-apply any active opacity overrides (important if edges were re-rendered during an animation)
        ApplyEdgeOpacityOverrides();

        AttachEdgeEventHandlers();

        if (DebugRenderingPerformance && sw != null)
        {
            sw.Stop();
            LogDebug($"  [RenderEdges] {sw.ElapsedMilliseconds}ms | Render: {renderTime}ms, Handlers: {sw.ElapsedMilliseconds - renderTime}ms");
        }
    }

    private void AttachPortEventHandlers()
    {
        if (Graph == null) return;
        
        foreach (var node in Graph.Nodes)
        {
            foreach (var port in node.Inputs.Concat(node.Outputs))
            {
                var portVisual = _graphRenderer.GetPortVisual(node.Id, port.Id);
                if (portVisual != null)
                {
                    portVisual.PointerPressed += OnPortPointerPressed;
                    portVisual.PointerEntered += OnPortPointerEntered;
                    portVisual.PointerExited += OnPortPointerExited;
                }
            }
        }
    }

    private void AttachEdgeEventHandlers()
    {
        if (Graph == null) return;
        
        foreach (var edge in Graph.Edges)
        {
            var edgeVisual = _graphRenderer.GetEdgeVisual(edge.Id);
            if (edgeVisual != null)
            {
                edgeVisual.PointerPressed -= OnEdgePointerPressed;
                edgeVisual.PointerPressed += OnEdgePointerPressed;
            }
        }
    }

    #endregion
}
