// CS0618: Suppress obsolete warnings - FlowCanvas subscribes to CollectionChanged
// events on Graph.Nodes/Edges which require the ObservableCollection properties.
#pragma warning disable CS0618

using Avalonia;
using Avalonia.Controls;
using FlowGraph.Core;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Data binding and graph change handling.
/// </summary>
public partial class FlowCanvas
{
    #region Graph Data Binding

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GraphProperty)
        {
            HandleGraphChanged(change.OldValue as Graph, change.NewValue as Graph);
        }
        else if (change.Property == SettingsProperty)
        {
            HandleSettingsChanged(change.NewValue as FlowCanvasSettings);
        }
    }

    private void HandleSettingsChanged(FlowCanvasSettings? newSettings)
    {
        if (newSettings == null) return;

        // Propagate settings to all dependent components
        _graphRenderer.UpdateSettings(newSettings);
        _viewport.UpdateSettings(newSettings);
        _gridRenderer.UpdateSettings(newSettings);
        _inputContext?.UpdateSettings(newSettings);
        _directRenderer?.UpdateSettings(newSettings);

        // Re-render with new settings
        _graphNeedsRender = true;
        RenderAll();
    }

    private void HandleGraphChanged(Graph? oldGraph, Graph? newGraph)
    {
        if (oldGraph != null)
        {
            oldGraph.Nodes.CollectionChanged -= OnNodesChanged;
            oldGraph.Edges.CollectionChanged -= OnEdgesChanged;
            UnsubscribeFromNodeChanges(oldGraph);
            UnsubscribeFromEdgeChanges(oldGraph);
        }

        if (newGraph != null)
        {
            newGraph.Nodes.CollectionChanged += OnNodesChanged;
            newGraph.Edges.CollectionChanged += OnEdgesChanged;
            SubscribeToNodeChanges(newGraph);
            SubscribeToEdgeChanges(newGraph);

            _graphNeedsRender = true;
            CenterOnGraph();
            ApplyViewportTransforms();
        }
    }

    /// <inheritdoc/>
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
        foreach (var node in graph.Elements.Nodes)
        {
            node.PropertyChanged += OnNodePropertyChanged;
        }
    }

    private void UnsubscribeFromNodeChanges(Graph graph)
    {
        foreach (var node in graph.Elements.Nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }
    }

    private void SubscribeToEdgeChanges(Graph graph)
    {
        foreach (var edge in graph.Elements.Edges)
        {
            edge.PropertyChanged += OnEdgePropertyChanged;
        }
    }

    private void UnsubscribeFromEdgeChanges(Graph graph)
    {
        foreach (var edge in graph.Elements.Edges)
        {
            edge.PropertyChanged -= OnEdgePropertyChanged;
        }
    }

    private static long _nodePropertyChangedCount = 0;

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Node node) return;

        _nodePropertyChangedCount++;

        // Use unified render service - handles both retained and direct rendering modes
        switch (e.PropertyName)
        {
            case nameof(Node.Position):
                _renderService.UpdateNodePosition(node);
                _renderService.UpdateResizeHandlePositions(node);
                _renderService.RenderEdges();
                break;
            case nameof(Node.IsSelected):
                _renderService.UpdateNodeSelection(node);
                UpdateResizeHandlesForNode(node);
                break;
            case nameof(Node.Width):
            case nameof(Node.Height):
                _renderService.UpdateNodeSize(node);
                _renderService.UpdateNodePosition(node);
                _renderService.UpdateResizeHandlePositions(node);
                _renderService.RenderEdges();
                break;
            case nameof(Node.IsCollapsed):
                // Update resize handles when collapse state changes
                _renderService.Invalidate();
                UpdateResizeHandlesForNode(node);
                break;
        }

        if (_nodePropertyChangedCount % 1000 == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DataBinding] NodePropertyChanged #{_nodePropertyChangedCount}, prop={e.PropertyName}");
        }
    }

    private void OnEdgePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Edge edge) return;

        // In direct rendering mode, invalidate the renderer for visual changes
        if (_useDirectRendering && _directRenderer != null)
        {
            if (e.PropertyName is nameof(Edge.IsSelected))
            {
                _directRenderer.InvalidateVisual();
            }
            return;
        }

        // Normal rendering mode - update edge selection visual
        switch (e.PropertyName)
        {
            case nameof(Edge.IsSelected):
                _graphRenderer.UpdateEdgeSelection(edge, _theme);
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

        // For Reset action (e.g., from BulkObservableCollection.AddRange), 
        // re-subscribe to all nodes since NewItems is null
        if (e.Action == NotifyCollectionChangedAction.Reset && Graph != null)
        {
            // Unsubscribe from all first to avoid duplicates
            foreach (var node in Graph.Elements.Nodes)
            {
                node.PropertyChanged -= OnNodePropertyChanged;
            }
            // Then subscribe to all
            foreach (var node in Graph.Elements.Nodes)
            {
                node.PropertyChanged += OnNodePropertyChanged;
            }
        }

        _graphNeedsRender = true;
        RenderElements();
    }

    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (Edge edge in e.OldItems)
            {
                edge.PropertyChanged -= OnEdgePropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (Edge edge in e.NewItems)
            {
                edge.PropertyChanged += OnEdgePropertyChanged;
            }
        }

        // For Reset action (e.g., from BulkObservableCollection.AddRange), 
        // re-subscribe to all edges since NewItems is null
        if (e.Action == NotifyCollectionChangedAction.Reset && Graph != null)
        {
            // Unsubscribe from all first to avoid duplicates
            foreach (var edge in Graph.Elements.Edges)
            {
                edge.PropertyChanged -= OnEdgePropertyChanged;
            }
            // Then subscribe to all
            foreach (var edge in Graph.Elements.Edges)
            {
                edge.PropertyChanged += OnEdgePropertyChanged;
            }
        }

        _graphNeedsRender = true;
        // Defer rendering to next UI tick to ensure collection is fully updated.
        // The CollectionChanged event fires BEFORE the collection modification is complete,
        // so iterating over the collection immediately would see stale data.
        global::Avalonia.Threading.Dispatcher.UIThread.Post(RenderEdges, global::Avalonia.Threading.DispatcherPriority.Render);
    }

    #endregion
}
