using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages selection state for nodes and edges in the flow canvas.
/// </summary>
public class SelectionManager
{
    private readonly Func<Graph?> _getGraph;
    private readonly Func<GraphRenderer> _getRenderer;
    private readonly Func<ThemeResources> _getTheme;
    private readonly CommandHistory _commandHistory;

    public SelectionManager(
        Func<Graph?> getGraph,
        Func<GraphRenderer> getRenderer,
        Func<ThemeResources> getTheme,
        CommandHistory commandHistory)
    {
        _getGraph = getGraph;
        _getRenderer = getRenderer;
        _getTheme = getTheme;
        _commandHistory = commandHistory;
    }

    /// <summary>
    /// Event raised when edges need to be re-rendered after selection changes.
    /// </summary>
    public event EventHandler? EdgesNeedRerender;

    /// <summary>
    /// Selects all nodes in the graph.
    /// </summary>
    public void SelectAll()
    {
        var graph = _getGraph();
        if (graph == null) return;

        foreach (var node in graph.Nodes)
        {
            node.IsSelected = true;
        }
    }

    /// <summary>
    /// Deselects all nodes and edges.
    /// </summary>
    public void DeselectAll()
    {
        var graph = _getGraph();
        if (graph == null) return;

        foreach (var node in graph.Nodes)
        {
            node.IsSelected = false;
        }

        foreach (var edge in graph.Edges)
        {
            edge.IsSelected = false;
        }

        EdgesNeedRerender?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Deletes all selected nodes and edges.
    /// </summary>
    public void DeleteSelected()
    {
        var graph = _getGraph();
        if (graph == null) return;

        var selectedEdges = graph.Edges.Where(e => e.IsSelected).ToList();
        var selectedNodes = graph.Nodes.Where(n => n.IsSelected).ToList();

        if (selectedEdges.Count == 0 && selectedNodes.Count == 0)
            return;

        var commands = new List<IGraphCommand>();

        if (selectedEdges.Count > 0)
        {
            commands.Add(new RemoveEdgesCommand(graph, selectedEdges));
        }

        if (selectedNodes.Count > 0)
        {
            commands.Add(new RemoveNodesCommand(graph, selectedNodes));
        }

        var description = (selectedNodes.Count, selectedEdges.Count) switch
        {
            (> 0, > 0) => $"Delete {selectedNodes.Count} nodes and {selectedEdges.Count} connections",
            (> 0, 0) => selectedNodes.Count == 1 ? "Delete node" : $"Delete {selectedNodes.Count} nodes",
            (0, > 0) => selectedEdges.Count == 1 ? "Delete connection" : $"Delete {selectedEdges.Count} connections",
            _ => "Delete"
        };

        _commandHistory.Execute(new CompositeCommand(description, commands));
    }

    /// <summary>
    /// Handles edge click to update selection state.
    /// </summary>
    public void HandleEdgeClicked(Edge clickedEdge, bool ctrlHeld)
    {
        var graph = _getGraph();
        var renderer = _getRenderer();
        var theme = _getTheme();

        if (graph == null) return;

        // Update visual state for ALL edges (some may have been deselected)
        foreach (var edge in graph.Edges)
        {
            renderer.UpdateEdgeSelection(edge, theme);
        }

        // Also update node visuals if Ctrl was not held (nodes were deselected)
        if (!ctrlHeld)
        {
            foreach (var node in graph.Nodes)
            {
                renderer.UpdateNodeSelection(node, theme);
            }
        }
    }

    /// <summary>
    /// Gets all selected nodes.
    /// </summary>
    public IEnumerable<Node> GetSelectedNodes()
    {
        var graph = _getGraph();
        return graph?.Nodes.Where(n => n.IsSelected) ?? Enumerable.Empty<Node>();
    }

    /// <summary>
    /// Gets all selected edges.
    /// </summary>
    public IEnumerable<Edge> GetSelectedEdges()
    {
        var graph = _getGraph();
        return graph?.Edges.Where(e => e.IsSelected) ?? Enumerable.Empty<Edge>();
    }

    /// <summary>
    /// Gets whether any items are selected.
    /// </summary>
    public bool HasSelection => GetSelectedNodes().Any() || GetSelectedEdges().Any();
}
