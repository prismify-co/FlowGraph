// CS0618: Suppress obsolete warnings - FlowMinimap subscribes to CollectionChanged
// events on Graph.Nodes/Edges which require the ObservableCollection properties.
#pragma warning disable CS0618

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FlowGraph.Core;
using System.Collections.Specialized;
using System.ComponentModel;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Controls;

public partial class FlowMinimap : UserControl
{
    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowMinimap, FlowCanvas?>(nameof(TargetCanvas));

    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    private MinimapRenderControl? _minimapRenderControl;
    private bool _isDragging;
    private Graph? _subscribedGraph;
    private FlowCanvas? _subscribedCanvas;
    private ViewportState? _subscribedViewport;
    private Dictionary<string, Node>? _nodeById; // Cache for O(1) parent lookup

    // Transform: minimapPos = (canvasPos - extentOrigin) * scale
    private double _scale;
    private double _extentX;
    private double _extentY;

    private const double DefaultNodeWidth = 150;
    private const double DefaultNodeHeight = 80;
    
    // Throttling for minimap rendering during drag to avoid O(n) work per node position change
    private DateTime _lastRenderTime = DateTime.MinValue;
    private const int RenderThrottleMs = 50; // Max 20 fps for minimap during drag
    private bool _renderPending;

    public FlowMinimap()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _minimapRenderControl = this.FindControl<MinimapRenderControl>("MinimapRenderControl");

        if (_minimapRenderControl != null)
        {
            _minimapRenderControl.PointerPressed += OnMinimapPointerPressed;
            _minimapRenderControl.PointerMoved += OnMinimapPointerMoved;
            _minimapRenderControl.PointerReleased += OnMinimapPointerReleased;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TargetCanvasProperty)
        {
            UnsubscribeFromCanvas();
            UnsubscribeFromGraph();

            if (change.NewValue is FlowCanvas newCanvas)
            {
                SubscribeToCanvas(newCanvas);
                if (newCanvas.Graph != null)
                    SubscribeToGraph(newCanvas.Graph);
            }

            RenderMinimap();
        }
    }

    private void SubscribeToCanvas(FlowCanvas canvas)
    {
        _subscribedCanvas = canvas;
        canvas.PropertyChanged += OnTargetCanvasPropertyChanged;
        _subscribedViewport = canvas.Viewport;
        _subscribedViewport.ViewportChanged += OnViewportChanged;
    }

    private void UnsubscribeFromCanvas()
    {
        if (_subscribedViewport != null)
        {
            _subscribedViewport.ViewportChanged -= OnViewportChanged;
            _subscribedViewport = null;
        }
        if (_subscribedCanvas != null)
        {
            _subscribedCanvas.PropertyChanged -= OnTargetCanvasPropertyChanged;
            _subscribedCanvas = null;
        }
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        // Viewport changed - need full re-render because extent may change
        RenderMinimap();
    }

    private void OnTargetCanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FlowCanvas.GraphProperty)
        {
            UnsubscribeFromGraph();
            if (e.NewValue is Graph newGraph)
                SubscribeToGraph(newGraph);
            RenderMinimap();
        }
    }

    private void SubscribeToGraph(Graph graph)
    {
        if (_subscribedGraph == graph) return;
        UnsubscribeFromGraph();

        _subscribedGraph = graph;
        graph.NodesChanged += OnGraphChanged;
        graph.EdgesChanged += OnGraphChanged;
        graph.BatchLoadCompleted += OnBatchLoadCompleted;

        foreach (var node in graph.Nodes)
            node.PropertyChanged += OnNodeChanged;
    }

    private void UnsubscribeFromGraph()
    {
        if (_subscribedGraph == null) return;

        _subscribedGraph.NodesChanged -= OnGraphChanged;
        _subscribedGraph.EdgesChanged -= OnGraphChanged;
        _subscribedGraph.BatchLoadCompleted -= OnBatchLoadCompleted;

        foreach (var node in _subscribedGraph.Nodes)
            node.PropertyChanged -= OnNodeChanged;

        _subscribedGraph = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnsubscribeFromCanvas();
        UnsubscribeFromGraph();
    }

    private void OnGraphChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Skip UI updates during batch loading - will render once when batch completes
        if (_subscribedGraph?.IsBatchLoading == true)
        {
            // Still need to track property changes for new nodes
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    if (item is Node node)
                        node.PropertyChanged += OnNodeChanged;

            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                    if (item is Node node)
                        node.PropertyChanged -= OnNodeChanged;
            return;
        }

        if (e.NewItems != null)
            foreach (var item in e.NewItems)
                if (item is Node node)
                    node.PropertyChanged += OnNodeChanged;

        if (e.OldItems != null)
            foreach (var item in e.OldItems)
                if (item is Node node)
                    node.PropertyChanged -= OnNodeChanged;

        RenderMinimap();
    }

    private void OnBatchLoadCompleted(object? sender, EventArgs e)
    {
        // Re-subscribe to all nodes that were added during batch
        if (_subscribedGraph != null)
        {
            foreach (var node in _subscribedGraph.Elements.Nodes)
            {
                node.PropertyChanged -= OnNodeChanged; // Avoid double-subscribe
                node.PropertyChanged += OnNodeChanged;
            }
        }
        RenderMinimap();
    }

    private static long _nodeChangedCount = 0;
    private static long _nodeChangedSkippedCount = 0;
    
    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        _nodeChangedCount++;
        
        // Re-render on position, selection, or collapse state changes
        if (e.PropertyName == nameof(Node.Position) ||
            e.PropertyName == nameof(Node.IsSelected) ||
            e.PropertyName == nameof(Node.IsCollapsed) ||
            e.PropertyName == nameof(Node.Width) ||
            e.PropertyName == nameof(Node.Height))
        {
            // OPTIMIZED: Throttle minimap rendering during rapid updates (e.g., drag)
            // to avoid O(n) work for every node position change
            RenderMinimapThrottled();
        }
        else
        {
            _nodeChangedSkippedCount++;
        }
        
        if (_nodeChangedCount % 1000 == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Minimap] NodeChanged #{_nodeChangedCount}, prop={e.PropertyName}, skipped={_nodeChangedSkippedCount}");
        }
    }
    
    private static long _throttledRenderCount = 0;
    private static long _throttledSkippedCount = 0;
    
    /// <summary>
    /// Renders the minimap with throttling to avoid excessive updates during drag.
    /// </summary>
    private void RenderMinimapThrottled()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRenderTime).TotalMilliseconds;
        
        if (elapsed >= RenderThrottleMs)
        {
            // Enough time has passed, render immediately
            _lastRenderTime = now;
            _renderPending = false;
            _throttledRenderCount++;
            RenderMinimap();
        }
        else if (!_renderPending)
        {
            // Schedule a delayed render
            _renderPending = true;
            _throttledSkippedCount++;
            var delay = RenderThrottleMs - (int)elapsed;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_renderPending)
                {
                    _renderPending = false;
                    _lastRenderTime = DateTime.UtcNow;
                    _throttledRenderCount++;
                    RenderMinimap();
                }
            }, DispatcherPriority.Background);
        }
        else
        {
            _throttledSkippedCount++;
        }
        // else: render already pending, skip this update
        
        if ((_throttledRenderCount + _throttledSkippedCount) % 100 == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Minimap] ThrottledRender: rendered={_throttledRenderCount}, skipped={_throttledSkippedCount}");
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RenderMinimap();
    }

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed ancestor group).
    /// Uses cached dictionary for O(1) parent lookup.
    /// </summary>
    private bool IsNodeVisible(Graph graph, Node node)
    {
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            // Use O(1) dictionary lookup instead of O(n) FirstOrDefault
            if (_nodeById != null && _nodeById.TryGetValue(currentParentId, out var parent))
            {
                if (parent.IsCollapsed)
                    return false;
                currentParentId = parent.ParentGroupId;
            }
            else
            {
                // Fallback to slow path if cache not available
                var parentNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == currentParentId);
                if (parentNode == null) break;
                if (parentNode.IsCollapsed) return false;
                currentParentId = parentNode.ParentGroupId;
            }
        }
        return true;
    }

    /// <summary>
    /// Transform canvas coordinates to minimap coordinates.
    /// Like Nodify: position = canvasPos - Extent.Location, then scaled
    /// </summary>
    private AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
    {
        return new AvaloniaPoint(
            (canvasX - _extentX) * _scale,
            (canvasY - _extentY) * _scale
        );
    }

    /// <summary>
    /// Transform minimap coordinates to canvas coordinates.
    /// </summary>
    private AvaloniaPoint MinimapToCanvas(double minimapX, double minimapY)
    {
        return new AvaloniaPoint(
            minimapX / _scale + _extentX,
            minimapY / _scale + _extentY
        );
    }

    private static long _renderMinimapCount = 0;
    private static long _totalRenderMinimapMs = 0;
    
    private void RenderMinimap()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        if (_minimapRenderControl == null || TargetCanvas?.Graph == null)
            return;

        var graph = TargetCanvas.Graph;
        if (!graph.Elements.Nodes.Any())
        {
            _minimapRenderControl.Graph = null;
            _minimapRenderControl.InvalidateVisual();
            return;
        }

        // Build node cache for O(1) parent lookup (only rebuild if graph changed)
        if (_nodeById == null || _subscribedGraph != graph)
        {
            _nodeById = graph.Elements.Nodes.ToDictionary(n => n.Id);
        }

        // Get only visible nodes (not hidden by collapsed groups)
        // Use a single pass to collect visible nodes and calculate bounds simultaneously
        var visibleNodes = new List<Node>(graph.Elements.Nodes.Count);
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        
        foreach (var node in graph.Elements.Nodes)
        {
            if (!IsNodeVisible(graph, node))
                continue;
                
            visibleNodes.Add(node);
            
            var nodeWidth = GetNodeWidth(node);
            var nodeHeight = GetNodeHeight(node);
            
            if (node.Position.X < minX) minX = node.Position.X;
            if (node.Position.Y < minY) minY = node.Position.Y;
            if (node.Position.X + nodeWidth > maxX) maxX = node.Position.X + nodeWidth;
            if (node.Position.Y + nodeHeight > maxY) maxY = node.Position.Y + nodeHeight;
        }
        
        if (visibleNodes.Count == 0)
        {
            _minimapRenderControl.Graph = null;
            _minimapRenderControl.InvalidateVisual();
            return;
        }

        // Get minimap display area
        var minimapWidth = _minimapRenderControl.Bounds.Width;
        var minimapHeight = _minimapRenderControl.Bounds.Height;
        if (minimapWidth <= 0 || minimapHeight <= 0)
        {
            minimapWidth = Bounds.Width - 2;
            minimapHeight = Bounds.Height - 2;
        }
        if (minimapWidth <= 0 || minimapHeight <= 0)
            return;

        // ItemsExtent already calculated in single pass above
        var itemsExtent = new Rect(minX, minY, maxX - minX, maxY - minY);

        // Get ViewportLocation and ViewportSize
        var viewport = TargetCanvas.Viewport;
        var viewportRect = viewport.GetVisibleRect();

        Rect viewportBounds = default;
        if (viewportRect.Width > 0 && viewportRect.Height > 0)
        {
            viewportBounds = new Rect(viewportRect.X, viewportRect.Y, viewportRect.Width, viewportRect.Height);
        }

        // Calculate Extent = union of ItemsExtent and Viewport
        var extent = itemsExtent;
        if (viewportBounds.Width > 0 && viewportBounds.Height > 0)
        {
            extent = extent.Union(viewportBounds);
        }

        // Add padding
        const double padding = 20;
        extent = extent.Inflate(padding);

        if (extent.Width <= 0 || extent.Height <= 0)
            return;

        // Calculate scale to fit extent in minimap
        var scaleX = minimapWidth / extent.Width;
        var scaleY = minimapHeight / extent.Height;
        _scale = Math.Min(scaleX, scaleY);

        // Store extent origin for coordinate transforms
        _extentX = extent.X;
        _extentY = extent.Y;

        // Center the content
        var scaledWidth = extent.Width * _scale;
        var scaledHeight = extent.Height * _scale;
        var offsetX = (minimapWidth - scaledWidth) / 2;
        var offsetY = (minimapHeight - scaledHeight) / 2;

        // Adjust extent origin to account for centering
        _extentX -= offsetX / _scale;
        _extentY -= offsetY / _scale;

        // Pass data to render control and trigger repaint
        _minimapRenderControl.Graph = graph;
        _minimapRenderControl.VisibleNodes = visibleNodes;
        _minimapRenderControl.NodeById = _nodeById;
        _minimapRenderControl.Scale = _scale;
        _minimapRenderControl.ExtentX = _extentX;
        _minimapRenderControl.ExtentY = _extentY;
        _minimapRenderControl.ViewportBounds = viewportBounds;
        _minimapRenderControl.InvalidateVisual();
        
        sw.Stop();
        _renderMinimapCount++;
        _totalRenderMinimapMs += sw.ElapsedMilliseconds;
        if (_renderMinimapCount % 20 == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Minimap] RenderMinimap #{_renderMinimapCount}, last20avg={_totalRenderMinimapMs}ms, nodes={visibleNodes.Count}");
            _totalRenderMinimapMs = 0;
        }
    }

    /// <summary>
    /// Gets the display width for a node.
    /// </summary>
    private double GetNodeWidth(Node node)
    {
        return node.Width ?? DefaultNodeWidth;
    }

    /// <summary>
    /// Gets the display height for a node.
    /// For collapsed groups, returns the header height only.
    /// </summary>
    private double GetNodeHeight(Node node)
    {
        if (node.IsGroup && node.IsCollapsed)
            return 28; // Header height only
        return node.Height ?? DefaultNodeHeight;
    }

    /// <summary>
    /// Gets the nesting depth of a group for z-ordering.
    /// Uses cached dictionary for O(1) parent lookup.
    /// </summary>
    private int GetGroupDepth(Graph graph, Node node)
    {
        int depth = 0;
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            depth++;
            // Use O(1) dictionary lookup instead of O(n) FirstOrDefault
            if (_nodeById != null && _nodeById.TryGetValue(currentParentId, out var parent))
            {
                currentParentId = parent.ParentGroupId;
            }
            else
            {
                // Fallback to slow path if cache not available
                var parentNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == currentParentId);
                if (parentNode == null) break;
                currentParentId = parentNode.ParentGroupId;
            }
        }
        return depth;
    }

    private void OnMinimapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TargetCanvas?.Graph == null || !TargetCanvas.Graph.Elements.Nodes.Any())
            return;

        var point = e.GetCurrentPoint(_minimapRenderControl);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            NavigateToPoint(e.GetPosition(_minimapRenderControl));
            e.Pointer.Capture(_minimapRenderControl);
            e.Handled = true;
        }
    }

    private void OnMinimapPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            NavigateToPoint(e.GetPosition(_minimapRenderControl));
            e.Handled = true;
        }
    }

    private void OnMinimapPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void NavigateToPoint(AvaloniaPoint minimapPoint)
    {
        if (TargetCanvas == null || _scale <= 0)
            return;

        var canvasPoint = MinimapToCanvas(minimapPoint.X, minimapPoint.Y);
        TargetCanvas.CenterOn(canvasPoint.X, canvasPoint.Y);
    }
}
