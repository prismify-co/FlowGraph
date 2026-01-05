using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Animation;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Routing;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using FlowGraph.Core.Commands;
using System.Collections.Specialized;
using System.ComponentModel;
using LayoutNs = FlowGraph.Avalonia.Layout;

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
    /// Gets the edge routing manager for controlling edge routing.
    /// </summary>
    public EdgeRoutingManager Routing => _edgeRoutingManager;

    /// <summary>
    /// Gets the current input state name (for debugging).
    /// </summary>
    public string CurrentInputState => _inputStateMachine.CurrentStateName;

    /// <summary>
    /// Gets the layout transition manager for animating layout/arrange operations.
    /// </summary>
    public LayoutNs.LayoutTransitionManager LayoutTransitions { get; private set; } = null!;

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

    /// <summary>
    /// Event raised when the input state changes.
    /// </summary>
    public event EventHandler<StateChangedEventArgs>? InputStateChanged;

    /// <summary>
    /// Event raised when the selection changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Event raised when the viewport changes (pan, zoom).
    /// </summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    /// <summary>
    /// Event raised when node dragging starts.
    /// </summary>
    public event EventHandler<NodeDragStartEventArgs>? NodeDragStart;

    /// <summary>
    /// Event raised when node dragging stops.
    /// </summary>
    public event EventHandler<NodeDragStopEventArgs>? NodeDragStop;

    /// <summary>
    /// Event raised when connection creation starts.
    /// </summary>
    public event EventHandler<ConnectStartEventArgs>? ConnectStart;

    /// <summary>
    /// Event raised when connection creation ends.
    /// </summary>
    public event EventHandler<ConnectEndEventArgs>? ConnectEnd;

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
    private InputStateMachine _inputStateMachine = null!;
    private InputStateContext _inputContext = null!;
    private SelectionManager _selectionManager = null!;
    private ClipboardManager _clipboardManager = null!;
    private GroupManager _groupManager = null!;
    private FlowCanvasContextMenu _contextMenu = null!;
    private ThemeResources _theme = null!;
    private AnimationManager _animationManager = null!;
    private EdgeRoutingManager _edgeRoutingManager = null!;

    // Tracks per-edge opacity overrides (used by group collapse/expand animations)
    private readonly Dictionary<string, double> _edgeOpacityOverrides = new();

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
            UpdateInputContextTheme();
            RenderAll();
        };
    }

    private void InitializeComponents()
    {
        _viewport = new ViewportState(Settings);
        _gridRenderer = new GridRenderer(Settings);
        _graphRenderer = new GraphRenderer(Settings);
        _graphRenderer.SetViewport(_viewport);
        _animationManager = new AnimationManager();
        
        // Initialize input state machine
        _inputContext = new InputStateContext(Settings, _viewport, _graphRenderer);
        _inputStateMachine = new InputStateMachine(_inputContext);
        
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

        // Initialize edge routing manager
        _edgeRoutingManager = new EdgeRoutingManager(
            () => Graph,
            () => Settings,
            RefreshEdges);

        SubscribeToInputContextEvents();
        SubscribeToSelectionManagerEvents();
        SubscribeToGroupManagerEvents();

        _viewport.ViewportChanged += OnViewportStateChanged;

        // Use refined animation states based on what's animating
        _animationManager.CategoriesChanged += OnAnimationCategoriesChanged;
        _animationManager.FrameUpdated += OnAnimationFrameUpdated;

        LayoutTransitions = new LayoutNs.LayoutTransitionManager(
            getGraph: () => Graph,
            refreshEdges: RefreshEdges,
            animations: _animationManager);
    }

    /// <summary>
    /// Handles animation category changes to update input state.
    /// </summary>
    private void OnAnimationCategoriesChanged(object? sender, AnimationCategoriesChangedEventArgs e)
    {
        UpdateAnimationInputState();
    }

    /// <summary>
    /// Handles animation frame updates to ensure state is current.
    /// </summary>
    private void OnAnimationFrameUpdated(object? sender, EventArgs e)
    {
        // Ensure we transition out of animation states when animations complete
        if (!_animationManager.HasAnimations)
        {
            if (IsInAnimationState())
            {
                _inputStateMachine.Reset();
            }
        }
    }

    /// <summary>
    /// Updates the input state based on currently running animations.
    /// </summary>
    private void UpdateAnimationInputState()
    {
        if (!_animationManager.HasAnimations)
        {
            // No animations - ensure we're in idle state
            if (IsInAnimationState())
            {
                _inputStateMachine.Reset();
            }
            return;
        }

        var categories = _animationManager.ActiveCategories;

        // Determine the appropriate animation state
        if (categories.Contains(AnimationCategory.Group))
        {
            // Group animations are modal - block all input
            if (_inputStateMachine.CurrentStateName != AnimatingState.Instance.Name)
            {
                _inputStateMachine.TransitionTo(AnimatingState.Instance);
            }
        }
        else if (categories.Contains(AnimationCategory.Viewport))
        {
            // Viewport animations allow cancellation and zoom wheel
            if (_inputStateMachine.CurrentStateName != "AnimatingViewport")
            {
                var viewportState = new AnimatingViewportState(
                    cancelAnimation: () => _animationManager.StopViewportAnimations());
                _inputStateMachine.TransitionTo(viewportState);
            }
        }
        else if (categories.Contains(AnimationCategory.NodePosition) || 
                 categories.Contains(AnimationCategory.NodeAppearance) ||
                 categories.Contains(AnimationCategory.Layout))
        {
            // Node animations allow viewport interactions
            if (_inputStateMachine.CurrentStateName != "AnimatingNodes")
            {
                var animatingNodeIds = _animationManager.AnimatingNodeIds;
                var nodesState = new AnimatingNodesState(
                    animatingNodeIds,
                    cancelAnimation: () => _animationManager.StopNodeAnimations());
                _inputStateMachine.TransitionTo(nodesState);
            }
        }
        else
        {
            // Other animations (edge, etc.) - allow most interactions
            // For now, treat as non-blocking
            if (IsInAnimationState())
            {
                _inputStateMachine.Reset();
            }
        }
    }

    /// <summary>
    /// Checks if the current input state is an animation state.
    /// </summary>
    private bool IsInAnimationState()
    {
        var stateName = _inputStateMachine.CurrentStateName;
        return stateName == AnimatingState.Instance.Name ||
               stateName == "AnimatingViewport" ||
               stateName == "AnimatingNodes";
    }

    private void OnViewportStateChanged(object? sender, EventArgs e)
    {
        ApplyViewportTransforms();
        RaiseViewportChanged();
    }

    private void RaiseViewportChanged()
    {
        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs(
            _viewport.Zoom,
            _viewport.OffsetX,
            _viewport.OffsetY,
            _viewport.GetVisibleRect()));
    }

    private void SubscribeToInputContextEvents()
    {
        _inputContext.ConnectionCompleted += OnConnectionCompleted;
        _inputContext.EdgeClicked += OnEdgeClicked;
        _inputContext.DeselectAllRequested += (_, _) => _selectionManager.DeselectAll();
        _inputContext.SelectAllRequested += (_, _) => _selectionManager.SelectAll();
        _inputContext.DeleteSelectedRequested += (_, _) => _selectionManager.DeleteSelected();
        _inputContext.UndoRequested += (_, _) => Undo();
        _inputContext.RedoRequested += (_, _) => Redo();
        _inputContext.CopyRequested += (_, _) => Copy();
        _inputContext.CutRequested += (_, _) => Cut();
        _inputContext.PasteRequested += (_, _) => Paste();
        _inputContext.DuplicateRequested += (_, _) => Duplicate();
        _inputContext.GroupRequested += (_, _) => GroupSelected();
        _inputContext.UngroupRequested += (_, _) => UngroupSelected();
        _inputContext.GroupCollapseToggleRequested += (_, e) => ToggleGroupCollapse(e.GroupId);
        _inputContext.NodesDragged += OnNodesDragged;
        _inputContext.NodesDragging += OnNodesDragging;
        _inputContext.NodeResizing += OnNodeResizing;
        _inputContext.NodeResized += OnNodeResized;
        _inputContext.GridRenderRequested += (_, _) => RenderGrid();
        _inputContext.StateChanged += (_, e) => InputStateChanged?.Invoke(this, e);
    }

    private void SubscribeToSelectionManagerEvents()
    {
        _selectionManager.EdgesNeedRerender += (_, _) => RenderEdges();
        _selectionManager.SelectionChanged += (_, e) => SelectionChanged?.Invoke(this, e);
    }

    private void SubscribeToGroupManagerEvents()
    {
        _groupManager.GroupCollapsedChanged += (s, e) =>
        {
            RenderGraph();
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

        // Apply background setting - make transparent if ShowBackground is false
        // This allows FlowBackground control to show through
        if (!Settings.ShowBackground && _rootPanel != null)
        {
            _rootPanel.Background = Brushes.Transparent;
            Background = Brushes.Transparent;
        }

        // Update input context with UI elements
        _inputContext.RootPanel = _rootPanel;
        _inputContext.MainCanvas = _mainCanvas;
        _inputContext.Theme = _theme;

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

    private void UpdateInputContextTheme()
    {
        if (_inputContext != null)
        {
            _inputContext.Theme = _theme;
        }
    }

    #endregion

    #region Input Event Handlers

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_inputStateMachine.HandleKeyDown(e))
        {
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _inputStateMachine.HandlePointerWheel(e);
    }

    private void OnRootPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_rootPanel);
        
        // Handle right-click for context menu
        if (point.Properties.IsRightButtonPressed)
        {
            HandleContextMenuRequest(e, null, null);
            return;
        }

        // Update context with current graph
        _inputContext.Graph = Graph;
        
        // Determine the source control for state handling
        var screenPos = e.GetPosition(_rootPanel);
        var hitElement = _mainCanvas?.InputHitTest(screenPos);
        
        _inputStateMachine.HandlePointerPressed(e, hitElement as Control);
        Focus();
    }

    private void OnRootPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerMoved(e);
    }

    private void OnRootPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerReleased(e);
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.Tag is Node node)
        {
            var point = e.GetCurrentPoint(control);
            
            if (point.Properties.IsRightButtonPressed)
            {
                HandleContextMenuRequest(e, control, node);
                return;
            }
            
            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, control);
            Focus();
        }
    }

    private void OnNodePointerMoved(object? sender, PointerEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerMoved(e);
    }

    private void OnNodePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerReleased(e);
    }

    private void OnPortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Ellipse portVisual)
        {
            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, portVisual);
        }
    }

    private void OnPortPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse portVisual && _theme != null)
        {
            portVisual.Fill = _theme.PortHover;
        }
    }

    private void OnPortPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse portVisual && _theme != null)
        {
            portVisual.Fill = _theme.PortBackground;
        }
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is global::Avalonia.Controls.Shapes.Path edgePath && edgePath.Tag is Edge edge)
        {
            var point = e.GetCurrentPoint(edgePath);
            
            if (point.Properties.IsRightButtonPressed)
            {
                HandleContextMenuRequest(e, edgePath, edge);
                return;
            }
            
            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, edgePath);
            Focus();
        }
    }

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e, Node node, ResizeHandlePosition position)
    {
        if (sender is Rectangle handle)
        {
            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, handle);
        }
    }

    private void OnResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerMoved(e);
    }

    private void OnResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerReleased(e);
    }

    /// <summary>
    /// Handles right-click context menu requests for nodes, edges, or empty canvas.
    /// </summary>
    private void HandleContextMenuRequest(PointerPressedEventArgs e, Control? target, object? targetObject)
    {
        var screenPos = e.GetPosition(_rootPanel);
        var canvasPos = _viewport.ScreenToCanvas(screenPos);
        var canvasPoint = new Core.Point(canvasPos.X, canvasPos.Y);

        if (targetObject is Node node)
        {
            // Select node if not already selected
            if (!node.IsSelected && Graph != null)
            {
                foreach (var n in Graph.Nodes)
                    n.IsSelected = false;
                node.IsSelected = true;
            }
            _contextMenu.Show(target!, e, canvasPoint);
        }
        else if (targetObject is Edge edge)
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
            _contextMenu.Show(target!, e, canvasPoint);
        }
        else
        {
            // Empty canvas - check if we hit a node or edge via hit testing
            var hitElement = _mainCanvas?.InputHitTest(screenPos);
            
            if (hitElement is Control control && control.Tag is Node hitNode)
            {
                if (!hitNode.IsSelected)
                {
                    foreach (var n in Graph?.Nodes ?? [])
                        n.IsSelected = false;
                    hitNode.IsSelected = true;
                }
                _contextMenu.Show(control, e, canvasPoint);
            }
            else if (hitElement is Control edgeControl && edgeControl.Tag is Edge hitEdge)
            {
                if (!hitEdge.IsSelected)
                {
                    foreach (var ed in Graph?.Edges ?? [])
                        ed.IsSelected = false;
                    hitEdge.IsSelected = true;
                }
                _contextMenu.Show(edgeControl, e, canvasPoint);
            }
            else
            {
                // Empty canvas
                _contextMenu.ShowCanvasMenu(this, canvasPoint);
            }
        }
        
        e.Handled = true;
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
                TargetPort = e.TargetPort.Id,
                Type = Settings.DefaultEdgeType
            };

            // Add the edge first
            CommandHistory.Execute(new AddEdgeCommand(Graph, newEdge));

            // Route the new edge if enabled
            _edgeRoutingManager.RouteNewEdge(newEdge);
        }
    }

    private void OnEdgeClicked(object? sender, EdgeClickedEventArgs e)
    {
        _selectionManager.HandleEdgeClicked(e.Edge, e.WasCtrlHeld);
    }

    private void OnNodesDragging(object? sender, NodesDraggingEventArgs e)
    {
        // Trigger edge re-routing during drag if enabled
        _edgeRoutingManager.OnNodesDragging(e.NodeIds);
    }

    private void OnNodesDragged(object? sender, NodesDraggedEventArgs e)
    {
        if (Graph == null) return;
        
        // Trigger final edge re-routing after drag completes
        _edgeRoutingManager.OnNodesDragCompleted(e.OldPositions.Keys);
        
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

    #region Animation API

    /// <summary>
    /// Gets the animation manager for controlling animations.
    /// </summary>
    public AnimationManager Animations => _animationManager;

    /// <summary>
    /// Smoothly animates to fit all nodes into the viewport.
    /// </summary>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void FitToViewAnimated(double duration = 0.3, Func<double, double>? easing = null)
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        var animation = ViewportAnimation.FitToBounds(_viewport, bounds, 50, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to center on the graph.
    /// </summary>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void CenterOnGraphAnimated(double duration = 0.3, Func<double, double>? easing = null)
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        var center = new global::Avalonia.Point(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);

        var animation = ViewportAnimation.CenterOn(_viewport, center, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to center on a specific point.
    /// </summary>
    /// <param name="x">X coordinate in canvas coordinates.</param>
    /// <param name="y">Y coordinate in canvas coordinates.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void CenterOnAnimated(double x, double y, double duration = 0.3, Func<double, double>? easing = null)
    {
        var animation = ViewportAnimation.CenterOn(
            _viewport, 
            new global::Avalonia.Point(x, y), 
            duration, 
            easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to center on a specific node.
    /// </summary>
    /// <param name="node">The node to center on.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void CenterOnNodeAnimated(Node node, double duration = 0.3, Func<double, double>? easing = null)
    {
        var (width, height) = _graphRenderer.GetNodeDimensions(node);
        var center = new global::Avalonia.Point(
            node.Position.X + width / 2,
            node.Position.Y + height / 2);

        var animation = ViewportAnimation.CenterOn(_viewport, center, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to a specific zoom level.
    /// </summary>
    /// <param name="targetZoom">Target zoom level.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.2).</param>
    /// <param name="easing">Optional easing function.</param>
    public void ZoomToAnimated(double targetZoom, double duration = 0.2, Func<double, double>? easing = null)
    {
        var animation = ViewportAnimation.ZoomTo(_viewport, targetZoom, null, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates a node to a new position.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="targetPosition">Target position in canvas coordinates.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void AnimateNodeTo(Node node, Core.Point targetPosition, double duration = 0.3, Func<double, double>? easing = null)
    {
        var animation = new NodeAnimation(node, targetPosition, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates multiple nodes to new positions.
    /// </summary>
    /// <param name="nodePositions">Dictionary mapping nodes to their target positions.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void AnimateNodesTo(IReadOnlyDictionary<Node, Core.Point> nodePositions, double duration = 0.3, Func<double, double>? easing = null)
    {
        var animation = new MultiNodeAnimation(nodePositions, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Stops all running animations.
    /// </summary>
    public void StopAllAnimations()
    {
        _animationManager.StopAll();
    }

    /// <summary>
    /// Animates an edge with a fade-in effect.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgeFadeIn(Edge edge, double duration = 0.3, Action? onComplete = null)
    {
        var animation = EdgeFadeAnimation.FadeIn(
            edge,
            duration,
            (e, opacity) => UpdateEdgeOpacity(e, opacity),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates an edge with a fade-out effect.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgeFadeOut(Edge edge, double duration = 0.3, Action? onComplete = null)
    {
        var animation = EdgeFadeAnimation.FadeOut(
            edge,
            duration,
            (e, opacity) => UpdateEdgeOpacity(e, opacity),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates an edge with a pulse/highlight effect.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="pulseCount">Number of pulse cycles (default: 2).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgePulse(Edge edge, int pulseCount = 2, Action? onComplete = null)
    {
        var animation = new EdgePulseAnimation(
            edge,
            baseThickness: 2,
            pulseAmount: 3,
            frequency: 2,
            duration: pulseCount * 0.5,
            (e, thickness) => UpdateEdgeThickness(e, thickness),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Starts a continuous flow animation on an edge (animated dashed line).
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="speed">Speed of the flow animation (default: 50).</param>
    /// <param name="reverse">If true, flow goes from target to source. If false (default), flow goes from source to target.</param>
    /// <returns>The animation instance (can be used to stop it later).</returns>
    public EdgeFlowAnimation StartEdgeFlowAnimation(Edge edge, double speed = 50, bool reverse = false)
    {
        var animation = new EdgeFlowAnimation(
            edge,
            speed,
            reverse,
            (e, offset) => UpdateEdgeDashOffset(e, offset));
        _animationManager.Start(animation);
        return animation;
    }

    /// <summary>
    /// Stops an edge flow animation.
    /// </summary>
    /// <param name="animation">The animation to stop.</param>
    public void StopEdgeFlowAnimation(EdgeFlowAnimation animation)
    {
        _animationManager.Stop(animation);
    }

    /// <summary>
    /// Animates an edge's color.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="targetColor">Target color.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgeColor(Edge edge, Color targetColor, double duration = 0.3, Action? onComplete = null)
    {
        // Get the current color from the visible path
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        var currentColor = visiblePath?.Stroke is SolidColorBrush brush 
            ? brush.Color 
            : (_theme?.EdgeStroke is SolidColorBrush themeBrush ? themeBrush.Color : Colors.Gray);
        
        var animation = new EdgeColorAnimation(
            edge,
            currentColor,
            targetColor,
            duration,
            onUpdate: (e, color) => UpdateEdgeColor(e, color),
            onComplete: onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Updates an edge's opacity and keeps all associated visuals in sync (visible stroke, hit area, markers).
    /// Also records the opacity in an override map so it can be re-applied if edges are re-rendered during animations.
    /// </summary>
    private void UpdateEdgeOpacity(Edge edge, double opacity)
    {
        _edgeOpacityOverrides[edge.Id] = opacity;

        // Update the visible path (the actual rendered stroke)
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            visiblePath.Opacity = opacity;
        }

        // Keep the hit area and markers in sync so they don't lag/persist visually
        var hitArea = _graphRenderer.GetEdgeVisual(edge.Id);
        if (hitArea != null)
        {
            hitArea.Opacity = opacity;
        }

        var markers = _graphRenderer.GetEdgeMarkers(edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.Opacity = opacity;
            }
        }

        var label = _graphRenderer.GetEdgeLabel(edge.Id);
        if (label != null)
        {
            label.Opacity = opacity;
        }
    }

    /// <summary>
    /// Updates an edge's stroke thickness on the currently rendered edge path.
    /// </summary>
    private void UpdateEdgeThickness(Edge edge, double thickness)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            visiblePath.StrokeThickness = thickness * _viewport.Zoom;
        }
    }

    /// <summary>
    /// Updates an edge's dash offset on the currently rendered edge path.
    /// Ensures a dash array exists so the offset has a visible effect.
    /// </summary>
    private void UpdateEdgeDashOffset(Edge edge, double offset)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            if (visiblePath.StrokeDashArray == null || visiblePath.StrokeDashArray.Count == 0)
            {
                visiblePath.StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 5, 5 };
            }

            visiblePath.StrokeDashOffset = offset;
        }
    }

    /// <summary>
    /// Updates an edge's stroke color on the currently rendered edge path.
    /// </summary>
    private void UpdateEdgeColor(Edge edge, Color color)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            visiblePath.Stroke = new SolidColorBrush(color);
        }
    }

    /// <summary>
    /// Re-applies any active edge opacity overrides after edges are re-rendered.
    /// This prevents edges and markers from popping back to full opacity mid-animation.
    /// </summary>
    private void ApplyEdgeOpacityOverrides()
    {
        if (Graph == null) return;

        // Prune missing edges
        var existing = Graph.Edges.Select(e => e.Id).ToHashSet();
        foreach (var edgeId in _edgeOpacityOverrides.Keys.Where(id => !existing.Contains(id)).ToList())
        {
            _edgeOpacityOverrides.Remove(edgeId);
        }

        foreach (var (edgeId, opacity) in _edgeOpacityOverrides)
        {
            var edge = Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge == null) continue;
            UpdateEdgeOpacity(edge, opacity);
        }
    }

    /// <summary>
    /// Clears opacity overrides for the provided edges.
    /// </summary>
    private void ClearEdgeOpacityOverrides(IEnumerable<Edge> edges)
    {
        foreach (var e in edges)
        {
            _edgeOpacityOverrides.Remove(e.Id);
        }
    }

    /// <summary>
    /// Gets the set of edges that should participate in a group collapse/expand fade.
    /// This includes both internal edges (child-to-child) and boundary edges (child-to-outside).
    /// </summary>
    private List<Edge> GetEdgesForGroupFade(Graph graph, string groupId)
    {
        var children = graph.GetGroupChildren(groupId).ToList();
        var childIds = children.Select(c => c.Id).ToHashSet();

        // Include both:
        // - internal edges (child->child)
        // - boundary edges (child->outside)
        return graph.Edges
            .Where(e => childIds.Contains(e.Source) || childIds.Contains(e.Target))
            .ToList();
    }

    /// <summary>
    /// Smoothly animates a node appearing with fade and scale effect.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateNodeAppear(Node node, double duration = 0.3, Action? onComplete = null)
    {
        var animation = NodeAppearAnimation.Appear(
            node,
            duration,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates a node disappearing with fade and scale effect.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.2).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateNodeDisappear(Node node, double duration = 0.2, Action? onComplete = null)
    {
        var animation = NodeAppearAnimation.Disappear(
            node,
            duration,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates multiple nodes appearing with staggered effect.
    /// </summary>
    /// <param name="nodes">The nodes to animate.</param>
    /// <param name="duration">Animation duration per node in seconds (default: 0.3).</param>
    /// <param name="stagger">Delay between each node's animation start (default: 0.05).</param>
    /// <param name="onComplete">Optional callback when all animations complete.</param>
    public void AnimateNodesAppear(IEnumerable<Node> nodes, double duration = 0.3, double stagger = 0.05, Action? onComplete = null)
    {
        var animation = new MultiNodeAppearAnimation(
            nodes,
            appearing: true,
            duration,
            stagger,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates multiple nodes disappearing with staggered effect.
    /// </summary>
    /// <param name="nodes">The nodes to animate.</param>
    /// <param name="duration">Animation duration per node in seconds (default: 0.2).</param>
    /// <param name="stagger">Delay between each node's animation start (default: 0.03).</param>
    /// <param name="onComplete">Optional callback when all animations complete.</param>
    public void AnimateNodesDisappear(IEnumerable<Node> nodes, double duration = 0.2, double stagger = 0.03, Action? onComplete = null)
    {
        var animation = new MultiNodeAppearAnimation(
            nodes,
            appearing: false,
            duration,
            stagger,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates a brief selection pulse on a node.
    /// </summary>
    /// <param name="node">The node to pulse.</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateSelectionPulse(Node node, Action? onComplete = null)
    {
        var animation = new SelectionPulseAnimation(
            0.4,
            intensity => UpdateNodeSelectionPulse(node, intensity),
            onComplete);
        _animationManager.Start(animation);
    }

    private void UpdateNodeAppearance(Node node, double opacity, double scale)
    {
        var nodeVisual = _graphRenderer.GetNodeVisual(node.Id);
        if (nodeVisual != null)
        {
            nodeVisual.Opacity = opacity;
            nodeVisual.RenderTransform = new ScaleTransform(scale, scale);
            nodeVisual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }

        // Also update ports
        foreach (var port in node.Inputs.Concat(node.Outputs))
        {
            var portVisual = _graphRenderer.GetPortVisual(node.Id, port.Id);
            if (portVisual != null)
            {
                portVisual.Opacity = opacity;
            }
        }
    }

    private void UpdateNodeSelectionPulse(Node node, double intensity)
    {
        var nodeVisual = _graphRenderer.GetNodeVisual(node.Id);
        if (nodeVisual is Border border && _theme != null)
        {
            // Interpolate border thickness for pulse effect
            var baseThickness = node.IsSelected ? 3 : 2;
            var pulseThickness = baseThickness + intensity * 2;
            border.BorderThickness = new Thickness(pulseThickness);
        }
    }

    /// <summary>
    /// Animates a group collapsing with sequenced transitions:
    /// 1. Children nodes and edges fade out together
    /// 2. Group shrinks
    /// </summary>
    /// <param name="groupId">The ID of the group to collapse.</param>
    /// <param name="duration">Total animation duration in seconds (default: 0.5).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateGroupCollapse(string groupId, double duration = 0.5, Action? onComplete = null)
    {
        if (Graph == null || _mainCanvas == null || _theme == null) return;

        var group = Graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || group.IsCollapsed) return;

        var expandedWidth = group.Width ?? 200;
        var expandedHeight = group.Height ?? 100;
        var collapsedWidth = 150.0;
        var collapsedHeight = 40.0;

        // Get children and connected/boundary edges
        var children = Graph.GetGroupChildren(groupId).ToList();
        var connectedEdges = GetEdgesForGroupFade(Graph, groupId);

        // Hide resize handles during animation
        _graphRenderer.RemoveResizeHandles(_mainCanvas, groupId);

        // Phase durations (content fade -> shrink)
        var contentFadeDuration = duration * 0.5;
        var shrinkDuration = duration * 0.5;

        // PHASE 1: Fade out edges AND nodes together (in parallel)
        var contentFadeAnimation = new Animation.GenericAnimation(
            contentFadeDuration,
            t =>
            {
                var opacity = 1.0 - Easing.EaseInCubic(t);

                // Fade out edges (visible + hit area + markers)
                foreach (var edge in connectedEdges)
                {
                    UpdateEdgeOpacity(edge, opacity);
                }

                // Fade out nodes and ports
                foreach (var child in children)
                {
                    var childVisual = _graphRenderer.GetNodeVisual(child.Id);
                    if (childVisual != null) childVisual.Opacity = opacity;
                    foreach (var port in child.Inputs.Concat(child.Outputs))
                    {
                        var portVisual = _graphRenderer.GetPortVisual(child.Id, port.Id);
                        if (portVisual != null) portVisual.Opacity = opacity;
                    }
                }
            },
            onComplete: () =>
            {
                // PHASE 2: Shrink group
                var shrinkAnimation = new Animation.GenericAnimation(
                    shrinkDuration,
                    t =>
                    {
                        var easedT = Easing.EaseInOutCubic(t);
                        group.Width = expandedWidth + (collapsedWidth - expandedWidth) * easedT;
                        group.Height = expandedHeight + (collapsedHeight - expandedHeight) * easedT;
                        _graphRenderer.UpdateNodeSize(group, _theme);
                        _graphRenderer.UpdateNodePosition(group);

                        // This re-renders edges; ensure overrides are re-applied in RenderEdges()
                        RenderEdges();
                    },
                    onComplete: () =>
                    {
                        // Final: Set collapsed state (triggers re-render)
                        _groupManager.SetCollapsed(groupId, true);

                        ClearEdgeOpacityOverrides(connectedEdges);
                        onComplete?.Invoke();
                    });
                _animationManager.Start(shrinkAnimation);
            });
        _animationManager.Start(contentFadeAnimation);
    }

    public void AnimateGroupExpand(string groupId, double duration = 0.5, Action? onComplete = null)
    {
        if (Graph == null || _mainCanvas == null || _theme == null) return;

        var group = Graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || !group.IsCollapsed) return;

        var collapsedWidth = group.Width ?? 150;
        var collapsedHeight = group.Height ?? 40;

        // Hide resize handles during animation
        _graphRenderer.RemoveResizeHandles(_mainCanvas, groupId);

        // Get children info and calculate expanded size
        var children = Graph.GetGroupChildren(groupId).ToList();
        if (children.Count == 0)
        {
            _groupManager.SetCollapsed(groupId, false);
            onComplete?.Invoke();
            return;
        }

        var connectedEdges = GetEdgesForGroupFade(Graph, groupId);

        // Calculate expanded size from children
        var padding = 20.0;
        var headerHeight = 30.0;
        var minX = children.Min(n => n.Position.X);
        var minY = children.Min(n => n.Position.Y);
        var maxX = children.Max(n => n.Position.X + (n.Width ?? Settings.NodeWidth));
        var maxY = children.Max(n => n.Position.Y + (n.Height ?? Settings.NodeHeight));
        var expandedWidth = maxX - minX + padding * 2;
        var expandedHeight = maxY - minY + padding * 2 + headerHeight;

        // Set IsCollapsed = false directly (avoid triggering re-render event)
        group.IsCollapsed = false;

        // Manually render children at opacity 0
        foreach (var child in children)
        {
            var control = _graphRenderer.RenderNode(_mainCanvas, child, _theme, null);
            control.Opacity = 0;
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;

            foreach (var port in child.Inputs.Concat(child.Outputs))
            {
                var portVisual = _graphRenderer.GetPortVisual(child.Id, port.Id);
                if (portVisual != null)
                {
                    portVisual.Opacity = 0;
                    portVisual.PointerPressed += OnPortPointerPressed;
                    portVisual.PointerEntered += OnPortPointerEntered;
                    portVisual.PointerExited += OnPortPointerExited;
                }
            }
        }

        // Re-render edges then set them to opacity 0 (and record override)
        RenderEdges();
        foreach (var edge in connectedEdges)
        {
            UpdateEdgeOpacity(edge, 0);
        }

        // Phase durations (expand -> content fade)
        var expandDuration = duration * 0.5;
        var contentFadeDuration = duration * 0.5;

        // PHASE 1: Expand group
        var expandAnimation = new Animation.GenericAnimation(
            expandDuration,
            t =>
            {
                var easedT = Easing.EaseOutCubic(t);
                group.Width = collapsedWidth + (expandedWidth - collapsedWidth) * easedT;
                group.Height = collapsedHeight + (expandedHeight - collapsedHeight) * easedT;
                _graphRenderer.UpdateNodeSize(group, _theme);
                _graphRenderer.UpdateNodePosition(group);
            },
            onComplete: () =>
            {
                // PHASE 2: Fade in nodes AND edges together (in parallel)
                var contentFadeAnimation = new Animation.GenericAnimation(
                    contentFadeDuration,
                    t =>
                    {
                        var opacity = Easing.EaseOutCubic(t);

                        // Fade in nodes and ports
                        foreach (var child in children)
                        {
                            var childVisual = _graphRenderer.GetNodeVisual(child.Id);
                            if (childVisual != null) childVisual.Opacity = opacity;
                            foreach (var port in child.Inputs.Concat(child.Outputs))
                            {
                                var portVisual = _graphRenderer.GetPortVisual(child.Id, port.Id);
                                if (portVisual != null) portVisual.Opacity = opacity;
                            }
                        }

                        // Fade in edges and markers
                        foreach (var edge in connectedEdges)
                        {
                            UpdateEdgeOpacity(edge, opacity);
                        }
                    },
                    onComplete: () =>
                    {
                        ClearEdgeOpacityOverrides(connectedEdges);

                        // Restore resize handles if group is still selected
                        if (group.IsSelected && _mainCanvas != null)
                        {
                            UpdateResizeHandlesForNode(group);
                        }
                        onComplete?.Invoke();
                    });
                _animationManager.Start(contentFadeAnimation);
            });
        _animationManager.Start(expandAnimation);
    }

    #endregion
}
