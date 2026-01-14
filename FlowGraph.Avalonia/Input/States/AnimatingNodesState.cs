using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// Input state active during node animations (position transitions, appear/disappear effects).
/// Allows viewport interactions (pan/zoom) while blocking node interactions.
/// </summary>
public class AnimatingNodesState : InputStateBase
{
    private readonly HashSet<string> _animatingNodeIds;
    private readonly Action? _cancelAnimation;
    private readonly Action? _onExit;

    public override string Name => "AnimatingNodes";
    public override bool IsModal => false; // Allow some interactions

    /// <summary>
    /// Creates a new node animating state.
    /// </summary>
    /// <param name="animatingNodeIds">IDs of nodes currently being animated.</param>
    /// <param name="cancelAnimation">Action to cancel the running node animations.</param>
    /// <param name="onExit">Optional callback when exiting this state.</param>
    public AnimatingNodesState(
        IEnumerable<string>? animatingNodeIds = null, 
        Action? cancelAnimation = null, 
        Action? onExit = null)
    {
        _animatingNodeIds = animatingNodeIds != null 
            ? new HashSet<string>(animatingNodeIds) 
            : new HashSet<string>();
        _cancelAnimation = cancelAnimation;
        _onExit = onExit;
    }

    /// <summary>
    /// Gets whether a specific node is currently animating.
    /// </summary>
    public bool IsNodeAnimating(string nodeId) => _animatingNodeIds.Contains(nodeId);

    /// <summary>
    /// Adds a node ID to the animating set.
    /// </summary>
    public void AddAnimatingNode(string nodeId) => _animatingNodeIds.Add(nodeId);

    /// <summary>
    /// Removes a node ID from the animating set.
    /// </summary>
    public void RemoveAnimatingNode(string nodeId) => _animatingNodeIds.Remove(nodeId);

    /// <summary>
    /// Gets whether there are any nodes still animating.
    /// </summary>
    public bool HasAnimatingNodes => _animatingNodeIds.Count > 0;

    public override void Exit(InputStateContext context)
    {
        _onExit?.Invoke();
    }

    public override StateTransitionResult HandlePointerPressed(InputStateContext context, PointerPressedEventArgs e, Control? source)
    {
        var point = e.GetCurrentPoint(context.RootPanel);
        var position = e.GetPosition(context.RootPanel);

        // Middle mouse button always starts panning (allowed during node animation)
        if (point.Properties.IsMiddleButtonPressed)
        {
            var panState = new PanningState(position, context.Viewport);
            CapturePointer(e, context.RootPanel);
            return StateTransitionResult.TransitionTo(panState);
        }

        // Check if clicking on an animating node - block interaction
        var node = Rendering.NodeRenderers.ResizableVisual.GetNodeFromTag(source?.Tag);
        if (point.Properties.IsLeftButtonPressed && node != null)
        {
            if (_animatingNodeIds.Contains(node.Id))
            {
                // Block interaction with animating node
                e.Handled = true;
                return StateTransitionResult.Stay();
            }
            
            // Allow interaction with non-animating nodes - transition to idle to handle
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        // Check if clicking on an edge connected to an animating node
        if (point.Properties.IsLeftButtonPressed && source?.Tag is Edge edge)
        {
            if (_animatingNodeIds.Contains(edge.Source) || _animatingNodeIds.Contains(edge.Target))
            {
                // Block interaction with edges connected to animating nodes
                e.Handled = true;
                return StateTransitionResult.Stay();
            }
        }

        // Empty canvas click - allow pan/box select
        if (point.Properties.IsLeftButtonPressed)
        {
            bool shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            bool shouldPan = context.Settings.PanOnDrag ? !shiftHeld : shiftHeld;

            if (shouldPan)
            {
                var panState = new PanningState(position, context.Viewport);
                CapturePointer(e, context.RootPanel);
                e.Handled = true;
                return StateTransitionResult.TransitionTo(panState);
            }
        }

        return StateTransitionResult.Unhandled();
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Allow pointer movement
        return StateTransitionResult.Unhandled();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        return StateTransitionResult.Unhandled();
    }

    public override StateTransitionResult HandlePointerWheel(InputStateContext context, PointerWheelEventArgs e)
    {
        // Allow zoom during node animations
        var position = e.GetPosition(context.RootPanel);
        
        if (e.Delta.Y > 0)
            context.Viewport.ZoomIn(position);
        else
            context.Viewport.ZoomOut(position);

        context.ApplyViewportTransform();
        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        // Cancel animations on Escape
        if (e.Key == Key.Escape)
        {
            _cancelAnimation?.Invoke();
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        // Allow viewport shortcuts (zoom)
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.Add:
                case Key.OemPlus:
                    context.Viewport.ZoomIn();
                    context.ApplyViewportTransform();
                    return StateTransitionResult.Stay(true);
                case Key.Subtract:
                case Key.OemMinus:
                    context.Viewport.ZoomOut();
                    context.ApplyViewportTransform();
                    return StateTransitionResult.Stay(true);
                case Key.D0:
                case Key.NumPad0:
                    context.Viewport.ResetZoom();
                    context.ApplyViewportTransform();
                    return StateTransitionResult.Stay(true);
            }
        }

        // Block other shortcuts that affect nodes
        return StateTransitionResult.Stay(handled: true);
    }
}
