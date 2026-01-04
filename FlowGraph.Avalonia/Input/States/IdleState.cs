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

        // Check for group collapse button click
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

            // Double-click to toggle collapse
            if (e.ClickCount == 2)
            {
                context.RaiseGroupCollapseToggle(node.Id);
                e.Handled = true;
                return StateTransitionResult.Stay();
            }
        }

        bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Handle selection
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

        // Start dragging
        var dragState = new DraggingState(graph, position, context.Viewport, context.Settings);
        CapturePointer(e, control);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(dragState);
    }

    private StateTransitionResult HandleEdgeClick(InputStateContext context, PointerPressedEventArgs e, Edge edge)
    {
        var graph = context.Graph;
        if (graph == null) return StateTransitionResult.Unhandled();

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
