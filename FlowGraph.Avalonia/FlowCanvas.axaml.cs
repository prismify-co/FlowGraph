using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using FlowGraph.Core.Commands;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlowGraph.Avalonia;

/// <summary>
/// A canvas control for displaying and editing flow graphs.
/// </summary>
public partial class FlowCanvas : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<Graph?> GraphProperty =
        AvaloniaProperty.Register<FlowCanvas, Graph?>(nameof(Graph));

    public static readonly StyledProperty<FlowCanvasSettings> SettingsProperty =
        AvaloniaProperty.Register<FlowCanvas, FlowCanvasSettings>(nameof(Settings), FlowCanvasSettings.Default);

    /// <summary>
    /// The graph to display and edit.
    /// </summary>
    public Graph? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    /// <summary>
    /// Settings for the canvas appearance and behavior.
    /// </summary>
    public FlowCanvasSettings Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the viewport state for external components (e.g., minimap).
    /// </summary>
    public ViewportState Viewport => _viewport;

    /// <summary>
    /// Gets the node renderer registry for registering custom node types.
    /// </summary>
    public Rendering.NodeRenderers.NodeRendererRegistry NodeRenderers => _graphRenderer.NodeRenderers;

    /// <summary>
    /// Gets the command history for undo/redo operations.
    /// </summary>
    public CommandHistory CommandHistory { get; } = new();

    /// <summary>
    /// Gets the current zoom level.
    /// </summary>
    public double CurrentZoom => _viewport.Zoom;

    /// <summary>
    /// Gets the selection manager for managing node and edge selection.
    /// </summary>
    public SelectionManager Selection => _selectionManager;

    /// <summary>
    /// Gets the group manager for advanced group operations.
    /// </summary>
    public GroupManager Groups => _groupManager;

    /// <summary>
    /// Gets or sets the connection validator for validating new connections.
    /// Set to null to allow all connections.
    /// </summary>
    public IConnectionValidator? ConnectionValidator { get; set; }

    /// <summary>
    /// Event raised when a connection is rejected by the validator.
    /// </summary>
    public event EventHandler<ConnectionRejectedEventArgs>? ConnectionRejected;

    /// <summary>
    /// Event raised when a group's collapsed state changes.
    /// </summary>
    public event EventHandler<GroupCollapsedEventArgs>? GroupCollapsedChanged;

    #endregion

    #region Private Fields

    // UI Elements
    private Canvas? _mainCanvas;
    private Canvas? _gridCanvas;
    private Panel? _rootPanel;

    // Components
    private ViewportState _viewport = null!;
    private GridRenderer _gridRenderer = null!;
    private GraphRenderer _graphRenderer = null!;
    private CanvasInputHandler _inputHandler = null!;
    private SelectionManager _selectionManager = null!;
    private ClipboardManager _clipboardManager = null!;
    private GroupManager _groupManager = null!;
    private FlowCanvasContextMenu _contextMenu = null!;
    private ThemeResources _theme = null!;

    #endregion

    #region Constructor & Initialization

    public FlowCanvas()
    {
        InitializeComponent();
        InitializeComponents();
        
        // Re-render when theme changes
        this.ActualThemeVariantChanged += (_, _) =>
        {
            _theme = new ThemeResources(this);
            RenderAll();
        };
    }

    private void InitializeComponents()
    {
        _viewport = new ViewportState(Settings);
        _gridRenderer = new GridRenderer(Settings);
        _graphRenderer = new GraphRenderer(Settings);
        _graphRenderer.SetViewport(_viewport);
        _inputHandler = new CanvasInputHandler(Settings, _viewport, _graphRenderer);
        _clipboardManager = new ClipboardManager();
        _selectionManager = new SelectionManager(
            () => Graph,
            () => _graphRenderer,
            () => _theme,
            CommandHistory);
        _groupManager = new GroupManager(
            () => Graph,
            CommandHistory,
            Settings);
        _contextMenu = new FlowCanvasContextMenu(this);

        SubscribeToInputHandlerEvents();
        SubscribeToSelectionManagerEvents();
        SubscribeToGroupManagerEvents();
        
        _viewport.ViewportChanged += (_, _) => ApplyViewportTransforms();
    }

    private void SubscribeToInputHandlerEvents()
    {
        _inputHandler.ConnectionCompleted += OnConnectionCompleted;
        _inputHandler.EdgeClicked += OnEdgeClicked;
        _inputHandler.DeselectAllRequested += (_, _) => _selectionManager.DeselectAll();
        _inputHandler.SelectAllRequested += (_, _) => _selectionManager.SelectAll();
        _inputHandler.DeleteSelectedRequested += (_, _) => _selectionManager.DeleteSelected();
        _inputHandler.UndoRequested += (_, _) => Undo();
        _inputHandler.RedoRequested += (_, _) => Redo();
        _inputHandler.CopyRequested += (_, _) => Copy();
        _inputHandler.CutRequested += (_, _) => Cut();
        _inputHandler.PasteRequested += (_, _) => Paste();
        _inputHandler.DuplicateRequested += (_, _) => Duplicate();
        _inputHandler.GroupRequested += (_, _) => GroupSelected();
        _inputHandler.UngroupRequested += (_, _) => UngroupSelected();
        _inputHandler.GroupCollapseToggleRequested += (_, e) => ToggleGroupCollapse(e.GroupId);
        _inputHandler.NodesDragged += OnNodesDragged;
        _inputHandler.NodeResizing += OnNodeResizing;
        _inputHandler.NodeResized += OnNodeResized;
        _inputHandler.GridRenderRequested += (_, _) => RenderGrid();
    }

    private void SubscribeToSelectionManagerEvents()
    {
        _selectionManager.EdgesNeedRerender += (_, _) => RenderEdges();
    }

    private void SubscribeToGroupManagerEvents()
    {
        _groupManager.GroupCollapsedChanged += (s, e) =>
        {
            RenderGraph(); // Re-render to show/hide collapsed children
            GroupCollapsedChanged?.Invoke(this, e);
        };
        _groupManager.GroupNeedsRerender += (s, groupId) => RenderGraph();
        _groupManager.NodesAddedToGroup += (s, e) => RenderGraph();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        _mainCanvas = this.FindControl<Canvas>("MainCanvas");
        _gridCanvas = this.FindControl<Canvas>("GridCanvas");
        _rootPanel = this.FindControl<Panel>("RootPanel");
        _theme = new ThemeResources(this);

        SetupEventHandlers();
        
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _viewport.SetViewSize(Bounds.Size);
        }
        
        RenderAll();
        CenterOnGraph();
    }

    private void SetupEventHandlers()
    {
        if (_rootPanel != null)
        {
            _rootPanel.PointerPressed += OnRootPanelPointerPressed;
            _rootPanel.PointerMoved += OnRootPanelPointerMoved;
            _rootPanel.PointerReleased += OnRootPanelPointerReleased;
            _rootPanel.PointerWheelChanged += OnPointerWheelChanged;
        }
    }

    private void ApplyViewportTransforms()
    {
        _graphRenderer.SetViewport(_viewport);
        RenderAll();
    }

    #endregion

    #region Input Event Handlers

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_inputHandler.HandleKeyDown(e, Graph))
        {
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _inputHandler.HandlePointerWheelChanged(e, _rootPanel);
    }

    private void OnRootPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_rootPanel);
        
        // Handle right-click for context menu
        if (point.Properties.IsRightButtonPressed)
        {
            var screenPos = e.GetPosition(_rootPanel);
            var canvasPos = _viewport.ScreenToCanvas(screenPos);
            
            // Check what was clicked
            var hitElement = _mainCanvas?.InputHitTest(screenPos);
            
            if (hitElement is Control control && control.Tag is Node node)
            {
                // Select node if not already selected
                if (!node.IsSelected)
                {
                    foreach (var n in Graph?.Nodes ?? [])
                        n.IsSelected = false;
                    node.IsSelected = true;
                }
                _contextMenu.Show(control, e, new Core.Point(canvasPos.X, canvasPos.Y));
            }
            else if (hitElement is Control edgeControl && edgeControl.Tag is Edge edge)
            {
                if (!edge.IsSelected)
                {
                    foreach (var ed in Graph?.Edges ?? [])
                        ed.IsSelected = false;
                    edge.IsSelected = true;
                }
                _contextMenu.Show(edgeControl, e, new Core.Point(canvasPos.X, canvasPos.Y));
            }
            else
            {
                // Empty canvas
                _contextMenu.ShowCanvasMenu(this, new Core.Point(canvasPos.X, canvasPos.Y));
            }
            
            e.Handled = true;
            return;
        }
        
        _inputHandler.HandleRootPanelPointerPressed(e, _rootPanel, _mainCanvas, Graph);
        Focus();
    }

    private void OnRootPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        _inputHandler.HandleRootPanelPointerMoved(e, _rootPanel, Graph);
    }

    private void OnRootPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputHandler.HandleRootPanelPointerReleased(e, _rootPanel, _mainCanvas, Graph);
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.Tag is Node node)
        {
            var point = e.GetCurrentPoint(control);
            
            // Handle right-click for context menu
            if (point.Properties.IsRightButtonPressed)
            {
                // Select node if not already selected
                if (!node.IsSelected && Graph != null)
                {
                    foreach (var n in Graph.Nodes)
                        n.IsSelected = false;
                    node.IsSelected = true;
                }
                
                var screenPos = e.GetPosition(_rootPanel);
                var canvasPos = _viewport.ScreenToCanvas(screenPos);
                _contextMenu.Show(control, e, new Core.Point(canvasPos.X, canvasPos.Y));
                e.Handled = true;
                return;
            }
            
            _inputHandler.HandleNodePointerPressed(control, node, e, _rootPanel, Graph);
            Focus();
        }
    }

    private void OnNodePointerMoved(object? sender, PointerEventArgs e)
    {
        _inputHandler.HandleNodePointerMoved(e, _rootPanel, Graph);
    }

    private void OnNodePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputHandler.HandleNodePointerReleased(e, Graph);
    }

    private void OnPortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Ellipse portVisual && portVisual.Tag is (Node node, Port port, bool isOutput))
        {
            _inputHandler.HandlePortPointerPressed(portVisual, node, port, isOutput, e, _rootPanel, _mainCanvas, _theme);
        }
    }

    private void OnPortPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse portVisual)
        {
            _inputHandler.HandlePortPointerEntered(portVisual, _theme);
        }
    }

    private void OnPortPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse portVisual)
        {
            _inputHandler.HandlePortPointerExited(portVisual, _theme);
        }
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is global::Avalonia.Controls.Shapes.Path edgePath && edgePath.Tag is Edge edge)
        {
            var point = e.GetCurrentPoint(edgePath);
            
            // Handle right-click for context menu
            if (point.Properties.IsRightButtonPressed)
            {
                // Select edge if not already selected
                if (!edge.IsSelected && Graph != null)
                {
                    foreach (var n in Graph.Nodes)
                        n.IsSelected = false;
                    foreach (var ed in Graph.Edges)
                        ed.IsSelected = false;
                    edge.IsSelected = true;
                }
                
                var screenPos = e.GetPosition(_rootPanel);
                var canvasPos = _viewport.ScreenToCanvas(screenPos);
                _contextMenu.Show(edgePath, e, new Core.Point(canvasPos.X, canvasPos.Y));
                e.Handled = true;
                return;
            }
            
            _inputHandler.HandleEdgePointerPressed(edgePath, edge, e, Graph);
            Focus();
        }
    }

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e, Node node, ResizeHandlePosition position)
    {
        if (sender is Rectangle handle)
        {
            _inputHandler.HandleResizeHandlePointerPressed(handle, node, position, e, _rootPanel, Settings);
        }
    }

    private void OnResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        _inputHandler.HandleResizeHandlePointerMoved(e, _rootPanel, Settings);
    }

    private void OnResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputHandler.HandleResizeHandlePointerReleased(e);
    }

    #endregion

    #region Event Handlers for Commands

    private void OnConnectionCompleted(object? sender, ConnectionCompletedEventArgs e)
    {
        if (Graph == null) return;

        // Create connection context for validation
        var context = new ConnectionContext
        {
            SourceNode = e.SourceNode,
            SourcePort = e.SourcePort,
            TargetNode = e.TargetNode,
            TargetPort = e.TargetPort,
            Graph = Graph
        };

        // Validate connection if validator is set
        if (ConnectionValidator != null)
        {
            var result = ConnectionValidator.Validate(context);
            if (!result.IsValid)
            {
                // Connection rejected - could raise an event here for UI feedback
                ConnectionRejected?.Invoke(this, new ConnectionRejectedEventArgs(context, result.Message));
                return;
            }
        }

        // Check if connection already exists (fallback check)
        var existingEdge = Graph.Edges.FirstOrDefault(edge =>
            edge.Source == e.SourceNode.Id && edge.Target == e.TargetNode.Id &&
            edge.SourcePort == e.SourcePort.Id && edge.TargetPort == e.TargetPort.Id);

        if (existingEdge == null)
        {
            var newEdge = new Edge
            {
                Source = e.SourceNode.Id,
                Target = e.TargetNode.Id,
                SourcePort = e.SourcePort.Id,
                TargetPort = e.TargetPort.Id
            };

            CommandHistory.Execute(new AddEdgeCommand(Graph, newEdge));
        }
    }

    private void OnEdgeClicked(object? sender, EdgeClickedEventArgs e)
    {
        _selectionManager.HandleEdgeClicked(e.Edge, e.WasCtrlHeld);
    }

    private void OnNodesDragged(object? sender, NodesDraggedEventArgs e)
    {
        if (Graph == null) return;
        var command = new MoveNodesCommand(Graph, e.OldPositions, e.NewPositions);
        CommandHistory.Execute(new AlreadyExecutedCommand(command));
    }

    private void OnNodeResizing(object? sender, NodeResizingEventArgs e)
    {
        e.Node.Width = e.NewWidth;
        e.Node.Height = e.NewHeight;
        e.Node.Position = e.NewPosition;
        
        _graphRenderer.UpdateNodeSize(e.Node, _theme);
        _graphRenderer.UpdateNodePosition(e.Node);
        _graphRenderer.UpdateResizeHandlePositions(e.Node);
        RenderEdges();
    }

    private void OnNodeResized(object? sender, NodeResizedEventArgs e)
    {
        if (Graph == null) return;
        var command = new ResizeNodeCommand(
            Graph, e.Node.Id,
            e.OldWidth, e.OldHeight, e.NewWidth, e.NewHeight,
            e.OldPosition, e.NewPosition);
        CommandHistory.Execute(new AlreadyExecutedCommand(command));
    }

    #endregion

    #region Undo/Redo

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public void Undo() => CommandHistory.Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public void Redo() => CommandHistory.Redo();

    #endregion

    #region Clipboard Operations

    /// <summary>
    /// Copies selected nodes to the clipboard.
    /// </summary>
    public void Copy()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Count == 0) return;

        _clipboardManager.Copy(selectedNodes, Graph.Edges);
    }

    /// <summary>
    /// Cuts selected nodes to the clipboard.
    /// </summary>
    public void Cut()
    {
        if (Graph == null) return;

        Copy();
        _selectionManager.DeleteSelected();
    }

    /// <summary>
    /// Pastes nodes from the clipboard.
    /// </summary>
    public void Paste()
    {
        if (Graph == null || !_clipboardManager.HasContent) return;

        // Deselect current selection
        foreach (var node in Graph.Nodes)
        {
            node.IsSelected = false;
        }

        // Calculate paste position (center of viewport or with offset from original)
        var viewCenter = _viewport.ScreenToCanvas(new global::Avalonia.Point(
            _viewport.ViewSize.Width / 2,
            _viewport.ViewSize.Height / 2));
        var pastePosition = new Core.Point(viewCenter.X, viewCenter.Y);

        var (pastedNodes, pastedEdges) = _clipboardManager.Paste(Graph, pastePosition);

        if (pastedNodes.Count > 0)
        {
            var command = new PasteCommand(Graph, pastedNodes, pastedEdges);
            CommandHistory.Execute(new AlreadyExecutedCommand(command));
        }
    }

    /// <summary>
    /// Duplicates selected nodes in place.
    /// </summary>
    public void Duplicate()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Count == 0) return;

        // Deselect current selection
        foreach (var node in selectedNodes)
        {
            node.IsSelected = false;
        }

        // Duplicate with small offset
        var offset = new Core.Point(20, 20);
        var (duplicatedNodes, duplicatedEdges) = _clipboardManager.Duplicate(
            Graph, selectedNodes, Graph.Edges, offset);

        if (duplicatedNodes.Count > 0)
        {
            var command = new DuplicateCommand(Graph, duplicatedNodes, duplicatedEdges);
            CommandHistory.Execute(new AlreadyExecutedCommand(command));
        }
    }

    #endregion

    #region Group Operations

    /// <summary>
    /// Groups the selected nodes into a new group.
    /// </summary>
    /// <param name="groupLabel">Optional label for the group.</param>
    /// <returns>The created group node, or null if grouping failed.</returns>
    public Node? GroupSelected(string? groupLabel = null)
    {
        if (Graph == null) return null;

        var selectedNodes = Graph.Nodes
            .Where(n => n.IsSelected && !n.IsGroup)
            .ToList();

        if (selectedNodes.Count < 2) return null;

        var nodeIds = selectedNodes.Select(n => n.Id).ToList();
        var command = new GroupNodesCommand(Graph, nodeIds, groupLabel);
        CommandHistory.Execute(command);

        // Return the created group
        return Graph.Nodes.FirstOrDefault(n => n.IsGroup &&
            nodeIds.All(id => Graph.Nodes.FirstOrDefault(n2 => n2.Id == id)?.ParentGroupId == n.Id));
    }

    /// <summary>
    /// Ungroups the selected group(s).
    /// </summary>
    public void UngroupSelected()
    {
        if (Graph == null) return;

        var selectedGroups = Graph.Nodes
            .Where(n => n.IsSelected && n.IsGroup)
            .ToList();

        if (selectedGroups.Count == 0) return;

        // Ungroup all selected groups
        var commands = selectedGroups
            .Select(g => new UngroupNodesCommand(Graph, g.Id))
            .Cast<IGraphCommand>()
            .ToList();

        if (commands.Count == 1)
        {
            CommandHistory.Execute(commands[0]);
        }
        else
        {
            CommandHistory.Execute(new CompositeCommand("Ungroup multiple groups", commands));
        }
    }

    /// <summary>
    /// Toggles the collapsed state of a group.
    /// </summary>
    /// <param name="groupId">The ID of the group to toggle.</param>
    public void ToggleGroupCollapse(string groupId)
    {
        _groupManager.ToggleCollapse(groupId);
    }

    /// <summary>
    /// Sets the collapsed state of a group.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="collapsed">True to collapse, false to expand.</param>
    public void SetGroupCollapsed(string groupId, bool collapsed)
    {
        _groupManager.SetCollapsed(groupId, collapsed);
    }

    /// <summary>
    /// Collapses all groups in the graph.
    /// </summary>
    public void CollapseAllGroups()
    {
        _groupManager.CollapseAll();
    }

    /// <summary>
    /// Expands all groups in the graph.
    /// </summary>
    public void ExpandAllGroups()
    {
        _groupManager.ExpandAll();
    }

    /// <summary>
    /// Adds nodes to an existing group.
    /// </summary>
    /// <param name="groupId">The ID of the target group.</param>
    /// <param name="nodeIds">The IDs of the nodes to add.</param>
    public void AddNodesToGroup(string groupId, IEnumerable<string> nodeIds)
    {
        _groupManager.AddNodesToGroup(groupId, nodeIds);
    }

    /// <summary>
    /// Adds selected nodes to the specified group.
    /// </summary>
    /// <param name="groupId">The ID of the target group.</param>
    public void AddSelectedToGroup(string groupId)
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes
            .Where(n => n.IsSelected && !n.IsGroup && n.Id != groupId)
            .Select(n => n.Id)
            .ToList();

        if (selectedNodes.Count > 0)
        {
            AddNodesToGroup(groupId, selectedNodes);
        }
    }

    /// <summary>
    /// Removes nodes from their parent group.
    /// </summary>
    /// <param name="nodeIds">The IDs of the nodes to remove from their groups.</param>
    public void RemoveNodesFromGroup(IEnumerable<string> nodeIds)
    {
        _groupManager.RemoveNodesFromGroup(nodeIds);
    }

    /// <summary>
    /// Removes selected nodes from their parent groups.
    /// </summary>
    public void RemoveSelectedFromGroups()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes
            .Where(n => n.IsSelected && !string.IsNullOrEmpty(n.ParentGroupId))
            .Select(n => n.Id)
            .ToList();

        if (selectedNodes.Count > 0)
        {
            RemoveNodesFromGroup(selectedNodes);
        }
    }

    /// <summary>
    /// Auto-resizes a group to fit its children.
    /// </summary>
    /// <param name="groupId">The ID of the group to resize.</param>
    public void AutoResizeGroup(string groupId)
    {
        _groupManager.AutoResizeGroup(groupId);
    }

    /// <summary>
    /// Auto-resizes all groups to fit their children.
    /// </summary>
    public void AutoResizeAllGroups()
    {
        if (Graph == null) return;

        foreach (var group in Graph.Nodes.Where(n => n.IsGroup))
        {
            _groupManager.AutoResizeGroup(group.Id);
        }
    }

    /// <summary>
    /// Gets all groups in the graph.
    /// </summary>
    public IEnumerable<Node> GetAllGroups()
    {
        return _groupManager.GetAllGroups();
    }

    /// <summary>
    /// Gets the children of a specific group.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    public IEnumerable<Node> GetGroupChildren(string groupId)
    {
        return _groupManager.GetGroupChildren(groupId);
    }

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed parent group).
    /// </summary>
    /// <param name="nodeId">The ID of the node to check.</param>
    public bool IsNodeVisible(string nodeId)
    {
        return _groupManager.IsNodeVisible(nodeId);
    }

    /// <summary>
    /// Gets all visible nodes (excluding those hidden by collapsed groups).
    /// </summary>
    public IEnumerable<Node> GetVisibleNodes()
    {
        return _groupManager.GetVisibleNodes();
    }

    /// <summary>
    /// Gets the group at a specific canvas point.
    /// </summary>
    /// <param name="canvasPoint">The point in canvas coordinates.</param>
    /// <returns>The group ID, or null if no group at that point.</returns>
    public string? GetGroupAtPoint(Core.Point canvasPoint)
    {
        return _groupManager.GetGroupAtPoint(canvasPoint);
    }

    #endregion

    #region Graph Data Binding

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GraphProperty)
        {
            HandleGraphChanged(change.OldValue as Graph, change.NewValue as Graph);
        }
    }

    private void HandleGraphChanged(Graph? oldGraph, Graph? newGraph)
    {
        if (oldGraph != null)
        {
            oldGraph.Nodes.CollectionChanged -= OnNodesChanged;
            oldGraph.Edges.CollectionChanged -= OnEdgesChanged;
            UnsubscribeFromNodeChanges(oldGraph);
        }

        if (newGraph != null)
        {
            newGraph.Nodes.CollectionChanged += OnNodesChanged;
            newGraph.Edges.CollectionChanged += OnEdgesChanged;
            SubscribeToNodeChanges(newGraph);
            
            CenterOnGraph();
            ApplyViewportTransforms();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        
        var wasZeroSize = _viewport.ViewSize.Width <= 0 || _viewport.ViewSize.Height <= 0;
        _viewport.SetViewSize(e.NewSize);
        
        if (wasZeroSize && e.NewSize.Width > 0 && e.NewSize.Height > 0 && Graph != null)
        {
            CenterOnGraph();
            ApplyViewportTransforms();
        }
        else
        {
            RenderGrid();
        }
    }

    private void SubscribeToNodeChanges(Graph graph)
    {
        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged += OnNodePropertyChanged;
        }
    }

    private void UnsubscribeFromNodeChanges(Graph graph)
    {
        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Node node) return;

        switch (e.PropertyName)
        {
            case nameof(Node.Position):
                _graphRenderer.UpdateNodePosition(node);
                _graphRenderer.UpdateResizeHandlePositions(node);
                RenderEdges();
                break;
            case nameof(Node.IsSelected):
                _graphRenderer.UpdateNodeSelection(node, _theme);
                UpdateResizeHandlesForNode(node);
                break;
            case nameof(Node.Width):
            case nameof(Node.Height):
                _graphRenderer.UpdateNodeSize(node, _theme);
                _graphRenderer.UpdateNodePosition(node);
                _graphRenderer.UpdateResizeHandlePositions(node);
                RenderEdges();
                break;
            case nameof(Node.IsCollapsed):
                // Update resize handles when collapse state changes
                UpdateResizeHandlesForNode(node);
                break;
        }
    }

    private void UpdateResizeHandlesForNode(Node node)
    {
        if (_mainCanvas == null || _theme == null) return;

        // Don't show resize handles for collapsed groups
        bool shouldShowHandles = node.IsSelected && 
                                 node.IsResizable && 
                                 !(node.IsGroup && node.IsCollapsed);

        if (shouldShowHandles)
        {
            _graphRenderer.RenderResizeHandles(_mainCanvas, node, _theme, (handle, n, pos) =>
            {
                handle.PointerPressed += (s, e) => OnResizeHandlePointerPressed(s, e, n, pos);
                handle.PointerMoved += OnResizeHandlePointerMoved;
                handle.PointerReleased += OnResizeHandlePointerReleased;
            });
        }
        else
        {
            _graphRenderer.RemoveResizeHandles(_mainCanvas, node.Id);
        }
    }

    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (Node node in e.OldItems)
            {
                node.PropertyChanged -= OnNodePropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (Node node in e.NewItems)
            {
                node.PropertyChanged += OnNodePropertyChanged;
            }
        }

        RenderGraph();
    }

    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderEdges();
    }

    #endregion

    #region Rendering

    private void RenderAll()
    {
        RenderGrid();
        RenderGraph();
    }

    private void RenderGrid()
    {
        if (_gridCanvas == null || _theme == null) return;
        _gridRenderer.Render(_gridCanvas, Bounds.Size, _viewport, _theme.GridColor);
    }

    private void RenderGraph()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;

        _mainCanvas.Children.Clear();
        _graphRenderer.Clear();

        RenderEdges();
        
        _graphRenderer.RenderNodes(_mainCanvas, Graph, _theme, (control, node) =>
        {
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;
        });

        AttachPortEventHandlers();
    }

    private void RenderEdges()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;
        _graphRenderer.RenderEdges(_mainCanvas, Graph, _theme);
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

    #region Public API - Viewport

    /// <summary>
    /// Forces a re-render of all edges. Useful after modifying edge properties.
    /// </summary>
    public void RefreshEdges()
    {
        RenderEdges();
    }

    /// <summary>
    /// Forces a complete re-render of the graph.
    /// </summary>
    public void Refresh()
    {
        RenderAll();
    }

    /// <summary>
    /// Zooms in by one step, keeping the graph centered.
    /// </summary>
    public void ZoomIn()
    {
        _viewport.ZoomIn(GetGraphCenterInScreenCoords());
    }

    /// <summary>
    /// Zooms out by one step, keeping the graph centered.
    /// </summary>
    public void ZoomOut()
    {
        _viewport.ZoomOut(GetGraphCenterInScreenCoords());
    }

    /// <summary>
    /// Resets zoom to 100%, keeping the graph centered.
    /// </summary>
    public void ResetZoom()
    {
        _viewport.SetZoom(1.0, GetGraphCenterInScreenCoords());
    }

    /// <summary>
    /// Sets the zoom level.
    /// </summary>
    public void SetZoom(double zoom) => _viewport.SetZoom(zoom);

    /// <summary>
    /// Fits all nodes into the viewport.
    /// </summary>
    public void FitToView()
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        _viewport.FitToBounds(bounds, Bounds.Size);
        RenderGrid();
    }

    /// <summary>
    /// Centers the viewport on the center of all nodes without changing zoom.
    /// </summary>
    public void CenterOnGraph()
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        var center = new global::Avalonia.Point(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);
        
        _viewport.CenterOn(center);
        RenderGrid();
    }

    /// <summary>
    /// Centers the viewport on a specific point in canvas coordinates.
    /// </summary>
    public void CenterOn(double x, double y)
    {
        _viewport.CenterOn(new global::Avalonia.Point(x, y));
        RenderGrid();
    }

    private global::Avalonia.Point? GetGraphCenterInScreenCoords()
    {
        if (Graph == null || Graph.Nodes.Count == 0)
            return null;

        var bounds = CalculateGraphBounds();
        var center = new global::Avalonia.Point(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);
        
        return _viewport.CanvasToScreen(center);
    }

    private Rect CalculateGraphBounds()
    {
        if (Graph == null || Graph.Nodes.Count == 0)
            return default;

        var minX = Graph.Nodes.Min(n => n.Position.X);
        var minY = Graph.Nodes.Min(n => n.Position.Y);
        var maxX = Graph.Nodes.Max(n => n.Position.X + (n.Width ?? Settings.NodeWidth));
        var maxY = Graph.Nodes.Max(n => n.Position.Y + (n.Height ?? Settings.NodeHeight));

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion
}
