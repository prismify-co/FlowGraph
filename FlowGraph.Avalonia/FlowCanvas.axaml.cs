using Avalonia;
using Avalonia.Controls;
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
using LayoutNs = FlowGraph.Avalonia.Layout;

namespace FlowGraph.Avalonia;

/// <summary>
/// A canvas control for displaying and editing flow graphs.
/// This is the main partial class containing initialization, properties, and events.
/// Implements <see cref="IFlowCanvasContext"/> to provide graph context to manager classes.
/// </summary>
public partial class FlowCanvas : UserControl, IFlowCanvasContext
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
    /// Gets the port renderer registry for registering custom port types.
    /// </summary>
    public Rendering.PortRenderers.PortRendererRegistry PortRenderers => _graphRenderer.PortRenderers;

    /// <summary>
    /// Gets the visual control for a node by its ID.
    /// This can be used to access the node's rendered visual for customization (e.g., changing border color).
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>The node's visual control, or null if not found.</returns>
    public Control? GetNodeVisual(string nodeId) => _graphRenderer.GetNodeVisual(nodeId);

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
    /// Gets the context menu manager for customizing context menus.
    /// </summary>
    public FlowCanvasContextMenu ContextMenuManager => _contextMenu;

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
    /// </summary>
    public IConnectionValidator? ConnectionValidator { get; set; }

    #endregion

    #region Public Methods - Performance

    /// <summary>
    /// Enables simplified node rendering for better performance with large graphs.
    /// This replaces the default renderer with a minimal renderer that uses fewer visual elements.
    /// Call before loading a large graph.
    /// </summary>
    public void EnableSimplifiedRendering()
    {
        Settings.UseSimplifiedNodeRendering = true;
        var simplifiedRenderer = new Rendering.NodeRenderers.SimplifiedNodeRenderer();

        // Use simplified renderer for all types except groups
        NodeRenderers.SetDefaultRenderer(simplifiedRenderer);
        NodeRenderers.Register("input", simplifiedRenderer);
        NodeRenderers.Register("output", simplifiedRenderer);
        // Keep group renderer for proper group functionality
        NodeRenderers.Register("group", new Rendering.NodeRenderers.GroupNodeRenderer());
    }

    /// <summary>
    /// Disables simplified rendering and restores the default node renderers.
    /// </summary>
    public void DisableSimplifiedRendering()
    {
        Settings.UseSimplifiedNodeRendering = false;
        NodeRenderers.Reset();
    }

    /// <summary>
    /// Enables direct GPU-accelerated rendering mode for maximum performance with very large graphs.
    /// This bypasses the Avalonia visual tree entirely and draws directly to a DrawingContext.
    /// Supports custom node renderers via IDirectNodeRenderer interface.
    /// </summary>
    public void EnableDirectRendering()
    {
        _useDirectRendering = true;

        if (_directRenderer == null)
        {
            _directRenderer = new DirectGraphRenderer(Settings, _graphRenderer.NodeRenderers);
        }
        else
        {
            // Update the node renderers reference
            _directRenderer.NodeRenderers = _graphRenderer.NodeRenderers;
        }

        if (_mainCanvas != null && !_mainCanvas.Children.Contains(_directRenderer))
        {
            _mainCanvas.Children.Clear();
            _mainCanvas.Children.Add(_directRenderer);
            _directRenderer.Width = _mainCanvas.Bounds.Width;
            _directRenderer.Height = _mainCanvas.Bounds.Height;
        }
    }

    /// <summary>
    /// Disables direct rendering mode and returns to normal visual tree rendering.
    /// </summary>
    public void DisableDirectRendering()
    {
        _useDirectRendering = false;

        if (_mainCanvas != null && _directRenderer != null)
        {
            _mainCanvas.Children.Remove(_directRenderer);
        }

        // Force full re-render with normal mode
        RenderGraph();
    }

    /// <summary>
    /// Gets whether direct rendering mode is enabled.
    /// </summary>
    public bool IsDirectRenderingEnabled => _useDirectRendering;

    #endregion

    #region Events

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

    /// <summary>
    /// Event raised when a user double-clicks a node to edit its label.
    /// </summary>
    public event EventHandler<NodeLabelEditRequestedEventArgs>? NodeLabelEditRequested;

    /// <summary>
    /// Event raised when a user double-clicks an edge label to edit it.
    /// </summary>
    public event EventHandler<EdgeLabelEditRequestedEventArgs>? EdgeLabelEditRequested;

    #endregion

    #region Public Methods - Label Editing

    /// <summary>
    /// Begins inline editing for a node's label.
    /// In visual tree mode, uses the node's renderer to show an inline TextBox.
    /// In direct rendering mode, creates a TextBox overlay at the node's position.
    /// </summary>
    /// <param name="node">The node to edit.</param>
    /// <returns>True if editing started successfully.</returns>
    public bool BeginEditNodeLabel(Node node) => _labelEditManager.BeginEditNodeLabel(node);

    /// <summary>
    /// Ends inline editing for a node's label and commits the change.
    /// </summary>
    /// <param name="node">The node being edited.</param>
    public void EndEditNodeLabel(Node node) => _labelEditManager.EndEditNodeLabel(node);

    /// <summary>
    /// Gets whether a node is currently in edit mode.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is being edited.</returns>
    public bool IsEditingNodeLabel(Node node) => _labelEditManager.IsEditingNodeLabel(node);

    /// <summary>
    /// Begins inline editing for an edge's label.
    /// Shows an inline TextBox over the edge label.
    /// </summary>
    /// <param name="edge">The edge to edit.</param>
    /// <returns>True if editing started successfully.</returns>
    public bool BeginEditEdgeLabel(Edge edge) => _labelEditManager.BeginEditEdgeLabel(edge);

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
    private DirectGraphRenderer? _directRenderer;
    private InputStateMachine _inputStateMachine = null!;
    private InputStateContext _inputContext = null!;
    private SelectionManager _selectionManager = null!;
    private ClipboardManager _clipboardManager = null!;
    private GroupManager _groupManager = null!;
    private GroupProxyManager _groupProxyManager = null!;
    private FlowCanvasContextMenu _contextMenu = null!;
    private ThemeResources _theme = null!;
    private AnimationManager _animationManager = null!;
    private EdgeRoutingManager _edgeRoutingManager = null!;
    private LabelEditManager _labelEditManager = null!;

    // Rendering mode
    private bool _useDirectRendering;

    // Animation state
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
            context: this,
            getRenderer: () => _graphRenderer,
            getTheme: () => _theme,
            commandHistory: CommandHistory);
        _groupManager = new GroupManager(
            context: this,
            commandHistory: CommandHistory,
            settings: Settings);
        _groupProxyManager = new GroupProxyManager(() => Graph);
        _contextMenu = new FlowCanvasContextMenu(this);

        // Forward context menu rename requests to the same event
        _contextMenu.NodeLabelEditRequested += (_, e) => NodeLabelEditRequested?.Invoke(this, e);

        // Initialize edge routing manager
        _edgeRoutingManager = new EdgeRoutingManager(
            context: this,
            refreshEdges: RefreshEdges);

        // Initialize label edit manager
        _labelEditManager = new LabelEditManager(
            getGraph: () => Graph,
            getMainCanvas: () => _mainCanvas,
            getViewport: () => _viewport,
            getTheme: () => _theme,
            getGraphRenderer: () => _graphRenderer,
            getDirectRenderer: () => _directRenderer,
            getIsDirectRendering: () => _useDirectRendering,
            renderEdges: RenderEdges);

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

    #endregion

    #region Event Subscriptions

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

        // Forward drag lifecycle events
        _inputContext.NodeDragStart += (_, e) => NodeDragStart?.Invoke(this, e);
        _inputContext.NodeDragStop += (_, e) => NodeDragStop?.Invoke(this, e);

        // Forward connect lifecycle events
        _inputContext.ConnectStart += (_, e) => ConnectStart?.Invoke(this, e);
        _inputContext.ConnectEnd += (_, e) => ConnectEnd?.Invoke(this, e);

        // Handle edge reconnection/disconnection
        _inputContext.EdgeReconnected += OnEdgeReconnected;
        _inputContext.EdgeDisconnected += OnEdgeDisconnected;

        // Forward label edit request
        _inputContext.NodeLabelEditRequested += (_, e) => NodeLabelEditRequested?.Invoke(this, e);
        _inputContext.EdgeLabelEditRequested += (_, e) => EdgeLabelEditRequested?.Invoke(this, e);
    }

    private void SubscribeToSelectionManagerEvents()
    {
        _selectionManager.EdgeRerenderRequested += (_, _) => RenderEdges();
        _selectionManager.SelectionChanged += (_, e) => SelectionChanged?.Invoke(this, e);
    }

    private void SubscribeToGroupManagerEvents()
    {
        _groupManager.GroupCollapsedChanged += (s, e) =>
        {
            RenderGraph();
            GroupCollapsedChanged?.Invoke(this, e);
        };
        _groupManager.GroupRerenderRequested += (s, groupId) => RenderGraph();
        _groupManager.NodesAddedToGroup += (s, e) => RenderGraph();
    }

    #endregion

    #region Lifecycle

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _mainCanvas = this.FindControl<Canvas>("MainCanvas");
        _gridCanvas = this.FindControl<Canvas>("GridCanvas");
        _rootPanel = this.FindControl<Panel>("RootPanel");
        _theme = new ThemeResources(this);

        // Apply background setting
        if (!Settings.ShowBackground && _rootPanel != null)
        {
            _rootPanel.Background = Brushes.Transparent;
            Background = Brushes.Transparent;
        }

        // Update input context with UI elements
        _inputContext.RootPanel = _rootPanel;
        _inputContext.MainCanvas = _mainCanvas;
        _inputContext.Theme = _theme;
        _inputContext.ConnectionValidator = ConnectionValidator;

        SetupEventHandlers();

        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _viewport.SetViewSize(Bounds.Size);
        }

        // Defer initial render until after first layout pass to avoid redundant heavy renders.
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RenderAll();
            CenterOnGraph();
        }, global::Avalonia.Threading.DispatcherPriority.Loaded);
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

    #region Animation State Management

    private void OnAnimationCategoriesChanged(object? sender, AnimationCategoriesChangedEventArgs e)
    {
        UpdateAnimationInputState();
    }

    private void OnAnimationFrameUpdated(object? sender, EventArgs e)
    {
        if (!_animationManager.HasAnimations && IsInAnimationState())
        {
            _inputStateMachine.Reset();
        }
    }

    private void UpdateAnimationInputState()
    {
        if (!_animationManager.HasAnimations)
        {
            if (IsInAnimationState())
            {
                _inputStateMachine.Reset();
            }
            return;
        }

        var categories = _animationManager.ActiveCategories;

        if (categories.Contains(AnimationCategory.Group))
        {
            if (_inputStateMachine.CurrentStateName != AnimatingState.Instance.Name)
            {
                _inputStateMachine.TransitionTo(AnimatingState.Instance);
            }
        }
        else if (categories.Contains(AnimationCategory.Viewport))
        {
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
            if (IsInAnimationState())
            {
                _inputStateMachine.Reset();
            }
        }
    }

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

    #endregion

    #region Command Event Handlers

    private void OnConnectionCompleted(object? sender, ConnectionCompletedEventArgs e)
    {
        if (Graph == null) return;

        var context = new ConnectionContext
        {
            SourceNode = e.SourceNode,
            SourcePort = e.SourcePort,
            TargetNode = e.TargetNode,
            TargetPort = e.TargetPort,
            Graph = Graph
        };

        if (ConnectionValidator != null)
        {
            var result = ConnectionValidator.Validate(context);
            if (!result.IsValid)
            {
                ConnectionRejected?.Invoke(this, new ConnectionRejectedEventArgs(context, result.Message));
                return;
            }
        }

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

            CommandHistory.Execute(new AddEdgeCommand(Graph, newEdge));
            _edgeRoutingManager.RouteNewEdge(newEdge);
        }
    }

    private void OnEdgeClicked(object? sender, EdgeClickedEventArgs e)
    {
        _selectionManager.HandleEdgeClicked(e.Edge, e.WasCtrlHeld);
    }

    private void OnNodesDragging(object? sender, NodesDraggingEventArgs e)
    {
        _edgeRoutingManager.OnNodesDragging(e.NodeIds);
    }

    private void OnNodesDragged(object? sender, NodesDraggedEventArgs e)
    {
        if (Graph == null) return;

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

    private void OnEdgeReconnected(object? sender, EdgeReconnectedEventArgs e)
    {
        if (Graph == null) return;

        var commands = new List<IGraphCommand>
        {
            new RemoveEdgeCommand(Graph, e.OldEdge),
            new AddEdgeCommand(Graph, e.NewEdge)
        };

        CommandHistory.Execute(new CompositeCommand("Reconnect edge", commands));
    }

    private void OnEdgeDisconnected(object? sender, EdgeDisconnectedEventArgs e)
    {
        if (Graph == null) return;
        CommandHistory.Execute(new RemoveEdgeCommand(Graph, e.Edge));
    }

    #endregion
}
