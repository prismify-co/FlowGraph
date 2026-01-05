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
    /// </summary>
    public IConnectionValidator? ConnectionValidator { get; set; }

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
