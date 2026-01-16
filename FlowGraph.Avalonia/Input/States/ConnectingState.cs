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
    private Ellipse? _debugMarker; // DEBUG: Visual marker to show where the endpoint is
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
            Opacity = 0.7
        };
        canvas.Children.Add(_tempLine);

        // DEBUG: Create a visible marker at the endpoint position (red circle)
        _debugMarker = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = Brushes.Red,
            Stroke = Brushes.White,
            StrokeThickness = 2
        };
        canvas.Children.Add(_debugMarker);
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

        // Remove temp line
        if (_tempLine != null && context.MainCanvas != null)
        {
            context.MainCanvas.Children.Remove(_tempLine);
            _tempLine = null;
        }

        // DEBUG: Remove debug marker
        if (_debugMarker != null && context.MainCanvas != null)
        {
            context.MainCanvas.Children.Remove(_debugMarker);
            _debugMarker = null;
        }
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Store screen position for AutoPan edge detection
        var screenPos = GetPosition(context, e);
        
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

        // Try to find a snap target (uses screen coordinates for distance calculation)
        _snappedTarget = FindSnapTarget(context, screenPos);

        UpdateTempLine(context);

        // Update port validation visual (uses canvas coordinates for hit testing)
        UpdatePortValidationVisual(context, _endPoint);

        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        // Get canvas coordinates for hit testing
        var canvasPoint = GetCanvasPosition(context, e);

        bool connectionCompleted = false;
        Node? targetNode = null;
        Port? targetPort = null;

        // First try direct hit test on port (using canvas coordinates)
        var hitElement = HitTest(context, canvasPoint);

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

        // Use canvas coordinates for path geometry on MainCanvas (which uses MatrixTransform)
        var startPoint = context.GraphRenderer.GetPortCanvasPosition(_sourceNode, _sourcePort, _fromOutput);

        // If we have a snapped target, draw to that port instead of the cursor
        AvaloniaPoint endPoint;
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

        var pathGeometry = BezierHelper.CreateBezierPath(startPoint, endPoint, !_fromOutput);
        _tempLine.Data = pathGeometry;

        // DEBUG: Position the debug marker at the endpoint (centered on the point)
        if (_debugMarker != null)
        {
            Canvas.SetLeft(_debugMarker, endPoint.X - 5);
            Canvas.SetTop(_debugMarker, endPoint.Y - 5);
        }
    }

    /// <summary>
    /// Finds the nearest compatible port within snap distance.
    /// OPTIMIZED: Uses early exit based on canvas distance to avoid checking all 5000 nodes.
    /// </summary>
    private (Node node, Port port, bool isOutput)? FindSnapTarget(InputStateContext context, AvaloniaPoint screenPoint)
    {
        var graph = context.Graph;
        var settings = context.Settings;

        if (graph == null || !settings.SnapConnectionToNode || settings.ConnectionSnapDistance <= 0)
            return null;

        var snapDistance = settings.ConnectionSnapDistance;
        var canvasPoint = context.ScreenToCanvas(screenPoint);
        var zoom = context.Viewport.Zoom;

        // OPTIMIZATION: Convert snap distance to canvas coordinates for early rejection
        // Add some padding to account for node width
        var canvasSnapDistance = (snapDistance / zoom) + settings.NodeWidth;

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

            if (canvasDistSq > canvasSnapDistance * canvasSnapDistance)
                continue;

            // Check ports on the opposite side (if from output, look at inputs)
            var portsToCheck = _fromOutput ? node.Inputs : node.Outputs;
            var isOutput = !_fromOutput;

            foreach (var port in portsToCheck)
            {
                // Get the port's screen position for distance calculation
                var portScreenPos = context.GraphRenderer.GetPortScreenPosition(node, port, isOutput);

                // Calculate distance
                var dx = portScreenPos.X - screenPoint.X;
                var dy = portScreenPos.Y - screenPoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < snapDistance && distance < bestDistance)
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
        var hitElement = HitTest(context, canvasPoint);
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
