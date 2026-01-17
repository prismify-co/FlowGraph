using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for creating a new connection by dragging from a port.
/// </summary>
public class ConnectingState : InputStateBase
{
    private readonly Node _sourceNode;
    private readonly Port _sourcePort;
    private readonly bool _fromOutput;
    private AvaloniaPoint _endPoint;
    private AvaloniaPath? _tempLine;
    private readonly Control? _portVisual;
    private readonly Cursor? _previousCursor;
    private readonly ThemeResources _theme;

    // Track hovered/snapped port for validation visual feedback
    private Control? _hoveredPortVisual;
    private IBrush? _hoveredPortOriginalFill;

    // Track the currently snapped target for the temp line
    private (Node node, Port port, bool isOutput)? _snappedTarget;

    // Track whether connect start was raised
    private bool _connectStartRaised;

    // Track which container holds the temp line (for cleanup)
    private Panel? _tempLineContainer;

    // Store viewport position for direct rendering mode temp line
    private AvaloniaPoint _endPointViewport;

    // DEBUG: counter for throttled logging
    private int _debugMoveCount;

    public override string Name => "Connecting";
    public override bool IsModal => true;

    public ConnectingState(
        Node sourceNode,
        Port sourcePort,
        bool fromOutput,
        AvaloniaPoint startPosition,
        Control? portVisual,
        ThemeResources theme)
    {
        _sourceNode = sourceNode;
        _sourcePort = sourcePort;
        _fromOutput = fromOutput;
        _endPoint = startPosition;
        _portVisual = portVisual;
        _previousCursor = portVisual?.Cursor;
        _theme = theme;
        _connectStartRaised = false;

        // Change cursor during connection (only if we have a real visual)
        if (portVisual != null)
        {
            portVisual.Cursor = new Cursor(StandardCursorType.Hand);
        }
    }

    public void CreateTempLine(Canvas canvas)
    {
        _tempLine = new AvaloniaPath
        {
            Stroke = _theme.EdgeStroke,
            StrokeThickness = 2,
            StrokeDashArray = [5, 3],
            Opacity = 0.7,
            IsHitTestVisible = false // Don't block hit testing on ports underneath
        };
        canvas.Children.Add(_tempLine);
        _tempLineContainer = canvas;
    }

    /// <summary>
    /// Creates the temporary connection line, using RootPanel for direct rendering mode
    /// or MainCanvas for visual tree mode.
    /// </summary>
    public void CreateTempLine(InputStateContext context)
    {
        _tempLine = new AvaloniaPath
        {
            Stroke = _theme.EdgeStroke,
            StrokeThickness = 2,
            StrokeDashArray = [5, 3],
            Opacity = 0.7,
            IsHitTestVisible = false
        };

        // In direct rendering mode, add to RootPanel (untransformed) and use viewport coords
        // In visual tree mode, add to MainCanvas (transformed) and use canvas coords
        if (context.DirectRenderer != null && context.RootPanel != null)
        {
            context.RootPanel.Children.Add(_tempLine);
            _tempLineContainer = context.RootPanel;
        }
        else if (context.MainCanvas != null)
        {
            context.MainCanvas.Children.Add(_tempLine);
            _tempLineContainer = context.MainCanvas;
        }
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);

        // Raise connect start event
        context.RaiseConnectStart(_sourceNode, _sourcePort, _fromOutput);
        _connectStartRaised = true;
    }

    public override void Exit(InputStateContext context)
    {
        // Restore cursor (only if we have a real visual)
        if (_portVisual != null)
        {
            _portVisual.Cursor = _previousCursor;
        }

        // Restore hovered port color
        RestoreHoveredPortColor();

        // Remove temp line from whichever container it was added to
        if (_tempLine != null && _tempLineContainer != null)
        {
            _tempLineContainer.Children.Remove(_tempLine);
            _tempLine = null;
            _tempLineContainer = null;
        }
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Store screen position for AutoPan edge detection and direct rendering mode
        var screenPos = GetScreenPosition(context, e);
        _endPointViewport = screenPos;

        // Get canvas position directly - MainCanvas has a transform, and GetPosition(MainCanvas)
        // automatically applies the inverse transform, giving us direct canvas coordinates
        _endPoint = GetCanvasPosition(context, e);

        // AutoPan: pan viewport when dragging near edges (use screen coordinates)
        if (context.Settings.EnableAutoPan && context.RootPanel != null)
        {
            var viewBounds = context.RootPanel.Bounds;
            var edgeDist = context.Settings.AutoPanEdgeDistance;
            var panSpeed = context.Settings.AutoPanSpeed;

            double panX = 0, panY = 0;
            if (screenPos.X < edgeDist) panX = panSpeed;
            else if (screenPos.X > viewBounds.Width - edgeDist) panX = -panSpeed;
            if (screenPos.Y < edgeDist) panY = panSpeed;
            else if (screenPos.Y > viewBounds.Height - edgeDist) panY = -panSpeed;

            if (panX != 0 || panY != 0)
            {
                context.Viewport.Pan(panX, panY);
                context.ApplyViewportTransform();
            }
        }

        // Try to find a snap target (uses canvas coordinates for distance calculation)
        _snappedTarget = FindSnapTarget(context, _endPoint);

        // Uncomment for debugging:
        // if (_debugMoveCount++ % 50 == 0)
        //     Console.WriteLine($"[CURSOR] screenPos={screenPos}, canvasPos={_endPoint}, zoom={context.Viewport.Zoom:F2}");
        UpdateTempLine(context);

        // Update port validation visual (uses canvas coordinates for hit testing)
        UpdatePortValidationVisual(context, _endPoint);

        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        // Get screen coordinates first, then convert to canvas using viewport
        // NOTE: Don't use e.GetPosition(MainCanvas) as it may not correctly apply the MatrixTransform
        var screenPoint = GetScreenPosition(context, e);
        var canvasPoint = context.ViewportToCanvas(screenPoint);

        bool connectionCompleted = false;
        Node? targetNode = null;
        Port? targetPort = null;

        // First try direct hit test on port (using canvas coordinates)
        // Use HitTestForPort which skips edge paths and markers that might block the port
        var hitElement = HitTestForPort(context, canvasPoint);

        if (hitElement is Control targetPortVisual &&
            targetPortVisual.Tag is (Node tn, Port tp, bool isOutput))
        {
            targetNode = tn;
            targetPort = tp;
        }
        // If no direct hit, try snap target
        else if (_snappedTarget.HasValue)
        {
            targetNode = _snappedTarget.Value.node;
            targetPort = _snappedTarget.Value.port;
            var snappedIsOutput = _snappedTarget.Value.isOutput;

            // Verify the snap is still valid
            if (_fromOutput == snappedIsOutput || !targetNode.IsConnectable)
            {
                targetNode = null;
                targetPort = null;
            }
        }
        else
        {
            // No direct hit and no snap target
        }

        // Attempt connection if we have a target
        if (targetNode != null && targetPort != null)
        {
            var targetIsOutput = targetNode.Outputs.Contains(targetPort);

            // Can only connect output to input (or input to output)
            if (_fromOutput != targetIsOutput && targetNode.IsConnectable)
            {
                // Validate connection if validator exists
                if (IsConnectionValid(context, targetNode, targetPort))
                {
                    var sourceNode = _fromOutput ? _sourceNode : targetNode;
                    var sourcePort = _fromOutput ? _sourcePort : targetPort;
                    var destNode = _fromOutput ? targetNode : _sourceNode;
                    var destPort = _fromOutput ? targetPort : _sourcePort;

                    context.RaiseConnectionCompleted(sourceNode, sourcePort, destNode, destPort);
                    connectionCompleted = true;
                }
            }
        }

        // Raise connect end event
        if (_connectStartRaised)
        {
            context.RaiseConnectEnd(_sourceNode, _sourcePort, targetNode, targetPort, connectionCompleted);
        }

        ReleasePointer(e);
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Raise connect end with cancelled
            if (_connectStartRaised)
            {
                context.RaiseConnectEnd(_sourceNode, _sourcePort, null, null, completed: false);
            }
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    private void UpdateTempLine(InputStateContext context)
    {
        if (_tempLine == null) return;

        // Get canvas coordinates for port positions
        var startPointCanvas = context.GraphRenderer.GetPortCanvasPosition(_sourceNode, _sourcePort, _fromOutput);

        // In direct rendering mode, temp line is in RootPanel (untransformed)
        // so we need to convert canvas coords to viewport coords
        AvaloniaPoint startPoint, endPoint;
        if (context.DirectRenderer != null)
        {
            // Start point: convert port position from canvas to viewport
            startPoint = context.CanvasToViewport(startPointCanvas);

            // End point: use viewport position directly for cursor,
            // or convert snapped port position from canvas to viewport
            if (_snappedTarget.HasValue)
            {
                var endPointCanvas = context.GraphRenderer.GetPortCanvasPosition(
                    _snappedTarget.Value.node,
                    _snappedTarget.Value.port,
                    _snappedTarget.Value.isOutput);
                endPoint = context.CanvasToViewport(endPointCanvas);
            }
            else
            {
                // Use the stored viewport position directly (cursor position in screen coords)
                endPoint = _endPointViewport;
            }
        }
        else
        {
            // In visual tree mode, temp line is in MainCanvas which has the transform
            // so we use canvas coordinates directly
            startPoint = startPointCanvas;

            if (_snappedTarget.HasValue)
            {
                endPoint = context.GraphRenderer.GetPortCanvasPosition(
                    _snappedTarget.Value.node,
                    _snappedTarget.Value.port,
                    _snappedTarget.Value.isOutput);
            }
            else
            {
                // _endPoint is already in canvas coordinates (from GetCanvasPosition)
                endPoint = _endPoint;
            }
        }

        var pathGeometry = BezierHelper.CreateBezierPath(startPoint, endPoint, !_fromOutput);
        _tempLine.Data = pathGeometry;
    }

    /// <summary>
    /// Finds the nearest compatible port within snap distance.
    /// OPTIMIZED: Uses early exit based on canvas distance to avoid checking all 5000 nodes.
    /// Uses canvas coordinates for all calculations since screen coordinate conversion has
    /// issues with RootPanel offsets.
    /// </summary>
    private (Node node, Port port, bool isOutput)? FindSnapTarget(InputStateContext context, AvaloniaPoint canvasPoint)
    {
        var graph = context.Graph;
        var settings = context.Settings;

        if (graph == null || !settings.SnapConnectionToNode || settings.ConnectionSnapDistance <= 0)
        {
            return null;
        }

        var snapDistance = settings.ConnectionSnapDistance;
        var zoom = context.Viewport.Zoom;

        // Convert snap distance to canvas coordinates (screen pixels / zoom = canvas units)
        var canvasSnapDistance = snapDistance / zoom;

        // OPTIMIZATION: For early rejection, add node width to allow for ports on node edges
        var earlyRejectDistance = canvasSnapDistance + settings.NodeWidth;

        (Node node, Port port, bool isOutput)? bestTarget = null;
        double bestDistance = double.MaxValue;

        foreach (var node in graph.Elements.Nodes)
        {
            // Skip the source node and non-connectable nodes
            if (node.Id == _sourceNode.Id || !node.IsConnectable || node.IsGroup)
                continue;

            // OPTIMIZATION: Early rejection based on canvas distance
            // If node center is too far from cursor in canvas coords, skip entirely
            var nodeWidth = node.Width ?? settings.NodeWidth;
            var nodeHeight = node.Height ?? settings.NodeHeight;
            var nodeCenterX = node.Position.X + nodeWidth / 2;
            var nodeCenterY = node.Position.Y + nodeHeight / 2;
            var canvasDx = nodeCenterX - canvasPoint.X;
            var canvasDy = nodeCenterY - canvasPoint.Y;
            var canvasDistSq = canvasDx * canvasDx + canvasDy * canvasDy;

            if (canvasDistSq > earlyRejectDistance * earlyRejectDistance)
            {
                continue;
            }

            // Check ports on the opposite side (if from output, look at inputs)
            var portsToCheck = _fromOutput ? node.Inputs : node.Outputs;
            var isOutput = !_fromOutput;

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
                    // Check if this connection would be valid
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
    /// Updates the visual appearance of ports during connection dragging.
    /// Shows green for valid connections, red for invalid ones.
    /// </summary>
    /// <param name="context">The input state context.</param>
    /// <param name="canvasPoint">The cursor position in canvas coordinates.</param>
    private void UpdatePortValidationVisual(InputStateContext context, AvaloniaPoint canvasPoint)
    {
        // First check direct hit test (uses canvas coordinates)
        // Use HitTestForPort which skips edge paths and markers
        var hitElement = HitTestForPort(context, canvasPoint);
        Control? targetPortVisual = null;
        Node? targetNode = null;
        Port? targetPort = null;
        bool isOutput = false;

        if (hitElement is Control portVisual &&
            portVisual.Tag is (Node tn, Port tp, bool io) &&
            portVisual != _portVisual)
        {
            targetPortVisual = portVisual;
            targetNode = tn;
            targetPort = tp;
            isOutput = io;
        }
        // If no direct hit, check snap target
        else if (_snappedTarget.HasValue)
        {
            targetNode = _snappedTarget.Value.node;
            targetPort = _snappedTarget.Value.port;
            isOutput = _snappedTarget.Value.isOutput;

            // Get the visual for the snapped port
            targetPortVisual = context.GraphRenderer.GetPortVisual(targetNode.Id, targetPort.Id);
        }

        if (targetPortVisual != null && targetNode != null && targetPort != null)
        {
            // New port being hovered/snapped
            if (_hoveredPortVisual != targetPortVisual)
            {
                // Restore previous hovered port
                RestoreHoveredPortColor();

                // Store new hovered port info
                _hoveredPortVisual = targetPortVisual;
                _hoveredPortOriginalFill = GetPortFill(targetPortVisual);

                // Determine if connection would be valid
                bool canConnect = _fromOutput != isOutput && targetNode.IsConnectable;
                bool isValid = canConnect && IsConnectionValid(context, targetNode, targetPort);

                // Apply validation color
                if (!canConnect)
                {
                    SetPortFill(targetPortVisual, _theme.PortInvalidConnection);
                }
                else if (isValid)
                {
                    SetPortFill(targetPortVisual, _theme.PortValidConnection);
                }
                else
                {
                    SetPortFill(targetPortVisual, _theme.PortInvalidConnection);
                }
            }
        }
        else
        {
            // Not hovering over a port - restore previous
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

    /// <summary>
    /// Restores the hovered port to its original color.
    /// </summary>
    private void RestoreHoveredPortColor()
    {
        if (_hoveredPortVisual != null)
        {
            SetPortFill(_hoveredPortVisual, _hoveredPortOriginalFill);
        }
        _hoveredPortVisual = null;
        _hoveredPortOriginalFill = null;
    }

    /// <summary>
    /// Checks if a connection to the target port is valid using the validator.
    /// </summary>
    private bool IsConnectionValid(InputStateContext context, Node targetNode, Port targetPort)
    {
        var graph = context.Graph;
        if (graph == null) return true;

        var validator = context.ConnectionValidator;
        if (validator == null) return true;

        // Build the connection context
        var sourceNode = _fromOutput ? _sourceNode : targetNode;
        var sourcePort = _fromOutput ? _sourcePort : targetPort;
        var destNode = _fromOutput ? targetNode : _sourceNode;
        var destPort = _fromOutput ? targetPort : _sourcePort;

        var connectionContext = new ConnectionContext
        {
            SourceNode = sourceNode,
            SourcePort = sourcePort,
            TargetNode = destNode,
            TargetPort = destPort,
            Graph = graph
        };

        var result = validator.Validate(connectionContext);
        return result.IsValid;
    }
}
