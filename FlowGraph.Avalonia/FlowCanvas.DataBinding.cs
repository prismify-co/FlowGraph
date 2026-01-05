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
}
