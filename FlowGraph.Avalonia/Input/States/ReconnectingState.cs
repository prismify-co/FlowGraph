using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using FlowGraph.Core.Commands;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for reconnecting an existing edge by dragging its endpoint.
/// Similar to React Flow's reconnectable edges - the edge visual updates in real-time.
/// </summary>
public class ReconnectingState : InputStateBase
{
    private readonly Edge _edge;
    private readonly bool _draggingTarget; // true = dragging target end, false = dragging source end
    private readonly Node _fixedNode;
    private readonly Port _fixedPort;
    private readonly bool _fixedPortIsOutput; // true if the fixed port is an output port
    private readonly Node _originalMovingNode;
    private readonly Port _originalMovingPort;
    private AvaloniaPoint _currentEndPoint;
    private readonly ThemeResources _theme;

    // Track hovered/snapped port for validation visual feedback
    private Ellipse? _hoveredPortVisual;
    private IBrush? _hoveredPortOriginalFill;
    
    // Track the currently snapped target
    private (Node node, Port port, bool isOutput)? _snappedTarget;

    public override string Name => "Reconnecting";
    public override bool IsModal => true;

    /// <summary>
    /// Creates a new reconnecting state.
    /// </summary>
    /// <param name="edge">The edge being reconnected.</param>
    /// <param name="draggingTarget">True if dragging the target (input) end, false if dragging the source (output) end.</param>
    /// <param name="fixedNode">The node whose connection stays fixed.</param>
    /// <param name="fixedPort">The port that stays connected.</param>
    /// <param name="movingNode">The node we're disconnecting from.</param>
    /// <param name="movingPort">The port we're disconnecting from.</param>
    /// <param name="startPosition">Initial screen position of the cursor.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public ReconnectingState(
        Edge edge,
        bool draggingTarget,
        Node fixedNode,
        Port fixedPort,
        Node movingNode,
        Port movingPort,
        AvaloniaPoint startPosition,
        ThemeResources theme)
    {
        _edge = edge;
        _draggingTarget = draggingTarget;
        _fixedNode = fixedNode;
        _fixedPort = fixedPort;
        // If we're dragging the target end, the fixed end is the source (output)
        // If we're dragging the source end, the fixed end is the target (input)
        _fixedPortIsOutput = draggingTarget; // source is output, target is input
        _originalMovingNode = movingNode;
        _originalMovingPort = movingPort;
        _currentEndPoint = startPosition;
        _theme = theme;
    }

    public void CreateTempLine(Canvas canvas)
    {
        // We no longer create a separate temp line - we update the existing edge visual
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);
        
        // Make the edge appear "detached" by styling it as a connection-in-progress
        UpdateEdgeVisualAsDetached(context);
    }

    public override void Exit(InputStateContext context)
    {
        // Restore hovered port color
        RestoreHoveredPortColor();
        
        // Reset the edge visual styling (it will be re-rendered if reconnection happened)
        ResetEdgeVisual(context);
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        _currentEndPoint = GetPosition(context, e);
        
        // Try to find a snap target
        _snappedTarget = FindSnapTarget(context, _currentEndPoint);
        
        // Update the edge visual to follow the cursor (or snap to target)
        UpdateEdgeVisual(context);
        UpdatePortValidationVisual(context, _currentEndPoint);
        
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        var graph = context.Graph;
        if (graph == null)
        {
            ReleasePointer(e);
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        var screenPoint = GetPosition(context, e);
        
        // Find target port (direct hit or snapped)
        Node? targetNode = null;
        Port? targetPort = null;
        
        var hitElement = HitTest(context, screenPoint);
        if (hitElement is Ellipse targetPortVisual && 
            targetPortVisual.Tag is (Node tn, Port tp, bool isOutput))
        {
            // Verify it's the correct port type
            if (_draggingTarget && !isOutput) // Need input port
            {
                targetNode = tn;
                targetPort = tp;
            }
            else if (!_draggingTarget && isOutput) // Need output port
            {
                targetNode = tn;
                targetPort = tp;
            }
        }
        else if (_snappedTarget.HasValue)
        {
            targetNode = _snappedTarget.Value.node;
            targetPort = _snappedTarget.Value.port;
        }

        // Determine what to do
        if (targetNode != null && targetPort != null && targetNode.IsConnectable)
        {
            // Reconnect to new port
            if (IsConnectionValid(context, targetNode, targetPort))
            {
                ReconnectEdge(context, targetNode, targetPort);
            }
        }
        else
        {
            // Dropped in empty space - delete the edge
            DeleteEdge(context);
        }

        ReleasePointer(e);
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel - edge stays as is, visual will be reset in Exit()
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    /// <summary>
    /// Updates the edge visual to show it as "detached" and following the cursor.
    /// </summary>
    private void UpdateEdgeVisualAsDetached(InputStateContext context)
    {
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath != null)
        {
            // Style as a connection in progress (dashed, slightly transparent)
            visiblePath.StrokeDashArray = [5, 3];
            visiblePath.Opacity = 0.8;
        }
    }

    /// <summary>
    /// Updates the edge visual to follow the cursor or snap to a target port.
    /// </summary>
    private void UpdateEdgeVisual(InputStateContext context)
    {
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath == null) return;

        // Get the fixed endpoint - use the correct isOutput value
        var fixedPoint = context.GraphRenderer.GetPortPosition(_fixedNode, _fixedPort, _fixedPortIsOutput);
        
        // Determine the moving endpoint
        AvaloniaPoint movingPoint;
        if (_snappedTarget.HasValue)
        {
            movingPoint = context.GraphRenderer.GetPortPosition(
                _snappedTarget.Value.node,
                _snappedTarget.Value.port,
                _snappedTarget.Value.isOutput);
        }
        else
        {
            movingPoint = _currentEndPoint;
        }

        // Create the path geometry
        // When dragging target: fixed is source (output), moving is target (input)
        // When dragging source: moving is source (output), fixed is target (input)
        PathGeometry pathGeometry;
        if (_draggingTarget)
        {
            // Fixed point is source, moving point is target
            pathGeometry = BezierHelper.CreateBezierPath(fixedPoint, movingPoint, false);
        }
        else
        {
            // Moving point is source, fixed point is target
            pathGeometry = BezierHelper.CreateBezierPath(movingPoint, fixedPoint, false);
        }
        
        visiblePath.Data = pathGeometry;
        
        // Also update the hit area path
        var hitAreaPath = context.GraphRenderer.GetEdgeVisual(_edge.Id);
        if (hitAreaPath != null)
        {
            hitAreaPath.Data = pathGeometry;
        }
        
        // Hide markers while dragging (they would be at wrong positions)
        var markers = context.GraphRenderer.GetEdgeMarkers(_edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.IsVisible = false;
            }
        }
    }

    /// <summary>
    /// Resets the edge visual to its normal state.
    /// </summary>
    private void ResetEdgeVisual(InputStateContext context)
    {
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath != null)
        {
            // Reset styling
            visiblePath.StrokeDashArray = null;
            visiblePath.Opacity = 1.0;
        }
        
        // Show markers again
        var markers = context.GraphRenderer.GetEdgeMarkers(_edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.IsVisible = true;
            }
        }
    }

    private (Node node, Port port, bool isOutput)? FindSnapTarget(InputStateContext context, AvaloniaPoint screenPoint)
    {
        var graph = context.Graph;
        var settings = context.Settings;
        
        if (graph == null || !settings.SnapConnectionToNode || settings.ConnectionSnapDistance <= 0)
            return null;

        var snapDistance = settings.ConnectionSnapDistance;
        
        (Node node, Port port, bool isOutput)? bestTarget = null;
        double bestDistance = double.MaxValue;

        foreach (var node in graph.Nodes)
        {
            // Skip non-connectable nodes and groups
            if (!node.IsConnectable || node.IsGroup)
                continue;
            
            // Skip the fixed node (can't connect to same node)
            if (node.Id == _fixedNode.Id)
                continue;

            // Look at the correct port type based on which end we're dragging
            var portsToCheck = _draggingTarget ? node.Inputs : node.Outputs;
            var isOutput = !_draggingTarget;

            foreach (var port in portsToCheck)
            {
                var portScreenPos = context.GraphRenderer.GetPortPosition(node, port, isOutput);
                
                var dx = portScreenPos.X - screenPoint.X;
                var dy = portScreenPos.Y - screenPoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < snapDistance && distance < bestDistance)
                {
                    if (IsConnectionValid(context, node, port))
                    {
                        bestDistance = distance;
                        bestTarget = (node, port, isOutput);
                    }
                }
            }
        }

        return bestTarget;
    }

    private void UpdatePortValidationVisual(InputStateContext context, AvaloniaPoint screenPoint)
    {
        var hitElement = HitTest(context, screenPoint);
        Ellipse? targetPortVisual = null;
        Node? targetNode = null;
        Port? targetPort = null;
        bool isCorrectType = false;

        if (hitElement is Ellipse portVisual && 
            portVisual.Tag is (Node tn, Port tp, bool isOutput))
        {
            // Check if it's the correct port type
            isCorrectType = (_draggingTarget && !isOutput) || (!_draggingTarget && isOutput);
            if (isCorrectType)
            {
                targetPortVisual = portVisual;
                targetNode = tn;
                targetPort = tp;
            }
        }
        else if (_snappedTarget.HasValue)
        {
            targetNode = _snappedTarget.Value.node;
            targetPort = _snappedTarget.Value.port;
            targetPortVisual = context.GraphRenderer.GetPortVisual(targetNode.Id, targetPort.Id);
            isCorrectType = true;
        }

        if (targetPortVisual != null && targetNode != null && targetPort != null && isCorrectType)
        {
            if (_hoveredPortVisual != targetPortVisual)
            {
                RestoreHoveredPortColor();
                
                _hoveredPortVisual = targetPortVisual;
                _hoveredPortOriginalFill = targetPortVisual.Fill;

                bool isValid = targetNode.IsConnectable && IsConnectionValid(context, targetNode, targetPort);
                targetPortVisual.Fill = isValid ? _theme.PortValidConnection : _theme.PortInvalidConnection;
            }
        }
        else
        {
            RestoreHoveredPortColor();
        }
    }

    private void RestoreHoveredPortColor()
    {
        if (_hoveredPortVisual != null && _hoveredPortOriginalFill != null)
        {
            _hoveredPortVisual.Fill = _hoveredPortOriginalFill;
        }
        _hoveredPortVisual = null;
        _hoveredPortOriginalFill = null;
    }

    private bool IsConnectionValid(InputStateContext context, Node targetNode, Port targetPort)
    {
        var graph = context.Graph;
        if (graph == null) return true;

        var validator = context.ConnectionValidator;
        if (validator == null) return true;

        // Build connection context based on which end we're moving
        Node sourceNode, destNode;
        Port sourcePort, destPort;
        
        if (_draggingTarget)
        {
            sourceNode = _fixedNode;
            sourcePort = _fixedPort;
            destNode = targetNode;
            destPort = targetPort;
        }
        else
        {
            sourceNode = targetNode;
            sourcePort = targetPort;
            destNode = _fixedNode;
            destPort = _fixedPort;
        }

        var connectionContext = new ConnectionContext
        {
            SourceNode = sourceNode,
            SourcePort = sourcePort,
            TargetNode = destNode,
            TargetPort = destPort,
            Graph = graph
        };

        return validator.Validate(connectionContext).IsValid;
    }

    private void ReconnectEdge(InputStateContext context, Node newNode, Port newPort)
    {
        var graph = context.Graph;
        if (graph == null) return;

        // Create the new edge
        Edge newEdge;
        if (_draggingTarget)
        {
            newEdge = new Edge
            {
                Source = _fixedNode.Id,
                SourcePort = _fixedPort.Id,
                Target = newNode.Id,
                TargetPort = newPort.Id,
                Type = _edge.Type,
                Label = _edge.Label,
                MarkerStart = _edge.MarkerStart,
                MarkerEnd = _edge.MarkerEnd
            };
        }
        else
        {
            newEdge = new Edge
            {
                Source = newNode.Id,
                SourcePort = newPort.Id,
                Target = _fixedNode.Id,
                TargetPort = _fixedPort.Id,
                Type = _edge.Type,
                Label = _edge.Label,
                MarkerStart = _edge.MarkerStart,
                MarkerEnd = _edge.MarkerEnd
            };
        }

        context.RaiseEdgeReconnected(_edge, newEdge);
    }

    private void DeleteEdge(InputStateContext context)
    {
        var graph = context.Graph;
        if (graph == null) return;

        context.RaiseEdgeDisconnected(_edge);
    }
}
