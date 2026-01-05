using Avalonia.Controls;
using FlowGraph.Core;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Rendering methods.
/// </summary>
public partial class FlowCanvas
{
    #region Rendering

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

        _mainCanvas.Children.Clear();
        _graphRenderer.Clear();

        // Render order for proper z-index:
        // 1. Groups (bottom) - rendered first in RenderNodes
        // 2. Edges (middle) - rendered after groups, before regular nodes  
        // 3. Regular nodes (top) - rendered last in RenderNodes
        // 4. Ports are rendered with their nodes
        
        // Render groups first (they go behind everything)
        RenderGroupNodes();
        
        // Render edges (on top of groups)
        RenderEdges();
        
        // Render regular nodes and ports (on top of edges)
        RenderRegularNodes();

        AttachPortEventHandlers();
    }

    private void RenderGroupNodes()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;
        
        // Render groups ordered by depth (outermost first)
        var groups = Graph.Nodes
            .Where(n => n.IsGroup && _graphRenderer.IsNodeVisible(Graph, n))
            .OrderBy(n => GetGroupDepth(n))
            .ToList();

        foreach (var group in groups)
        {
            var control = _graphRenderer.RenderNode(_mainCanvas, group, _theme, null);
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;
        }
    }

    private void RenderRegularNodes()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;
        
        foreach (var node in Graph.Nodes.Where(n => !n.IsGroup && _graphRenderer.IsNodeVisible(Graph, n)))
        {
            var control = _graphRenderer.RenderNode(_mainCanvas, node, _theme, null);
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;
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
        _graphRenderer.RenderEdges(_mainCanvas, Graph, _theme);

        // Re-apply any active opacity overrides (important if edges were re-rendered during an animation)
        ApplyEdgeOpacityOverrides();

        AttachEdgeEventHandlers();
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
