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
    private AvaloniaPoint _currentEndPointViewport;
    private readonly ThemeResources _theme;

    // Temp line overlay for showing the reconnecting edge
    private AvaloniaPath? _tempLine;
    private Panel? _tempLineContainer;
    private bool _isDirectRenderingMode;

    // Track hovered/snapped port for validation visual feedback
    private Control? _hoveredPortVisual;
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
        // Legacy method - kept for backwards compatibility
        CreateTempLineInternal(canvas, isDirectMode: false);
    }

    /// <summary>
    /// Creates the temporary reconnection line, using RootPanel for direct rendering mode
    /// or MainCanvas for visual tree mode.
    /// </summary>
    public void CreateTempLine(InputStateContext context)
    {
        _isDirectRenderingMode = context.DirectRenderer != null;

        if (_isDirectRenderingMode && context.RootPanel != null)
        {
            CreateTempLineInternal(context.RootPanel, isDirectMode: true);
        }
        else if (context.MainCanvas != null)
        {
            CreateTempLineInternal(context.MainCanvas, isDirectMode: false);
        }
    }

    private void CreateTempLineInternal(Panel container, bool isDirectMode)
    {
        _tempLine = new AvaloniaPath
        {
            Stroke = _theme.EdgeStroke,
            StrokeThickness = 2,
            StrokeDashArray = [5, 3],
            Opacity = 0.8,
            IsHitTestVisible = false
        };
        container.Children.Add(_tempLine);
        _tempLineContainer = container;
        _isDirectRenderingMode = isDirectMode;
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);

        // Create the temp line overlay for showing the reconnecting edge
        CreateTempLine(context);

        // Hide the original edge visual while reconnecting (if it exists in visual tree mode)
        HideOriginalEdgeVisual(context);

        // Initial update of the temp line
        UpdateTempLine(context);
    }

    public override void Exit(InputStateContext context)
    {
        // Restore hovered port color
        RestoreHoveredPortColor();

        // Remove temp line
        if (_tempLine != null && _tempLineContainer != null)
        {
            _tempLineContainer.Children.Remove(_tempLine);
            _tempLine = null;
            _tempLineContainer = null;
        }

        // Restore the original edge visual (will be re-rendered if reconnection happened)
        ShowOriginalEdgeVisual(context);
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Get positions using typed coordinate system
        var viewportPos = GetTypedViewportPosition(context, e);
        var canvasPos = GetTypedCanvasPosition(context, e);
        
        _currentEndPointViewport = ToAvaloniaPoint(viewportPos);
        _currentEndPoint = ToAvaloniaPoint(canvasPos);

        // AutoPan: pan viewport when dragging near edges (uses viewport coordinates)
        if (context.Settings.EnableAutoPan && context.RootPanel != null)
        {
            var viewBounds = context.RootPanel.Bounds;
            var edgeDist = context.Settings.AutoPanEdgeDistance;
            var panSpeed = context.Settings.AutoPanSpeed;

            double panX = 0, panY = 0;
            if (viewportPos.X < edgeDist) panX = panSpeed;
            else if (viewportPos.X > viewBounds.Width - edgeDist) panX = -panSpeed;
            if (viewportPos.Y < edgeDist) panY = panSpeed;
            else if (viewportPos.Y > viewBounds.Height - edgeDist) panY = -panSpeed;

            if (panX != 0 || panY != 0)
            {
                context.Viewport.Pan(panX, panY);
                context.ApplyViewportTransform();
            }
        }

        // Try to find a snap target (uses canvas coordinates for distance calculation)
        _snappedTarget = FindSnapTarget(context, _currentEndPoint);

        // Update the temp line overlay to follow the cursor (or snap to target)
        UpdateTempLine(context);

        // Update port validation visual (uses canvas coordinates for hit testing)
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

        // Find target port (direct hit or snapped)
        Node? targetNode = null;
        Port? targetPort = null;

        // Use canvas coordinates for hit testing (typed system handles rendering mode)
        var canvasPos = GetTypedCanvasPosition(context, e);
        var canvasPoint = ToAvaloniaPoint(canvasPos);
        var viewportPoint = GetTypedViewportPosition(context, e);

        Console.WriteLine($"[ReconnectingState.Released] viewportPoint=({viewportPoint.X}, {viewportPoint.Y}), canvasPoint={canvasPoint}");
        Console.WriteLine($"[ReconnectingState.Released] Viewport: Zoom={context.Viewport.Zoom}, Offset=({context.Viewport.OffsetX}, {context.Viewport.OffsetY})");
        Console.WriteLine($"[ReconnectingState.Released] _draggingTarget={_draggingTarget} (true=need input port, false=need output port)");

        // Use HitTestForPort which skips edge paths and markers that might block the port
        var hitElement = HitTestForPort(context, canvasPoint);
        Console.WriteLine($"[ReconnectingState.Released] HitTestForPort result: {hitElement?.GetType().Name ?? "null"}, Tag={hitElement?.Tag}");

        if (hitElement is Control targetPortVisual &&
            targetPortVisual.Tag is (Node tn, Port tp, bool isOutput))
        {
            Console.WriteLine($"[ReconnectingState.Released] Hit port: Node={tn.Id}, Port={tp.Id}, isOutput={isOutput}");
            // Verify it's the correct port type
            if (_draggingTarget && !isOutput) // Need input port
            {
                Console.WriteLine($"[ReconnectingState.Released] Accepted: Need input, got input");
                targetNode = tn;
                targetPort = tp;
            }
            else if (!_draggingTarget && isOutput) // Need output port
            {
                Console.WriteLine($"[ReconnectingState.Released] Accepted: Need output, got output");
                targetNode = tn;
                targetPort = tp;
            }
            else
            {
                Console.WriteLine($"[ReconnectingState.Released] Rejected: Port type mismatch");
            }
        }
        else if (_snappedTarget.HasValue)
        {
            Console.WriteLine($"[ReconnectingState.Released] Using snap target: Node={_snappedTarget.Value.node.Id}, Port={_snappedTarget.Value.port.Id}");
            targetNode = _snappedTarget.Value.node;
            targetPort = _snappedTarget.Value.port;
        }
        else
        {
            Console.WriteLine($"[ReconnectingState.Released] No hit and no snap target");
        }

        Console.WriteLine($"[ReconnectingState.Released] Final target: Node={targetNode?.Id ?? "null"}, Port={targetPort?.Id ?? "null"}");

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
    /// Hides the original edge visual while reconnecting (visual tree mode only).
    /// </summary>
    private void HideOriginalEdgeVisual(InputStateContext context)
    {
        // In visual tree mode, hide the original edge so our temp line is visible
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath != null)
        {
            visiblePath.IsVisible = false;
        }

        var hitAreaPath = context.GraphRenderer.GetEdgeVisual(_edge.Id);
        if (hitAreaPath != null)
        {
            hitAreaPath.IsVisible = false;
        }

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
    /// Shows the original edge visual after reconnecting (visual tree mode only).
    /// </summary>
    private void ShowOriginalEdgeVisual(InputStateContext context)
    {
        // Restore visibility (the edge will be re-rendered/removed if reconnection happened)
        var visiblePath = context.GraphRenderer.GetEdgeVisiblePath(_edge.Id);
        if (visiblePath != null)
        {
            visiblePath.IsVisible = true;
        }

        var hitAreaPath = context.GraphRenderer.GetEdgeVisual(_edge.Id);
        if (hitAreaPath != null)
        {
            hitAreaPath.IsVisible = true;
        }

        var markers = context.GraphRenderer.GetEdgeMarkers(_edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.IsVisible = true;
            }
        }
    }

    /// <summary>
    /// Updates the temp line overlay to follow the cursor or snap to a target port.
    /// </summary>
    private void UpdateTempLine(InputStateContext context)
    {
        if (_tempLine == null) return;

        // Get canvas coordinates for the fixed port position
        var fixedPointCanvas = context.GraphRenderer.GetPortCanvasPosition(_fixedNode, _fixedPort, _fixedPortIsOutput);

        // Determine the moving endpoint in canvas coordinates
        AvaloniaPoint movingPointCanvas;
        if (_snappedTarget.HasValue)
        {
            movingPointCanvas = context.GraphRenderer.GetPortCanvasPosition(
                _snappedTarget.Value.node,
                _snappedTarget.Value.port,
                _snappedTarget.Value.isOutput);
        }
        else
        {
            movingPointCanvas = _currentEndPoint;
        }

        // Convert to the appropriate coordinate space based on rendering mode
        AvaloniaPoint startPoint, endPoint;
        if (_isDirectRenderingMode)
        {
            // Direct rendering mode: temp line is in RootPanel (untransformed)
            // Convert canvas coords to viewport coords
            var fixedPointViewport = context.CanvasToViewport(fixedPointCanvas);

            AvaloniaPoint movingPointViewport;
            if (_snappedTarget.HasValue)
            {
                movingPointViewport = context.CanvasToViewport(movingPointCanvas);
            }
            else
            {
                // Use viewport position directly for cursor
                movingPointViewport = _currentEndPointViewport;
            }

            // Determine start/end based on which end we're dragging
            if (_draggingTarget)
            {
                startPoint = fixedPointViewport;
                endPoint = movingPointViewport;
            }
            else
            {
                startPoint = movingPointViewport;
                endPoint = fixedPointViewport;
            }
        }
        else
        {
            // Visual tree mode: temp line is in MainCanvas (transformed)
            // Use canvas coordinates directly
            if (_draggingTarget)
            {
                startPoint = fixedPointCanvas;
                endPoint = movingPointCanvas;
            }
            else
            {
                startPoint = movingPointCanvas;
                endPoint = fixedPointCanvas;
            }
        }

        // Create the bezier path geometry
        // When dragging target: fixed is source (output), moving is target (input)
        // When dragging source: moving is source (output), fixed is target (input)
        var pathGeometry = BezierHelper.CreateBezierPath(startPoint, endPoint, !_draggingTarget);
        _tempLine.Data = pathGeometry;
    }

    private (Node node, Port port, bool isOutput)? FindSnapTarget(InputStateContext context, AvaloniaPoint canvasPoint)
    {
        var graph = context.Graph;
        var settings = context.Settings;

        if (graph == null || !settings.SnapConnectionToNode || settings.ConnectionSnapDistance <= 0)
            return null;

        var snapDistance = settings.ConnectionSnapDistance;
        var zoom = context.Viewport.Zoom;

        // Convert snap distance to canvas coordinates (screen pixels / zoom = canvas units)
        var canvasSnapDistance = snapDistance / zoom;

        // For early rejection, add node width to allow for ports on node edges
        var earlyRejectDistance = canvasSnapDistance + settings.NodeWidth;

        (Node node, Port port, bool isOutput)? bestTarget = null;
        double bestDistance = double.MaxValue;

        foreach (var node in graph.Elements.Nodes)
        {
            // Skip non-connectable nodes and groups
            if (!node.IsConnectable || node.IsGroup)
                continue;

            // Skip the fixed node (can't connect to same node)
            if (node.Id == _fixedNode.Id)
                continue;

            // OPTIMIZATION: Early rejection based on canvas distance
            var nodeWidth = node.Width ?? settings.NodeWidth;
            var nodeHeight = node.Height ?? settings.NodeHeight;
            var nodeCenterX = node.Position.X + nodeWidth / 2;
            var nodeCenterY = node.Position.Y + nodeHeight / 2;
            var canvasDx = nodeCenterX - canvasPoint.X;
            var canvasDy = nodeCenterY - canvasPoint.Y;
            var canvasDistSq = canvasDx * canvasDx + canvasDy * canvasDy;

            if (canvasDistSq > earlyRejectDistance * earlyRejectDistance)
                continue;

            // Look at the correct port type based on which end we're dragging
            var portsToCheck = _draggingTarget ? node.Inputs : node.Outputs;
            var isOutput = !_draggingTarget;

            foreach (var port in portsToCheck)
            {
                // Get the port's canvas position for distance calculation
                var portCanvasPos = context.GraphRenderer.GetPortCanvasPosition(node, port, isOutput);

                // Calculate distance in canvas coordinates
                var dx = portCanvasPos.X - canvasPoint.X;
                var dy = portCanvasPos.Y - canvasPoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < canvasSnapDistance && distance < bestDistance)
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

    /// <summary>
    /// Updates the visual appearance of ports during reconnection dragging.
    /// </summary>
    /// <param name="context">The input state context.</param>
    /// <param name="canvasPoint">The cursor position in canvas coordinates.</param>
    private void UpdatePortValidationVisual(InputStateContext context, AvaloniaPoint canvasPoint)
    {
        // Use HitTestForPort which skips edge paths and markers
        var hitElement = HitTestForPort(context, canvasPoint);
        Control? targetPortVisual = null;
        Node? targetNode = null;
        Port? targetPort = null;
        bool isCorrectType = false;

        if (hitElement is Control portVisual &&
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
                _hoveredPortOriginalFill = GetPortFill(targetPortVisual);

                bool isValid = targetNode.IsConnectable && IsConnectionValid(context, targetNode, targetPort);
                SetPortFill(targetPortVisual, isValid ? _theme.PortValidConnection : _theme.PortInvalidConnection);
            }
        }
        else
        {
            RestoreHoveredPortColor();
        }
    }

    /// <summary>
    /// Gets the fill brush from a port visual (works with Shape-based controls).
    /// </summary>
    private static IBrush? GetPortFill(Control visual)
    {
        return visual is Shape shape ? shape.Fill : null;
    }

    /// <summary>
    /// Sets the fill brush on a port visual (works with Shape-based controls).
    /// </summary>
    private static void SetPortFill(Control visual, IBrush? fill)
    {
        if (visual is Shape shape)
        {
            shape.Fill = fill;
        }
    }

    private void RestoreHoveredPortColor()
    {
        if (_hoveredPortVisual != null)
        {
            SetPortFill(_hoveredPortVisual, _hoveredPortOriginalFill);
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
