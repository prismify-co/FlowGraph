using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages selection state for nodes and edges in the flow canvas.
/// </summary>
public class SelectionManager
{
    private readonly IGraphContext _context;
    private readonly Func<GraphRenderer> _getRenderer;
    private readonly Func<ThemeResources> _getTheme;
    private readonly CommandHistory _commandHistory;

    // Track previous selection state to detect changes
    private HashSet<string> _previousSelectedNodeIds = new();
    private HashSet<string> _previousSelectedEdgeIds = new();

    /// <summary>
    /// Creates a new selection manager.
    /// </summary>
    /// <param name="context">The graph context providing access to the current graph.</param>
    /// <param name="getRenderer">Function to get the graph renderer.</param>
    /// <param name="getTheme">Function to get the current theme.</param>
    /// <param name="commandHistory">The command history for undo/redo support.</param>
    public SelectionManager(
        IGraphContext context,
        Func<GraphRenderer> getRenderer,
        Func<ThemeResources> getTheme,
        CommandHistory commandHistory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _getRenderer = getRenderer ?? throw new ArgumentNullException(nameof(getRenderer));
        _getTheme = getTheme ?? throw new ArgumentNullException(nameof(getTheme));
        _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
    }

    // Backwards compatibility constructor
    [Obsolete("Use the constructor that accepts IGraphContext instead.")]
    public SelectionManager(
        Func<Graph?> getGraph,
        Func<GraphRenderer> getRenderer,
        Func<ThemeResources> getTheme,
        CommandHistory commandHistory)
        : this(new FuncGraphContext(getGraph), getRenderer, getTheme, commandHistory)
    {
    }

    /// <summary>
    /// Event raised when edges need to be re-rendered after selection changes.
    /// </summary>
    public event EventHandler? EdgeRerenderRequested;

    /// <summary>
    /// Event raised when the selection changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Selects all nodes in the graph.
    /// </summary>
    public void SelectAll()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        foreach (var node in graph.Nodes)
        {
            // Only select nodes that are selectable
            if (node.IsSelectable)
            {
                node.IsSelected = true;
            }
        }

        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Deselects all nodes and edges.
    /// </summary>
    public void DeselectAll()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        foreach (var node in graph.Nodes)
        {
            node.IsSelected = false;
        }

        foreach (var edge in graph.Edges)
        {
            edge.IsSelected = false;
        }

        EdgeRerenderRequested?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Deletes all selected nodes and edges.
    /// </summary>
    public void DeleteSelected()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        // Only delete edges that are deletable (or all edges for simplicity, since Edge doesn't have IsDeletable yet)
        var selectedEdges = graph.Edges.Where(e => e.IsSelected).ToList();

        // Only delete nodes that are deletable
        var selectedNodes = graph.Nodes.Where(n => n.IsSelected && n.IsDeletable).ToList();

        // Also filter edges - don't delete edges connected to non-deletable nodes
        var nonDeletableNodeIds = graph.Nodes.Where(n => !n.IsDeletable).Select(n => n.Id).ToHashSet();
        selectedEdges = selectedEdges
            .Where(e => !nonDeletableNodeIds.Contains(e.Source) && !nonDeletableNodeIds.Contains(e.Target))
            .ToList();

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
            ( > 0, > 0) => $"Delete {selectedNodes.Count} nodes and {selectedEdges.Count} connections",
            ( > 0, 0) => selectedNodes.Count == 1 ? "Delete node" : $"Delete {selectedNodes.Count} nodes",
            (0, > 0) => selectedEdges.Count == 1 ? "Delete connection" : $"Delete {selectedEdges.Count} connections",
            _ => "Delete"
        };

        _commandHistory.Execute(new CompositeCommand(description, commands));
        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Handles edge click to update selection state.
    /// </summary>
    public void HandleEdgeClicked(Edge clickedEdge, bool ctrlHeld)
    {
        var graph = _context.Graph;
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

        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Notifies that the selection may have changed (called by external code).
    /// </summary>
    public void NotifySelectionMayHaveChanged()
    {
        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Gets all selected nodes.
    /// </summary>
    public IEnumerable<Node> GetSelectedNodes()
    {
        var graph = _context.Graph;
        return graph?.Nodes.Where(n => n.IsSelected) ?? Enumerable.Empty<Node>();
    }

    /// <summary>
    /// Gets all selected edges.
    /// </summary>
    public IEnumerable<Edge> GetSelectedEdges()
    {
        var graph = _context.Graph;
        return graph?.Edges.Where(e => e.IsSelected) ?? Enumerable.Empty<Edge>();
    }

    /// <summary>
    /// Gets whether any items are selected.
    /// </summary>
    public bool HasSelection => GetSelectedNodes().Any() || GetSelectedEdges().Any();

    private void RaiseSelectionChangedIfNeeded()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var currentSelectedNodes = graph.Nodes.Where(n => n.IsSelected).ToList();
        var currentSelectedEdges = graph.Edges.Where(e => e.IsSelected).ToList();

        var currentNodeIds = currentSelectedNodes.Select(n => n.Id).ToHashSet();
        var currentEdgeIds = currentSelectedEdges.Select(e => e.Id).ToHashSet();

        // Check if selection actually changed
        if (currentNodeIds.SetEquals(_previousSelectedNodeIds) &&
            currentEdgeIds.SetEquals(_previousSelectedEdgeIds))
        {
            return;
        }

        // Calculate added/removed
        var addedNodeIds = currentNodeIds.Except(_previousSelectedNodeIds).ToHashSet();
        var removedNodeIds = _previousSelectedNodeIds.Except(currentNodeIds).ToHashSet();
        var addedEdgeIds = currentEdgeIds.Except(_previousSelectedEdgeIds).ToHashSet();
        var removedEdgeIds = _previousSelectedEdgeIds.Except(currentEdgeIds).ToHashSet();

        var addedNodes = currentSelectedNodes.Where(n => addedNodeIds.Contains(n.Id)).ToList();
        var removedNodes = graph.Nodes.Where(n => removedNodeIds.Contains(n.Id)).ToList();
        var addedEdges = currentSelectedEdges.Where(e => addedEdgeIds.Contains(e.Id)).ToList();
        var removedEdges = graph.Edges.Where(e => removedEdgeIds.Contains(e.Id)).ToList();

        // Update tracking
        _previousSelectedNodeIds = currentNodeIds;
        _previousSelectedEdgeIds = currentEdgeIds;

        // Raise event
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(
            currentSelectedNodes,
            currentSelectedEdges,
            addedNodes,
            removedNodes,
            addedEdges,
            removedEdges));
    }
}
