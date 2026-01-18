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
using FlowGraph.Core.Coordinates;
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
    /// Gets the edge renderer registry for registering custom edge types.
    /// </summary>
    public Rendering.EdgeRenderers.EdgeRendererRegistry EdgeRenderers => _graphRenderer.EdgeRenderers;

    /// <summary>
    /// Gets the background renderer registry for custom background rendering.
    /// </summary>
    public Rendering.BackgroundRenderers.BackgroundRendererRegistry BackgroundRenderers => _graphRenderer.BackgroundRenderers;

    /// <summary>
    /// Gets the shape renderer registry for registering custom shape types.
    /// </summary>
    public Rendering.ShapeRenderers.ShapeRendererRegistry ShapeRenderers => Rendering.ShapeRenderers.ShapeRendererRegistry.Instance;

    /// <summary>
    /// Gets the visual control for a node by its ID.
    /// This can be used to access the node's rendered visual for customization (e.g., changing border color).
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>The node's visual control, or null if not found.</returns>
    public Control? GetNodeVisual(string nodeId) => _graphRenderer.GetNodeVisual(nodeId);

    /// <summary>
    /// Gets the visual control for a shape element by its ID.
    /// </summary>
    /// <param name="shapeId">The shape ID.</param>
    /// <returns>The shape's visual control, or null if not found.</returns>
    public Control? GetShapeVisual(string shapeId) => _shapeVisualManager?.GetVisual(shapeId);

    /// <summary>
    /// Gets the command history for undo/redo operations.
    /// </summary>
    public CommandHistory CommandHistory { get; } = new();

    /// <summary>
    /// Gets the current zoom level.
    /// </summary>
    public double CurrentZoom => _viewport.Zoom;

    /// <summary>
    /// Gets the current pan offset in canvas coordinates.
    /// </summary>
    public global::Avalonia.Point Offset => new(_viewport.OffsetX, _viewport.OffsetY);

    /// <summary>
    /// Gets canvas coordinates from a pointer event.
    /// This is the correct way to get coordinates for hit testing, node positioning, etc.
    /// </summary>
    /// <param name="e">The pointer event.</param>
    /// <returns>Position in canvas coordinates.</returns>
    /// <remarks>
    /// <para>This method uses <c>e.GetPosition(MainCanvas)</c> internally, which correctly</para>
    /// <para>accounts for the viewport transform. Prefer this over <see cref="ScreenToCanvas(double, double)"/>.</para>
    /// <para><b>DEPRECATED:</b> For new code, use <see cref="GetTypedCanvasPosition(global::Avalonia.Input.PointerEventArgs)"/> 
    /// which returns a type-safe <see cref="CanvasPoint"/> instead.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In your Pro extension service:
    /// var canvasPos = _canvas.GetCanvasPosition(e);
    /// var hitHandle = HitTest(canvasPos.X, canvasPos.Y);
    /// </code>
    /// </example>
    [System.Obsolete("Use GetTypedCanvasPosition(e) instead, which returns a type-safe CanvasPoint.")]
    public global::Avalonia.Point GetCanvasPosition(global::Avalonia.Input.PointerEventArgs e)
        => _mainCanvas != null ? e.GetPosition(_mainCanvas) : e.GetPosition(this);

    /// <summary>
    /// Gets viewport/screen coordinates from a pointer event.
    /// Use this for pan calculations, auto-pan edge detection, etc.
    /// </summary>
    /// <param name="e">The pointer event.</param>
    /// <returns>Position in viewport/screen coordinates.</returns>
    /// <remarks>
    /// <para><b>DEPRECATED:</b> For new code, use <see cref="GetTypedViewportPosition(global::Avalonia.Input.PointerEventArgs)"/>
    /// which returns a type-safe <see cref="ViewportPoint"/> instead.</para>
    /// </remarks>
    [System.Obsolete("Use GetTypedViewportPosition(e) instead, which returns a type-safe ViewportPoint.")]
    public global::Avalonia.Point GetViewportPosition(global::Avalonia.Input.PointerEventArgs e)
        => _rootPanel != null ? e.GetPosition(_rootPanel) : e.GetPosition(this);

    /// <summary>
    /// Gets canvas coordinates from a pointer event using the type-safe coordinate system.
    /// This is the <b>preferred method</b> for extension/Pro code to get canvas coordinates.
    /// </summary>
    /// <param name="e">The pointer event.</param>
    /// <returns>Position in canvas coordinates as a type-safe <see cref="CanvasPoint"/>.</returns>
    /// <remarks>
    /// <para>Use canvas coordinates for:</para>
    /// <list type="bullet">
    /// <item>Hit testing on the canvas</item>
    /// <item>Node/edge positions</item>
    /// <item>Selection box bounds</item>
    /// <item>Any position that needs to be stored or compared with node data</item>
    /// </list>
    /// <para>
    /// This method uses the same coordinate system as the internal input states,
    /// ensuring consistent behavior in both Visual Tree and Direct Rendering modes.
    /// </para>
    /// </remarks>
    public CanvasPoint GetTypedCanvasPosition(global::Avalonia.Input.PointerEventArgs e)
    {
        #pragma warning disable CS0618 // We're the new typed wrapper calling the old method
        var avaloniaPoint = GetCanvasPosition(e);
        #pragma warning restore CS0618
        return new CanvasPoint(avaloniaPoint.X, avaloniaPoint.Y);
    }

    /// <summary>
    /// Gets viewport coordinates from a pointer event using the type-safe coordinate system.
    /// This is the <b>preferred method</b> for extension/Pro code to get viewport coordinates.
    /// </summary>
    /// <param name="e">The pointer event.</param>
    /// <returns>Position in viewport coordinates as a type-safe <see cref="ViewportPoint"/>.</returns>
    /// <remarks>
    /// <para>Use viewport coordinates for:</para>
    /// <list type="bullet">
    /// <item>Pan calculations (delta from last position)</item>
    /// <item>Auto-pan edge detection</item>
    /// <item>UI overlay positioning</item>
    /// <item>Any position relative to the visible screen area</item>
    /// </list>
    /// </remarks>
    public ViewportPoint GetTypedViewportPosition(global::Avalonia.Input.PointerEventArgs e)
    {
        #pragma warning disable CS0618 // We're the new typed wrapper calling the old method
        var avaloniaPoint = GetViewportPosition(e);
        #pragma warning restore CS0618
        return new ViewportPoint(avaloniaPoint.X, avaloniaPoint.Y);
    }

    /// <summary>
    /// Converts canvas coordinates to screen coordinates.
    /// </summary>
    /// <param name="canvasX">X position in canvas coordinates.</param>
    /// <param name="canvasY">Y position in canvas coordinates.</param>
    /// <returns>The equivalent position in screen coordinates.</returns>
    public global::Avalonia.Point CanvasToScreen(double canvasX, double canvasY)
        => new((canvasX - _viewport.OffsetX) * _viewport.Zoom,
               (canvasY - _viewport.OffsetY) * _viewport.Zoom);

    /// <summary>
    /// Converts canvas coordinates to screen coordinates.
    /// </summary>
    /// <param name="canvasPoint">Position in canvas coordinates.</param>
    /// <returns>The equivalent position in screen coordinates.</returns>
    public global::Avalonia.Point CanvasToScreen(global::Avalonia.Point canvasPoint)
        => CanvasToScreen(canvasPoint.X, canvasPoint.Y);

    /// <summary>
    /// Converts viewport coordinates to canvas coordinates.
    /// </summary>
    /// <param name="screenX">X position in viewport coordinates.</param>
    /// <param name="screenY">Y position in viewport coordinates.</param>
    /// <returns>The equivalent position in canvas coordinates.</returns>
    /// <remarks>
    /// <para><b>WARNING:</b> Do not use this with <c>e.GetPosition(this)</c> for pointer events!</para>
    /// <para>For pointer events, use <see cref="GetCanvasPosition(global::Avalonia.Input.PointerEventArgs)"/> instead,</para>
    /// <para>which correctly handles the MainCanvas transform.</para>
    /// <para>This method is intended for converting known viewport coordinates (e.g., from bounds calculations).</para>
    /// </remarks>
    [System.Obsolete("For pointer events, use GetCanvasPosition(e) instead. This method is for viewport coordinate conversion only.")]
    public global::Avalonia.Point ScreenToCanvas(double screenX, double screenY)
        => new(screenX / _viewport.Zoom + _viewport.OffsetX,
               screenY / _viewport.Zoom + _viewport.OffsetY);

    /// <summary>
    /// Converts viewport coordinates to canvas coordinates.
    /// </summary>
    /// <param name="screenPoint">Position in viewport coordinates.</param>
    /// <returns>The equivalent position in canvas coordinates.</returns>
    /// <remarks>
    /// <para><b>WARNING:</b> Do not use this with <c>e.GetPosition(this)</c> for pointer events!</para>
    /// <para>For pointer events, use <see cref="GetCanvasPosition(global::Avalonia.Input.PointerEventArgs)"/> instead.</para>
    /// </remarks>
    [System.Obsolete("For pointer events, use GetCanvasPosition(e) instead. This method is for viewport coordinate conversion only.")]
    #pragma warning disable CS0618 // Suppress obsolete warning for internal call
    public global::Avalonia.Point ScreenToCanvas(global::Avalonia.Point screenPoint)
        => ScreenToCanvas(screenPoint.X, screenPoint.Y);
    #pragma warning restore CS0618

    /// <summary>
    /// Gets the visible bounds in canvas coordinates.
    /// This represents the area of the canvas currently visible in the viewport.
    /// </summary>
    public global::Avalonia.Rect VisibleBounds
    {
        get
        {
            #pragma warning disable CS0618 // ScreenToCanvas is obsolete for pointer events but valid for bounds
            var topLeft = ScreenToCanvas(0, 0);
            var bottomRight = ScreenToCanvas(Bounds.Width, Bounds.Height);
            #pragma warning restore CS0618
            return new global::Avalonia.Rect(topLeft, bottomRight);
        }
    }

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

    private ISnapProvider? _snapProvider;
    /// <summary>
    /// Gets or sets the snap provider for providing snap offsets during drag operations.
    /// External systems (like helper lines, guides) can register as a snap provider
    /// to influence node positions during drag without directly setting positions.
    /// </summary>
    public ISnapProvider? SnapProvider
    {
        get => _snapProvider;
        set
        {
            _snapProvider = value;
            // Update the input context immediately so drag operations see the change
            _inputContext.SnapProvider = value;
        }
    }

    private ICollisionProvider? _collisionProvider;
    /// <summary>
    /// Gets or sets the collision provider for preventing node overlap during drag.
    /// Applied after SnapProvider, allowing collision to override snap when needed.
    /// </summary>
    public ICollisionProvider? CollisionProvider
    {
        get => _collisionProvider;
        set
        {
            _collisionProvider = value;
            _inputContext.CollisionProvider = value;
        }
    }

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
            _directRenderer = new DirectCanvasRenderer(Settings, _graphRenderer.NodeRenderers);
        }
        else
        {
            // Update the node renderers reference and settings
            _directRenderer.NodeRenderers = _graphRenderer.NodeRenderers;
            _directRenderer.UpdateSettings(Settings);
        }

        // CRITICAL: Add DirectCanvasRenderer to _rootPanel, NOT _mainCanvas!
        // DirectCanvasRenderer does its own zoom/pan transforms internally,
        // so it must NOT be affected by the MatrixTransform on _mainCanvas.
        // This ensures:
        // 1. Rendering is in screen coordinates (no double transform)
        // 2. Hit testing receives screen coordinates directly
        if (_rootPanel != null && !_rootPanel.Children.Contains(_directRenderer))
        {
            // Clear visual tree mode elements from MainCanvas
            if (_mainCanvas != null)
            {
                _mainCanvas.Children.Clear();
            }

            // Clear visual manager tracking dictionaries
            // DirectCanvasRenderer will handle its own rendering
            _graphRenderer.Clear();

            // Add DirectCanvasRenderer to RootPanel (after GridCanvas, MainCanvas)
            _rootPanel.Children.Add(_directRenderer);
            _directRenderer.Width = _rootPanel.Bounds.Width;
            _directRenderer.Height = _rootPanel.Bounds.Height;

            // Update input context to trigger redraws on viewport changes
            _inputContext.DirectRenderer = _directRenderer;
        }
    }

    /// <summary>
    /// Disables direct rendering mode and returns to normal visual tree rendering.
    /// </summary>
    public void DisableDirectRendering()
    {
        _useDirectRendering = false;

        // Remove DirectCanvasRenderer from RootPanel (where we added it)
        if (_rootPanel != null && _directRenderer != null)
        {
            _rootPanel.Children.Remove(_directRenderer);
        }

        // Clear DirectRenderer from input context
        _inputContext.DirectRenderer = null;

        // Force full re-render with normal mode
        RenderElements();
    }

    /// <summary>
    /// Gets whether direct rendering mode is enabled.
    /// </summary>
    public bool IsDirectRenderingEnabled => _useDirectRendering;

    /// <summary>
    /// Gets the unified render service for programmatic rendering operations.
    /// This service abstracts the difference between retained and direct rendering modes,
    /// ensuring all operations work correctly regardless of which mode is active.
    /// </summary>
    public ICanvasRenderService RenderService => _renderService;

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

    /// <summary>
    /// Event raised when a user double-clicks a shape (e.g., sticky note) to edit its text.
    /// </summary>
    public event EventHandler<ShapeTextEditRequestedEventArgs>? ShapeTextEditRequested;

    /// <summary>
    /// Event raised when an edge is clicked.
    /// </summary>
    public event EventHandler<EdgeClickedEventArgs>? EdgeClicked;

    /// <summary>
    /// Event raised when a connection is successfully completed.
    /// </summary>
    public event EventHandler<ConnectionCompletedEventArgs>? ConnectionCompleted;

    /// <summary>
    /// Event raised when a node is resized.
    /// </summary>
    public event EventHandler<NodeResizedEventArgs>? NodeResized;

    /// <summary>
    /// Event raised when a node is being resized (in progress).
    /// </summary>
    public event EventHandler<NodeResizingEventArgs>? NodeResizing;

    /// <summary>
    /// Event raised when nodes have been dragged to a new position.
    /// </summary>
    public event EventHandler<NodesDraggedEventArgs>? NodesDragged;

    /// <summary>
    /// Event raised when an edge is reconnected to a different port.
    /// </summary>
    public event EventHandler<EdgeReconnectedEventArgs>? EdgeReconnected;

    /// <summary>
    /// Event raised when an edge is disconnected.
    /// </summary>
    public event EventHandler<EdgeDisconnectedEventArgs>? EdgeDisconnected;

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

    /// <summary>
    /// Begins inline editing for a shape's text (e.g., sticky notes).
    /// Shows an inline TextBox overlay on the shape.
    /// </summary>
    /// <param name="shape">The shape to edit.</param>
    /// <returns>True if editing started successfully.</returns>
    public bool BeginEditShapeText(Core.Elements.Shapes.ShapeElement shape) => _labelEditManager.BeginEditShapeText(shape);

    #endregion

    #region Private Fields

    // UI Elements
    private Canvas? _mainCanvas;
    private Canvas? _gridCanvas;
    private Panel? _rootPanel;
    private MatrixTransform? _viewportTransform;

    // Components
    private ViewportState _viewport = null!;
    private GridRenderer _gridRenderer = null!;
    private CanvasElementManager _graphRenderer = null!;
    private DirectCanvasRenderer? _directRenderer;
    private ICanvasRenderService _renderService = null!;
    private InputStateMachine _inputStateMachine = null!;
    private InputStateContext _inputContext = null!;
    private SelectionManager _selectionManager = null!;
    private ClipboardManager _clipboardManager = null!;
    private GroupManager _groupManager = null!;
    private GroupProxyManager _groupProxyManager = null!;
    private FlowCanvasContextMenu _contextMenu = null!;
    private ThemeResources _theme = null!;
    private AnimationManager _animationManager = null!;
    private Animation.EdgeFlowAnimationManager _edgeFlowManager = null!;
    private Animation.EdgeEffectsManager _edgeEffectsManager = null!;
    private EdgeRoutingManager _edgeRoutingManager = null!;
    private LabelEditManager _labelEditManager = null!;
    private Rendering.ShapeRenderers.ShapeVisualManager? _shapeVisualManager;

    // Rendering mode
    private bool _useDirectRendering;

    // Viewport optimization - track last state to detect what changed
    private double _lastZoom = 1.0;
    private double _lastOffsetX = 0;
    private double _lastOffsetY = 0;

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
        _graphRenderer = new CanvasElementManager(Settings);
        _graphRenderer.SetViewport(_viewport);
        _animationManager = new AnimationManager();

        // Initialize edge flow animation manager for automatic animated edges
        _edgeFlowManager = new Animation.EdgeFlowAnimationManager(
            _animationManager,
            (edge, offset) => UpdateEdgeDashOffset(edge, offset));

        // Initialize edge effects manager for rainbow/pulse effects
        _edgeEffectsManager = new Animation.EdgeEffectsManager(
            _animationManager,
            (edge, color) => UpdateEdgeColor(edge, color),
            (edge, opacity) => UpdateEdgeOpacity(edge, opacity));

        // Initialize unified render service that abstracts retained vs direct rendering
        _renderService = new CanvasRenderService(
            retainedRenderer: _graphRenderer,
            getDirectRenderer: () => _directRenderer,
            getIsDirectRenderingMode: () => _useDirectRendering,
            renderEdgesAction: RenderEdges,
            refreshAction: Refresh,
            getTheme: () => _theme);

        // Initialize input state machine
        _inputContext = new InputStateContext(Settings, _viewport, _graphRenderer);
        _inputStateMachine = new InputStateMachine(_inputContext);

        _clipboardManager = new ClipboardManager();
        _selectionManager = new SelectionManager(
            context: this,
            getRenderer: () => _graphRenderer,
            getTheme: () => _theme,
            commandHistory: CommandHistory);

        // Invalidate spatial index after undo/redo (commands may move nodes)
        CommandHistory.HistoryChanged += (_, _) => _directRenderer?.InvalidatePositions();

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
            animations: _animationManager,
            invalidatePositions: () => _directRenderer?.InvalidatePositions());
    }

    #endregion

    #region Event Subscriptions

    private void SubscribeToInputContextEvents()
    {
        _inputContext.ConnectionCompleted += OnConnectionCompleted;
        _inputContext.EdgeClicked += OnEdgeClicked;
        _inputContext.DeselectAllRequested += (_, _) => _selectionManager.DeselectAll();
        _inputContext.SelectAllRequested += (_, _) => _selectionManager.SelectAll();
        _inputContext.DeleteSelectedRequested += (_, _) =>
        {
            _selectionManager.DeleteSelected();
            Refresh();
        };
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
        // NOTE: GridRenderRequested is no longer needed to trigger renders.
        // Viewport changes are now handled efficiently through ViewportChanged -> ApplyViewportTransforms()
        // which uses transform-based panning (fast path) or full render (zoom changes).
        // The old handler was: (_, _) => { _graphNeedsRender = true; RenderAll(); }
        // which defeated the fast-path optimization.
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

        // Forward label edit request with default inline editing behavior
        _inputContext.NodeLabelEditRequested += (_, e) => 
        {
            NodeLabelEditRequested?.Invoke(this, e);
            // If not handled externally, start inline editing
            if (!e.Handled)
            {
                BeginEditNodeLabel(e.Node);
                e.Handled = true;
            }
        };
        _inputContext.EdgeLabelEditRequested += (_, e) => 
        {
            EdgeLabelEditRequested?.Invoke(this, e);
            if (!e.Handled)
            {
                BeginEditEdgeLabel(e.Edge);
                e.Handled = true;
            }
        };
        _inputContext.ShapeTextEditRequested += (_, e) => 
        {
            ShapeTextEditRequested?.Invoke(this, e);
            if (!e.Handled)
            {
                BeginEditShapeText(e.Shape);
                e.Handled = true;
            }
        };

        // Handle selection change request (used by shape clicks)
        _inputContext.SelectionChangeRequested += (_, _) => _selectionManager.NotifySelectionMayHaveChanged();
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
            RenderElements();
            GroupCollapsedChanged?.Invoke(this, e);
        };
        _groupManager.GroupRerenderRequested += (s, groupId) => RenderElements();
        _groupManager.NodesAddedToGroup += (s, e) => RenderElements();
    }

    #endregion

    #region Lifecycle

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _mainCanvas = this.FindControl<Canvas>("MainCanvas");
        _gridCanvas = this.FindControl<Canvas>("GridCanvas");
        _rootPanel = this.FindControl<Panel>("RootPanel");

        // Get the MatrixTransform from MainCanvas.RenderTransform
        if (_mainCanvas?.RenderTransform is MatrixTransform mt)
        {
            _viewportTransform = mt;
        }

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
        _inputContext.ViewportTransform = _viewportTransform;
        _inputContext.Theme = _theme;
        _inputContext.ConnectionValidator = ConnectionValidator;
        _inputContext.SnapProvider = SnapProvider;

        // Initialize shape visual manager now that canvas is available
        if (_mainCanvas != null)
        {
            _shapeVisualManager = new Rendering.ShapeRenderers.ShapeVisualManager(_mainCanvas);
            var renderContext = new Rendering.RenderContext(Settings);
            renderContext.SetViewport(_viewport);
            _shapeVisualManager.SetRenderContext(renderContext);
            _inputContext.ShapeVisualManager = _shapeVisualManager;
            _graphRenderer.SetShapeVisualManager(_shapeVisualManager);
        }

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

    private static int _fullRenderCount = 0;

    private void ApplyViewportTransforms()
    {
        _graphRenderer.SetViewport(_viewport);

        // Detect what changed for logging
        var zoomChanged = Math.Abs(_lastZoom - _viewport.Zoom) > 0.001;
        var offsetChanged = Math.Abs(_lastOffsetX - _viewport.OffsetX) > 0.1 ||
                            Math.Abs(_lastOffsetY - _viewport.OffsetY) > 0.1;

        // Phase 2: Transform-based pan/zoom with retained mode
        // Apply the viewport transform to MainCanvas for instant pan/zoom
        if (_viewportTransform != null)
        {
            _viewport.ApplyToTransforms(_viewportTransform);
        }

        // DirectRendering mode bypasses visual tree, so we need to trigger a redraw
        if (_useDirectRendering && _directRenderer != null && (zoomChanged || offsetChanged))
        {
            _directRenderer.InvalidateVisual();
        }

        // For zoom changes, only update elements that use InverseScale
        // (resize handles must stay constant screen size)
        // The MatrixTransform handles scaling for all other elements
        if (zoomChanged)
        {
            _fullRenderCount++;
            if (_fullRenderCount % 50 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewport] Zoom #{_fullRenderCount} (retained mode, updating handles only)");
            }

            _lastZoom = _viewport.Zoom;
            _lastOffsetX = _viewport.OffsetX;
            _lastOffsetY = _viewport.OffsetY;

            // Update resize handles (they use InverseScale for constant screen size)
            // Only for visual tree mode - DirectCanvasRenderer handles its own
            if (!_useDirectRendering)
            {
                _graphRenderer.UpdateAllResizeHandles();
            }

            // Update grid background (separate canvas, not transformed)
            RenderGrid();

            // Update custom background renderers (they render to GridCanvas which has no transform)
            RenderCustomBackgrounds();
        }
        else if (offsetChanged)
        {
            // Pan-only change - update transform and grid
            _lastOffsetX = _viewport.OffsetX;
            _lastOffsetY = _viewport.OffsetY;

            // Grid background is on separate untransformed canvas, must re-render
            RenderGrid();

            // Update custom background renderers (they render to GridCanvas which has no transform)
            RenderCustomBackgrounds();
        }
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
        // Invalidate spatial index if animations are moving nodes
        var categories = _animationManager.ActiveCategories;
        if (categories.Contains(Animation.AnimationCategory.NodePosition) ||
            categories.Contains(Animation.AnimationCategory.Layout) ||
            categories.Contains(Animation.AnimationCategory.Group))
        {
            _directRenderer?.InvalidatePositions();
        }

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

        var existingEdge = Graph.Elements.Edges.FirstOrDefault(edge =>
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

            ConnectionCompleted?.Invoke(this, e);
        }
    }

    private void OnEdgeClicked(object? sender, EdgeClickedEventArgs e)
    {
        _selectionManager.HandleEdgeClicked(e.Edge, e.WasCtrlHeld);
        EdgeClicked?.Invoke(this, e);
    }

    private void OnNodesDragging(object? sender, NodesDraggingEventArgs e)
    {
        _edgeRoutingManager.OnNodesDragging(e.NodeIds);
        // Invalidate spatial index so hit testing uses updated node positions
        // Use InvalidatePositions() instead of InvalidateIndex() to preserve _nodeById dictionary
        _directRenderer?.InvalidatePositions();
    }

    private void OnNodesDragged(object? sender, NodesDraggedEventArgs e)
    {
        if (Graph == null) return;

        _edgeRoutingManager.OnNodesDragCompleted(e.OldPositions.Keys);

        var command = new MoveNodesCommand(Graph, e.OldPositions, e.NewPositions);
        CommandHistory.Execute(new AlreadyExecutedCommand(command));

        NodesDragged?.Invoke(this, e);
    }

    private void OnNodeResizing(object? sender, NodeResizingEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[OnNodeResizing] Node={e.Node.Id}, NewSize={e.NewWidth}x{e.NewHeight}");
        e.Node.Width = e.NewWidth;
        e.Node.Height = e.NewHeight;
        e.Node.Position = e.NewPosition;

        // Use the batched resize update - handles both modes efficiently
        _renderService.UpdateNodeAfterResize(e.Node);

        NodeResizing?.Invoke(this, e);
    }

    private void OnNodeResized(object? sender, NodeResizedEventArgs e)
    {
        if (Graph == null) return;
        var command = new ResizeNodeCommand(
            Graph, e.Node.Id,
            e.OldWidth, e.OldHeight, e.NewWidth, e.NewHeight,
            e.OldPosition, e.NewPosition);
        CommandHistory.Execute(new AlreadyExecutedCommand(command));

        NodeResized?.Invoke(this, e);
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

        EdgeReconnected?.Invoke(this, e);
    }

    private void OnEdgeDisconnected(object? sender, EdgeDisconnectedEventArgs e)
    {
        if (Graph == null) return;
        CommandHistory.Execute(new RemoveEdgeCommand(Graph, e.Edge));

        EdgeDisconnected?.Invoke(this, e);
    }

    #endregion
}
