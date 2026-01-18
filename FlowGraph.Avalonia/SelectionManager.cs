using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using FlowGraph.Core.Commands;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages selection state for nodes and edges in the flow canvas.
/// </summary>
public class SelectionManager
{
    private readonly IGraphContext _context;
    private readonly Func<CanvasElementManager> _getRenderer;
    private readonly Func<ThemeResources> _getTheme;
    private readonly CommandHistory _commandHistory;

    // Track previous selection state to detect changes
    private HashSet<string> _previousSelectedNodeIds = new();
    private HashSet<string> _previousSelectedEdgeIds = new();
    private HashSet<string> _previousSelectedShapeIds = new();

    /// <summary>
    /// Creates a new selection manager.
    /// </summary>
    /// <param name="context">The graph context providing access to the current graph.</param>
    /// <param name="getRenderer">Function to get the graph renderer.</param>
    /// <param name="getTheme">Function to get the current theme.</param>
    /// <param name="commandHistory">The command history for undo/redo support.</param>
    public SelectionManager(
        IGraphContext context,
        Func<CanvasElementManager> getRenderer,
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
        Func<CanvasElementManager> getRenderer,
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
    /// Selects all selectable elements (nodes, edges, and shapes) in the graph.
    /// </summary>
    public void SelectAll()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        foreach (var node in graph.Elements.Nodes)
        {
            if (node.IsSelectable)
            {
                node.IsSelected = true;
            }
        }

        foreach (var edge in graph.Elements.Edges)
        {
            if (edge.IsSelectable)
            {
                edge.IsSelected = true;
            }
        }

        foreach (var shape in graph.Elements.Shapes)
        {
            if (shape.IsSelectable)
            {
                shape.IsSelected = true;
                _getRenderer()?.UpdateShapeSelection(shape.Id, true);
            }
        }

        EdgeRerenderRequested?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Deselects all nodes, edges, and shapes.
    /// </summary>
    public void DeselectAll()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        // OPTIMIZED: Only deselect items that are actually selected
        foreach (var node in graph.Elements.Nodes.Where(n => n.IsSelected))
        {
            node.IsSelected = false;
        }

        foreach (var edge in graph.Elements.Edges.Where(e => e.IsSelected))
        {
            edge.IsSelected = false;
        }

        // Deselect shapes and update their visuals
        var renderer = _getRenderer();
        foreach (var shape in graph.Elements.Shapes.Where(s => s.IsSelected))
        {
            shape.IsSelected = false;
            renderer?.UpdateShapeSelection(shape.Id, false);
        }

        EdgeRerenderRequested?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChangedIfNeeded();
    }

    /// <summary>
    /// Deletes all selected nodes, edges, and shapes.
    /// </summary>
    public void DeleteSelected()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        // Collect selected edges
        var selectedEdges = graph.Elements.Edges.Where(e => e.IsSelected).ToList();

        // Only delete nodes that are deletable
        var selectedNodes = graph.Elements.Nodes.Where(n => n.IsSelected && n.IsDeletable).ToList();

        // Collect selected shapes (shapes don't have IsDeletable, assume all are deletable)
        var selectedShapes = graph.Elements.Shapes.Where(s => s.IsSelected).ToList();

        // Also filter edges - don't delete edges connected to non-deletable nodes
        var nonDeletableNodeIds = graph.Elements.Nodes.Where(n => !n.IsDeletable).Select(n => n.Id).ToHashSet();
        selectedEdges = selectedEdges
            .Where(e => !nonDeletableNodeIds.Contains(e.Source) && !nonDeletableNodeIds.Contains(e.Target))
            .ToList();

        if (selectedEdges.Count == 0 && selectedNodes.Count == 0 && selectedShapes.Count == 0)
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

        if (selectedShapes.Count > 0)
        {
            commands.Add(new RemoveElementsCommand(graph, selectedShapes.Cast<Core.Elements.ICanvasElement>().ToList()));
        }

        var parts = new List<string>();
        if (selectedNodes.Count > 0)
            parts.Add(selectedNodes.Count == 1 ? "1 node" : $"{selectedNodes.Count} nodes");
        if (selectedEdges.Count > 0)
            parts.Add(selectedEdges.Count == 1 ? "1 connection" : $"{selectedEdges.Count} connections");
        if (selectedShapes.Count > 0)
            parts.Add(selectedShapes.Count == 1 ? "1 shape" : $"{selectedShapes.Count} shapes");

        var description = $"Delete {string.Join(" and ", parts)}";

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

        // OPTIMIZED: Only update visual state for edges that changed
        // The clicked edge's visual needs updating
        renderer.UpdateEdgeSelection(clickedEdge, theme);

        // If ctrl wasn't held, other selected edges were deselected - update those
        if (!ctrlHeld)
        {
            foreach (var edge in graph.Elements.Edges.Where(e => e.Id != clickedEdge.Id && e.IsSelected == false))
            {
                // Only update edges that might have changed (previously selected)
                renderer.UpdateEdgeSelection(edge, theme);
            }

            // Only update previously selected nodes that were deselected
            foreach (var node in graph.Elements.Nodes.Where(n => n.IsSelected == false))
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
        return graph?.Elements.Nodes.Where(n => n.IsSelected) ?? Enumerable.Empty<Node>();
    }

    /// <summary>
    /// Gets all selected edges.
    /// </summary>
    public IEnumerable<Edge> GetSelectedEdges()
    {
        var graph = _context.Graph;
        return graph?.Elements.Edges.Where(e => e.IsSelected) ?? Enumerable.Empty<Edge>();
    }

    /// <summary>
    /// Gets all selected shapes.
    /// </summary>
    public IEnumerable<ShapeElement> GetSelectedShapes()
    {
        var graph = _context.Graph;
        return graph?.Elements.Shapes.Where(s => s.IsSelected) ?? Enumerable.Empty<ShapeElement>();
    }

    /// <summary>
    /// Gets whether any items are selected.
    /// </summary>
    public bool HasSelection => GetSelectedNodes().Any() || GetSelectedEdges().Any() || GetSelectedShapes().Any();

    private void RaiseSelectionChangedIfNeeded()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var currentSelectedNodes = graph.Elements.Nodes.Where(n => n.IsSelected).ToList();
        var currentSelectedEdges = graph.Elements.Edges.Where(e => e.IsSelected).ToList();
        var currentSelectedShapes = graph.Elements.Shapes.Where(s => s.IsSelected).ToList();

        var currentNodeIds = currentSelectedNodes.Select(n => n.Id).ToHashSet();
        var currentEdgeIds = currentSelectedEdges.Select(e => e.Id).ToHashSet();
        var currentShapeIds = currentSelectedShapes.Select(s => s.Id).ToHashSet();

        // Check if selection actually changed
        if (currentNodeIds.SetEquals(_previousSelectedNodeIds) &&
            currentEdgeIds.SetEquals(_previousSelectedEdgeIds) &&
            currentShapeIds.SetEquals(_previousSelectedShapeIds))
        {
            return;
        }

        // Calculate added/removed
        var addedNodeIds = currentNodeIds.Except(_previousSelectedNodeIds).ToHashSet();
        var removedNodeIds = _previousSelectedNodeIds.Except(currentNodeIds).ToHashSet();
        var addedEdgeIds = currentEdgeIds.Except(_previousSelectedEdgeIds).ToHashSet();
        var removedEdgeIds = _previousSelectedEdgeIds.Except(currentEdgeIds).ToHashSet();
        var addedShapeIds = currentShapeIds.Except(_previousSelectedShapeIds).ToHashSet();
        var removedShapeIds = _previousSelectedShapeIds.Except(currentShapeIds).ToHashSet();

        var addedNodes = currentSelectedNodes.Where(n => addedNodeIds.Contains(n.Id)).ToList();
        var removedNodes = graph.Elements.Nodes.Where(n => removedNodeIds.Contains(n.Id)).ToList();
        var addedEdges = currentSelectedEdges.Where(e => addedEdgeIds.Contains(e.Id)).ToList();
        var removedEdges = graph.Elements.Edges.Where(e => removedEdgeIds.Contains(e.Id)).ToList();
        var addedShapes = currentSelectedShapes.Where(s => addedShapeIds.Contains(s.Id)).ToList();
        var removedShapes = graph.Elements.Shapes.Where(s => removedShapeIds.Contains(s.Id)).ToList();

        // Update tracking
        _previousSelectedNodeIds = currentNodeIds;
        _previousSelectedEdgeIds = currentEdgeIds;
        _previousSelectedShapeIds = currentShapeIds;

        // Raise event
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(
            currentSelectedNodes,
            currentSelectedEdges,
            addedNodes,
            removedNodes,
            addedEdges,
            removedEdges,
            currentSelectedShapes,
            addedShapes,
            removedShapes));
    }
}
