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
/// </summary>
public class ReconnectingState : InputStateBase
{
    private readonly Edge _edge;
    private readonly bool _draggingTarget; // true = dragging target end, false = dragging source end
    private readonly Node _fixedNode;
    private readonly Port _fixedPort;
    private readonly Node _originalMovingNode;
    private readonly Port _originalMovingPort;
    private AvaloniaPoint _endPoint;
    private AvaloniaPath? _tempLine;
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
    /// <param name="draggingTarget">True if dragging the target (input) end, false for source (output) end.</param>
    /// <param name="fixedNode">The node that stays connected.</param>
    /// <param name="fixedPort">The port that stays connected.</param>
    /// <param name="movingNode">The original node being disconnected.</param>
    /// <param name="movingPort">The original port being disconnected.</param>
    /// <param name="startPosition">Initial screen position.</param>
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
        _originalMovingNode = movingNode;
        _originalMovingPort = movingPort;
        _endPoint = startPosition;
        _theme = theme;
    }

    public void CreateTempLine(Canvas canvas)
    {
        _tempLine = new AvaloniaPath
        {
            Stroke = _theme.EdgeStroke,
            StrokeThickness = 2,
            StrokeDashArray = [5, 3],
            Opacity = 0.7
        };
        canvas.Children.Add(_tempLine);
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);
        
        // Hide the original edge while reconnecting
        HideOriginalEdge(context);
    }

    public override void Exit(InputStateContext context)
    {
        // Restore hovered port color
        RestoreHoveredPortColor();

        // Remove temp line
        if (_tempLine != null && context.MainCanvas != null)
        {
            context.MainCanvas.Children.Remove(_tempLine);
            _tempLine = null;
        }
        
        // Show the original edge again (it will be re-rendered if reconnection happened)
        ShowOriginalEdge(context);
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        _endPoint = GetPosition(context, e);
        
        // Try to find a snap target
        _snappedTarget = FindSnapTarget(context, _endPoint);
        
        UpdateTempLine(context);
        UpdatePortValidationVisual(context, _endPoint);
        
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
            // Cancel - do nothing, edge stays as is
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    private void UpdateTempLine(InputStateContext context)
    {
        if (_tempLine == null) return;

        var fixedPoint = context.GraphRenderer.GetPortPosition(_fixedNode, _fixedPort, !_draggingTarget);
        
        // If we have a snapped target, draw to that port
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
            movingPoint = _endPoint;
        }

        // Create bezier path - direction depends on which end we're dragging
        var pathGeometry = _draggingTarget
            ? BezierHelper.CreateBezierPath(fixedPoint, movingPoint, false)  // Source to target
            : BezierHelper.CreateBezierPath(movingPoint, fixedPoint, true);  // Source to target (reversed)
        
        _tempLine.Data = pathGeometry;
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

        // Create a composite command: remove old edge, add new edge
        var oldEdge = _edge;
        
        Edge newEdge;
        if (_draggingTarget)
        {
            newEdge = new Edge
            {
                Source = _fixedNode.Id,
                SourcePort = _fixedPort.Id,
                Target = newNode.Id,
                TargetPort = newPort.Id,
                Type = oldEdge.Type,
                Label = oldEdge.Label,
                MarkerStart = oldEdge.MarkerStart,
                MarkerEnd = oldEdge.MarkerEnd
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
                Type = oldEdge.Type,
                Label = oldEdge.Label,
                MarkerStart = oldEdge.MarkerStart,
                MarkerEnd = oldEdge.MarkerEnd
            };
        }

        // Execute as composite for undo support
        var commands = new List<IGraphCommand>
        {
            new RemoveEdgeCommand(graph, oldEdge),
            new AddEdgeCommand(graph, newEdge)
        };
        
        // We need access to command history - raise an event instead
        context.RaiseEdgeReconnected(oldEdge, newEdge);
    }

    private void DeleteEdge(InputStateContext context)
    {
        var graph = context.Graph;
        if (graph == null) return;

        context.RaiseEdgeDisconnected(_edge);
    }

    /// <summary>
    /// Hides the original edge visuals while reconnecting.
    /// </summary>
    private void HideOriginalEdge(InputStateContext context)
    {
        // Hide the visible path
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath != null)
        {
            visiblePath.IsVisible = false;
        }

        // Hide the hit area path
        var hitArea = context.GraphRenderer.GetEdgeVisual(_edge.Id);
        if (hitArea != null)
        {
            hitArea.IsVisible = false;
        }

        // Hide markers
        var markers = context.GraphRenderer.GetEdgeMarkers(_edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.IsVisible = false;
            }
        }

        // Hide label
        var label = context.GraphRenderer.GetEdgeLabel(_edge.Id);
        if (label != null)
        {
            label.IsVisible = false;
        }
    }

    /// <summary>
    /// Shows the original edge visuals (called on cancel or after state exits).
    /// </summary>
    private void ShowOriginalEdge(InputStateContext context)
    {
        // Show the visible path
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath != null)
        {
            visiblePath.IsVisible = true;
        }

        // Show the hit area path
        var hitArea = context.GraphRenderer.GetEdgeVisual(_edge.Id);
        if (hitArea != null)
        {
            hitArea.IsVisible = true;
        }

        // Show markers
        var markers = context.GraphRenderer.GetEdgeMarkers(_edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.IsVisible = true;
            }
        }

        // Show label
        var label = context.GraphRenderer.GetEdgeLabel(_edge.Id);
        if (label != null)
        {
            label.IsVisible = true;
        }
    }
}
