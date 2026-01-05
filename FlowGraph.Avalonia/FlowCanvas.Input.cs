using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Core;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Input event handling.
/// </summary>
public partial class FlowCanvas
{
    #region Input Event Handlers

    /// <inheritdoc/>
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

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e, Node node, Rendering.ResizeHandlePosition position)
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

    #endregion

    #region Context Menu

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
}
