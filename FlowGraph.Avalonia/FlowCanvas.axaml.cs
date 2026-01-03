using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlowGraph.Avalonia;

/// <summary>
/// A canvas control for displaying and editing flow graphs.
/// </summary>
public partial class FlowCanvas : UserControl
{
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

    /// <summary>
    /// Gets the viewport state for external components (e.g., minimap).
    /// </summary>
    public ViewportState Viewport => _viewport;

    // UI Elements
    private Canvas? _mainCanvas;
    private Canvas? _gridCanvas;
    private Panel? _rootPanel;
    
    // Transform for main canvas - using MatrixTransform for precise control
    private readonly MatrixTransform _canvasTransform = new();

    // Components
    private ViewportState _viewport = null!;
    private GridRenderer _gridRenderer = null!;
    private GraphRenderer _graphRenderer = null!;
    private CanvasInputHandler _inputHandler = null!;
    private ThemeResources _theme = null!;

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
        _graphRenderer.SetViewport(_viewport); // Pass viewport to renderer
        _inputHandler = new CanvasInputHandler(Settings, _viewport, _graphRenderer);

        // Subscribe to input handler events
        _inputHandler.ConnectionCompleted += OnConnectionCompleted;
        _inputHandler.EdgeClicked += OnEdgeClicked;
        _inputHandler.DeselectAllRequested += (_, _) => DeselectAll();
        _inputHandler.SelectAllRequested += (_, _) => SelectAllNodes();
        _inputHandler.DeleteSelectedRequested += (_, _) => DeleteSelected();
        _inputHandler.GridRenderRequested += (_, _) => RenderGrid();

        // Subscribe to viewport changes
        _viewport.ViewportChanged += (_, _) => ApplyViewportTransforms();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        _mainCanvas = this.FindControl<Canvas>("MainCanvas");
        _gridCanvas = this.FindControl<Canvas>("GridCanvas");
        _rootPanel = this.FindControl<Panel>("RootPanel");
        _theme = new ThemeResources(this);

        SetupTransforms();
        SetupEventHandlers();
        
        // Set initial view size if bounds are available
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _viewport.SetViewSize(Bounds.Size);
        }
        
        RenderAll();
        
        // Center on graph after initial render
        CenterOnGraph();
    }

    private void SetupTransforms()
    {
        // No longer using RenderTransform - positions are transformed directly in GraphRenderer
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
        // Update the graph renderer with the current viewport
        _graphRenderer.SetViewport(_viewport);
        
        // Re-render everything with new transforms
        RenderAll();
    }

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
        if (sender is Border border && border.Tag is Node node)
        {
            _inputHandler.HandleNodePointerPressed(border, node, e, _rootPanel, Graph);
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
            _inputHandler.HandleEdgePointerPressed(edgePath, edge, e, Graph);
            Focus();
        }
    }

    private void OnConnectionCompleted(object? sender, ConnectionCompletedEventArgs e)
    {
        // Check if connection already exists
        var existingEdge = Graph?.Edges.FirstOrDefault(edge =>
            edge.Source == e.SourceNode.Id && edge.Target == e.TargetNode.Id &&
            edge.SourcePort == e.SourcePort.Id && edge.TargetPort == e.TargetPort.Id);

        if (existingEdge == null)
        {
            Graph?.AddEdge(new Edge
            {
                Source = e.SourceNode.Id,
                Target = e.TargetNode.Id,
                SourcePort = e.SourcePort.Id,
                TargetPort = e.TargetPort.Id
            });
        }
    }

    #endregion

    #region Selection Management

    private void DeleteSelected()
    {
        if (Graph == null) return;

        // Delete selected edges first
        var selectedEdges = Graph.Edges.Where(e => e.IsSelected).ToList();
        foreach (var edge in selectedEdges)
        {
            Graph.RemoveEdge(edge.Id);
        }

        // Then delete selected nodes (which also removes their edges)
        var selectedNodes = Graph.Nodes.Where(n => n.IsSelected).ToList();
        foreach (var node in selectedNodes)
        {
            Graph.RemoveNode(node.Id);
        }
    }

    private void SelectAllNodes()
    {
        if (Graph == null) return;

        foreach (var node in Graph.Nodes)
        {
            node.IsSelected = true;
        }
    }

    private void DeselectAll()
    {
        if (Graph == null) return;

        foreach (var node in Graph.Nodes)
        {
            node.IsSelected = false;
        }
        
        foreach (var edge in Graph.Edges)
        {
            edge.IsSelected = false;
        }
        
        // Re-render edges to update visual state
        RenderEdges();
    }

    private void OnEdgeClicked(object? sender, EdgeClickedEventArgs e)
    {
        if (Graph == null) return;
        
        // Update visual state for ALL edges (some may have been deselected)
        foreach (var edge in Graph.Edges)
        {
            _graphRenderer.UpdateEdgeSelection(edge, _theme);
        }
        
        // Also update node visuals if Ctrl was not held (nodes were deselected)
        if (!e.WasCtrlHeld)
        {
            foreach (var node in Graph.Nodes)
            {
                _graphRenderer.UpdateNodeSelection(node, _theme);
            }
        }
    }

    #endregion

    #region Graph Data Binding

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GraphProperty)
        {
            if (change.OldValue is Graph oldGraph)
            {
                oldGraph.Nodes.CollectionChanged -= OnNodesChanged;
                oldGraph.Edges.CollectionChanged -= OnEdgesChanged;
                UnsubscribeFromNodeChanges(oldGraph);
            }

            if (change.NewValue is Graph newGraph)
            {
                newGraph.Nodes.CollectionChanged += OnNodesChanged;
                newGraph.Edges.CollectionChanged += OnEdgesChanged;
                SubscribeToNodeChanges(newGraph);
                
                // First center on the graph (this updates the viewport offsets)
                CenterOnGraph();
                
                // Then render with the updated viewport
                // ApplyViewportTransforms will call RenderAll()
                ApplyViewportTransforms();
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        
        var wasZeroSize = _viewport.ViewSize.Width <= 0 || _viewport.ViewSize.Height <= 0;
        _viewport.SetViewSize(e.NewSize);
        
        // If this is the first time we have a valid size, center on the graph
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
        if (sender is Node node)
        {
            if (e.PropertyName == nameof(Node.Position))
            {
                _graphRenderer.UpdateNodePosition(node);
                RenderEdges();
            }
            else if (e.PropertyName == nameof(Node.IsSelected))
            {
                _graphRenderer.UpdateNodeSelection(node, _theme);
            }
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
        
        _graphRenderer.RenderNodes(_mainCanvas, Graph, _theme, (border, node) =>
        {
            border.PointerPressed += OnNodePointerPressed;
            border.PointerMoved += OnNodePointerMoved;
            border.PointerReleased += OnNodePointerReleased;
        });

        // Attach port event handlers
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
        
        // Note: Edge event handlers are attached in RenderEdges()
    }

    private void RenderEdges()
    {
        if (_mainCanvas == null || Graph == null || _theme == null) return;
        _graphRenderer.RenderEdges(_mainCanvas, Graph, _theme);
        
        // Re-attach edge event handlers after rendering
        AttachEdgeEventHandlers();
    }

    /// <summary>
    /// Attaches event handlers to all edge visuals.
    /// </summary>
    private void AttachEdgeEventHandlers()
    {
        if (Graph == null) return;
        
        foreach (var edge in Graph.Edges)
        {
            var edgeVisual = _graphRenderer.GetEdgeVisual(edge.Id);
            if (edgeVisual != null)
            {
                // Remove any existing handler to prevent duplicates
                edgeVisual.PointerPressed -= OnEdgePointerPressed;
                edgeVisual.PointerPressed += OnEdgePointerPressed;
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Zooms in by one step, keeping the graph centered.
    /// </summary>
    public void ZoomIn()
    {
        var zoomCenter = GetGraphCenterInScreenCoords();
        _viewport.ZoomIn(zoomCenter);
    }

    /// <summary>
    /// Zooms out by one step, keeping the graph centered.
    /// </summary>
    public void ZoomOut()
    {
        var zoomCenter = GetGraphCenterInScreenCoords();
        _viewport.ZoomOut(zoomCenter);
    }

    /// <summary>
    /// Resets zoom to 100%, keeping the graph centered.
    /// </summary>
    public void ResetZoom()
    {
        var zoomCenter = GetGraphCenterInScreenCoords();
        _viewport.SetZoom(1.0, zoomCenter);
    }

    /// <summary>
    /// Gets the center of the graph in screen coordinates.
    /// Falls back to view center if no nodes exist.
    /// </summary>
    private global::Avalonia.Point? GetGraphCenterInScreenCoords()
    {
        if (Graph == null || Graph.Nodes.Count == 0)
            return null; // Will use view center as fallback

        var minX = Graph.Nodes.Min(n => n.Position.X);
        var minY = Graph.Nodes.Min(n => n.Position.Y);
        var maxX = Graph.Nodes.Max(n => n.Position.X + Settings.NodeWidth);
        var maxY = Graph.Nodes.Max(n => n.Position.Y + Settings.NodeHeight);

        var graphCenterCanvas = new global::Avalonia.Point((minX + maxX) / 2, (minY + maxY) / 2);
        return _viewport.CanvasToScreen(graphCenterCanvas);
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

        var minX = Graph.Nodes.Min(n => n.Position.X);
        var minY = Graph.Nodes.Min(n => n.Position.Y);
        var maxX = Graph.Nodes.Max(n => n.Position.X + Settings.NodeWidth);
        var maxY = Graph.Nodes.Max(n => n.Position.Y + Settings.NodeHeight);

        var bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        _viewport.FitToBounds(bounds, Bounds.Size);
        RenderGrid();
    }

    /// <summary>
    /// Centers the viewport on the center of all nodes without changing zoom.
    /// </summary>
    public void CenterOnGraph()
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var minX = Graph.Nodes.Min(n => n.Position.X);
        var minY = Graph.Nodes.Min(n => n.Position.Y);
        var maxX = Graph.Nodes.Max(n => n.Position.X + Settings.NodeWidth);
        var maxY = Graph.Nodes.Max(n => n.Position.Y + Settings.NodeHeight);

        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        _viewport.CenterOn(new global::Avalonia.Point(centerX, centerY));
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

    /// <summary>
    /// Gets the current zoom level.
    /// </summary>
    public double CurrentZoom => _viewport.Zoom;

    #endregion
}
