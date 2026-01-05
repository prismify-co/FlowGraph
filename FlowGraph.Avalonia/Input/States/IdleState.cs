using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// The default idle state - handles initial interactions and transitions to other states.
/// </summary>
public class IdleState : InputStateBase
{
    public static IdleState Instance { get; } = new();

    public override string Name => "Idle";

    public override StateTransitionResult HandlePointerPressed(InputStateContext context, PointerPressedEventArgs e, Control? source)
    {
        var point = GetPointerPoint(context, e);
        var position = GetPosition(context, e);

        // Middle mouse button always starts panning
        if (point.Properties.IsMiddleButtonPressed)
        {
            var panState = new PanningState(position, context.Viewport);
            CapturePointer(e, context.RootPanel);
            return StateTransitionResult.TransitionTo(panState);
        }

        // Left click handling
        if (point.Properties.IsLeftButtonPressed)
        {
            // Check what was clicked via source control's Tag
            if (source?.Tag is Node node)
            {
                return HandleNodeClick(context, e, source, node, position);
            }
            
            if (source?.Tag is Edge edge)
            {
                return HandleEdgeClick(context, e, edge);
            }

            if (source?.Tag is (Node portNode, Port port, bool isOutput))
            {
                return HandlePortClick(context, e, source as Ellipse, portNode, port, isOutput);
            }

            if (source?.Tag is (Node resizeNode, ResizeHandlePosition handlePos))
            {
                return HandleResizeHandleClick(context, e, source as Rectangle, resizeNode, handlePos, position);
            }

            // Empty canvas click
            return HandleCanvasClick(context, e, position);
        }

        return StateTransitionResult.Unhandled();
    }

    public override StateTransitionResult HandlePointerWheel(InputStateContext context, PointerWheelEventArgs e)
    {
        var position = GetPosition(context, e);
        bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        
        // Pan on scroll behavior
        if (context.Settings.PanOnScroll && !ctrlHeld)
        {
            // Scroll without Ctrl = pan
            var speed = context.Settings.PanOnScrollSpeed * 50;
            var deltaX = e.Delta.X * speed;
            var deltaY = e.Delta.Y * speed;
            context.Viewport.Pan(deltaX, deltaY);
            context.RaiseGridRender();
            e.Handled = true;
            return StateTransitionResult.Stay();
        }
        
        // Default zoom behavior (or Ctrl+scroll when PanOnScroll is enabled)
        if (e.Delta.Y > 0)
            context.Viewport.ZoomIn(position);
        else
            context.Viewport.ZoomOut(position);

        context.RaiseGridRender();
        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        return HandleKeyboardShortcut(context, e);
    }

    #region Click Handlers

    private StateTransitionResult HandleNodeClick(
        InputStateContext context, 
        PointerPressedEventArgs e, 
        Control control, 
        Node node, 
        AvaloniaPoint position)
    {
        var graph = context.Graph;
        if (graph == null) return StateTransitionResult.Unhandled();

        // Check for group collapse button click (always allowed)
        if (node.IsGroup)
        {
            var clickPos = e.GetPosition(control);
            var scale = context.Viewport.Zoom;
            var buttonWidth = 35 * scale;
            var buttonHeight = 28 * scale;

            if (clickPos.X < buttonWidth && clickPos.Y < buttonHeight)
            {
                context.RaiseGroupCollapseToggle(node.Id);
                e.Handled = true;
                return StateTransitionResult.Stay();
            }
        }

        // Double-click handling - check FIRST before any selection/drag logic
        if (e.ClickCount == 2)
        {
            if (node.IsGroup)
            {
                if (context.Settings.EnableGroupLabelEditing)
                {
                    var screenPos = context.CanvasToScreen(new AvaloniaPoint(node.Position.X, node.Position.Y));
                    context.RaiseNodeLabelEditRequested(node, screenPos);
                }
                else
                {
                    context.RaiseGroupCollapseToggle(node.Id);
                }
            }
            else if (context.Settings.EnableNodeLabelEditing)
            {
                var screenPos = context.CanvasToScreen(new AvaloniaPoint(node.Position.X, node.Position.Y));
                context.RaiseNodeLabelEditRequested(node, screenPos);
            }
            e.Handled = true;
            return StateTransitionResult.Stay();
        }

        bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Handle selection only if node is selectable
        if (node.IsSelectable)
        {
            if (!ctrlHeld && !node.IsSelected)
            {
                foreach (var n in graph.Nodes.Where(n => n.Id != node.Id))
                    n.IsSelected = false;
                node.IsSelected = true;
            }
            else if (ctrlHeld)
            {
                node.IsSelected = !node.IsSelected;
            }
        }

        // Start dragging only if node is draggable and selected
        if (node.IsDraggable && node.IsSelected)
        {
            var dragState = new DraggingState(graph, position, context.Viewport, context.Settings);
            CapturePointer(e, control);
            e.Handled = true;
            return StateTransitionResult.TransitionTo(dragState);
        }

        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    private StateTransitionResult HandleEdgeClick(InputStateContext context, PointerPressedEventArgs e, Edge edge)
    {
        var graph = context.Graph;
        if (graph == null) return StateTransitionResult.Unhandled();

        // Check if click is near edge endpoints for reconnection
        var screenPos = GetPosition(context, e);
        var reconnectInfo = CheckEdgeEndpointClick(context, edge, screenPos);
        
        if (reconnectInfo.HasValue && context.Settings.ShowEdgeEndpointHandles)
        {
            var (draggingTarget, fixedNode, fixedPort, movingNode, movingPort) = reconnectInfo.Value;
            
            // Start reconnecting state
            if (context.Theme != null && context.MainCanvas != null)
            {
                var reconnectState = new ReconnectingState(
                    edge, draggingTarget, fixedNode, fixedPort, movingNode, movingPort, screenPos, context.Theme);
                reconnectState.CreateTempLine(context.MainCanvas);
                CapturePointer(e, context.RootPanel);
                e.Handled = true;
                return StateTransitionResult.TransitionTo(reconnectState);
            }
        }

        // Normal edge selection
        bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (!ctrlHeld)
        {
            foreach (var n in graph.Nodes)
                n.IsSelected = false;
            foreach (var ed in graph.Edges.Where(ed => ed.Id != edge.Id))
                ed.IsSelected = false;
        }

        edge.IsSelected = ctrlHeld ? !edge.IsSelected : true;
        context.RaiseEdgeClicked(edge, ctrlHeld);
        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    /// <summary>
    /// Checks if a click is near an edge endpoint and returns reconnection info if so.
    /// </summary>
    private (bool draggingTarget, Node fixedNode, Port fixedPort, Node movingNode, Port movingPort)? 
        CheckEdgeEndpointClick(InputStateContext context, Edge edge, AvaloniaPoint screenPos)
    {
        var graph = context.Graph;
        if (graph == null) return null;

        var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.Target);
        
        if (sourceNode == null || targetNode == null) return null;

        var sourcePort = sourceNode.Outputs.FirstOrDefault(p => p.Id == edge.SourcePort);
        var targetPort = targetNode.Inputs.FirstOrDefault(p => p.Id == edge.TargetPort);
        
        if (sourcePort == null || targetPort == null) return null;

        var sourceScreenPos = context.GraphRenderer.GetPortPosition(sourceNode, sourcePort, true);
        var targetScreenPos = context.GraphRenderer.GetPortPosition(targetNode, targetPort, false);

        var snapDistance = context.Settings.EdgeEndpointHandleSize * 2;

        // Check distance to source endpoint
        var distToSource = Math.Sqrt(
            Math.Pow(screenPos.X - sourceScreenPos.X, 2) + 
            Math.Pow(screenPos.Y - sourceScreenPos.Y, 2));
        
        // Check distance to target endpoint  
        var distToTarget = Math.Sqrt(
            Math.Pow(screenPos.X - targetScreenPos.X, 2) + 
            Math.Pow(screenPos.Y - targetScreenPos.Y, 2));

        if (distToTarget < snapDistance && distToTarget < distToSource)
        {
            // Dragging target end - source stays fixed
            return (true, sourceNode, sourcePort, targetNode, targetPort);
        }
        else if (distToSource < snapDistance)
        {
            // Dragging source end - target stays fixed
            return (false, targetNode, targetPort, sourceNode, sourcePort);
        }

        return null;
    }

    private StateTransitionResult HandlePortClick(
        InputStateContext context,
        PointerPressedEventArgs e,
        Ellipse? portVisual,
        Node node,
        Port port,
        bool isOutput)
    {
        if (portVisual == null || context.MainCanvas == null || context.Theme == null)
            return StateTransitionResult.Unhandled();

        // Check if node allows connections
        if (!node.IsConnectable)
        {
            e.Handled = true;
            return StateTransitionResult.Stay();
        }

        // If strict connection direction is enabled, only allow starting connections from output ports
        if (context.Settings.StrictConnectionDirection && !isOutput)
        {
            // Don't start a new connection from an input port
            e.Handled = true;
            return StateTransitionResult.Stay();
        }

        var position = GetPosition(context, e);
        var connectingState = new ConnectingState(node, port, isOutput, position, portVisual, context.Theme);
        connectingState.CreateTempLine(context.MainCanvas);
        CapturePointer(e, portVisual);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(connectingState);
    }

    private StateTransitionResult HandleResizeHandleClick(
        InputStateContext context,
        PointerPressedEventArgs e,
        Rectangle? handle,
        Node node,
        ResizeHandlePosition handlePos,
        AvaloniaPoint position)
    {
        if (handle == null) return StateTransitionResult.Unhandled();

        var resizeState = new ResizingState(node, handlePos, position, context.Settings, context.Viewport, context.GraphRenderer);
        CapturePointer(e, handle);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(resizeState);
    }

    private StateTransitionResult HandleCanvasClick(InputStateContext context, PointerPressedEventArgs e, AvaloniaPoint position)
    {
        bool shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Determine if we should pan or box select based on settings
        bool shouldPan = context.Settings.PanOnDrag ? !shiftHeld : shiftHeld;

        if (!ctrlHeld)
        {
            context.RaiseDeselectAll();
        }

        if (shouldPan)
        {
            var panState = new PanningState(position, context.Viewport);
            CapturePointer(e, context.RootPanel);
            e.Handled = true;
            return StateTransitionResult.TransitionTo(panState);
        }
        else
        {
            var canvasPoint = context.ScreenToCanvas(position);
            var boxSelectState = new BoxSelectingState(canvasPoint, context.MainCanvas, context.Settings, context.Viewport);
            CapturePointer(e, context.RootPanel);
            e.Handled = true;
            return StateTransitionResult.TransitionTo(boxSelectState);
        }
    }

    #endregion

    #region Keyboard Handling

    private StateTransitionResult HandleKeyboardShortcut(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            context.RaiseDeleteSelected();
            return StateTransitionResult.Stay(true);
        }

        // F2 to rename selected node (like Windows Explorer)
        // F2 always works for renaming, regardless of EnableNodeLabelEditing/EnableGroupLabelEditing settings
        // (those settings only control double-click behavior)
        if (e.Key == Key.F2)
        {
            var graph = context.Graph;
            var selectedNode = graph?.Nodes.FirstOrDefault(n => n.IsSelected);
            if (selectedNode != null)
            {
                var screenPos = context.CanvasToScreen(new AvaloniaPoint(selectedNode.Position.X, selectedNode.Position.Y));
                context.RaiseNodeLabelEditRequested(selectedNode, screenPos);
                return StateTransitionResult.Stay(true);
            }
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.A:
                    context.RaiseSelectAll();
                    return StateTransitionResult.Stay(true);
                case Key.Z when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                    context.RaiseRedo();
                    return StateTransitionResult.Stay(true);
                case Key.Z:
                    context.RaiseUndo();
                    return StateTransitionResult.Stay(true);
                case Key.Y:
                    context.RaiseRedo();
                    return StateTransitionResult.Stay(true);
                case Key.C:
                    context.RaiseCopy();
                    return StateTransitionResult.Stay(true);
                case Key.X:
                    context.RaiseCut();
                    return StateTransitionResult.Stay(true);
                case Key.V:
                    context.RaisePaste();
                    return StateTransitionResult.Stay(true);
                case Key.D:
                    context.RaiseDuplicate();
                    return StateTransitionResult.Stay(true);
                case Key.G when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                    context.RaiseUngroup();
                    return StateTransitionResult.Stay(true);
                case Key.G:
                    context.RaiseGroup();
                    return StateTransitionResult.Stay(true);
            }
        }

        if (e.Key == Key.Escape)
        {
            context.RaiseDeselectAll();
            return StateTransitionResult.Stay(true);
        }

        return StateTransitionResult.Unhandled();
    }

    #endregion
}
