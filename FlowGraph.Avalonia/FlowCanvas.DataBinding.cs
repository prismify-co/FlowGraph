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

    private void SubscribeToEdgeChanges(Graph graph)
    {
        foreach (var edge in graph.Edges)
        {
            edge.PropertyChanged += OnEdgePropertyChanged;
        }
    }

    private void UnsubscribeFromEdgeChanges(Graph graph)
    {
        foreach (var edge in graph.Edges)
        {
            edge.PropertyChanged -= OnEdgePropertyChanged;
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Node node) return;

        // In direct rendering mode, just invalidate the renderer for any visual change
        if (_useDirectRendering && _directRenderer != null)
        {
            if (e.PropertyName is nameof(Node.Position) or nameof(Node.IsSelected) 
                or nameof(Node.Width) or nameof(Node.Height) or nameof(Node.IsCollapsed))
            {
                _directRenderer.InvalidateVisual();
            }
            return;
        }

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
            foreach (var node in Graph.Nodes)
            {
                node.PropertyChanged -= OnNodePropertyChanged;
            }
            // Then subscribe to all
            foreach (var node in Graph.Nodes)
            {
                node.PropertyChanged += OnNodePropertyChanged;
            }
        }

        RenderGraph();
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
            foreach (var edge in Graph.Edges)
            {
                edge.PropertyChanged -= OnEdgePropertyChanged;
            }
            // Then subscribe to all
            foreach (var edge in Graph.Edges)
            {
                edge.PropertyChanged += OnEdgePropertyChanged;
            }
        }

        RenderEdges();
    }

    #endregion
}
