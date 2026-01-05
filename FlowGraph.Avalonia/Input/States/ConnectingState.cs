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
    private readonly Ellipse _portVisual;
    private readonly Cursor? _previousCursor;
    private readonly ThemeResources _theme;

    // Track hovered port for validation visual feedback
    private Ellipse? _hoveredPortVisual;
    private IBrush? _hoveredPortOriginalFill;
    
    // Track whether connect start was raised
    private bool _connectStartRaised;

    public override string Name => "Connecting";
    public override bool IsModal => true;

    public ConnectingState(
        Node sourceNode, 
        Port sourcePort, 
        bool fromOutput, 
        AvaloniaPoint startPosition,
        Ellipse portVisual,
        ThemeResources theme)
    {
        _sourceNode = sourceNode;
        _sourcePort = sourcePort;
        _fromOutput = fromOutput;
        _endPoint = startPosition;
        _portVisual = portVisual;
        _previousCursor = portVisual.Cursor;
        _theme = theme;
        _connectStartRaised = false;

        // Change cursor during connection
        portVisual.Cursor = new Cursor(StandardCursorType.Hand);
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
        
        // Raise connect start event
        context.RaiseConnectStart(_sourceNode, _sourcePort, _fromOutput);
        _connectStartRaised = true;
    }

    public override void Exit(InputStateContext context)
    {
        // Restore cursor
        _portVisual.Cursor = _previousCursor;

        // Restore hovered port color
        RestoreHoveredPortColor();

        // Remove temp line
        if (_tempLine != null && context.MainCanvas != null)
        {
            context.MainCanvas.Children.Remove(_tempLine);
            _tempLine = null;
        }
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        _endPoint = GetPosition(context, e);
        UpdateTempLine(context);
        
        // Update port validation visual
        UpdatePortValidationVisual(context, _endPoint);
        
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        var screenPoint = GetPosition(context, e);
        var hitElement = HitTest(context, screenPoint);
        
        bool connectionCompleted = false;
        Node? targetNode = null;
        Port? targetPort = null;

        if (hitElement is Ellipse targetPortVisual && 
            targetPortVisual.Tag is (Node tn, Port tp, bool isOutput))
        {
            targetNode = tn;
            targetPort = tp;
            
            // Can only connect output to input (or input to output)
            // Also check that target node allows connections
            if (_fromOutput != isOutput && targetNode.IsConnectable)
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

        var startPoint = context.GraphRenderer.GetPortPosition(_sourceNode, _sourcePort, _fromOutput);
        var pathGeometry = BezierHelper.CreateBezierPath(startPoint, _endPoint, !_fromOutput);
        _tempLine.Data = pathGeometry;
    }

    /// <summary>
    /// Updates the visual appearance of ports during connection dragging.
    /// Shows green for valid connections, red for invalid ones.
    /// </summary>
    private void UpdatePortValidationVisual(InputStateContext context, AvaloniaPoint screenPoint)
    {
        var hitElement = HitTest(context, screenPoint);

        // Check if we're hovering over a port
        if (hitElement is Ellipse targetPortVisual && 
            targetPortVisual.Tag is (Node targetNode, Port targetPort, bool isOutput) &&
            targetPortVisual != _portVisual) // Don't highlight source port
        {
            // New port being hovered
            if (_hoveredPortVisual != targetPortVisual)
            {
                // Restore previous hovered port
                RestoreHoveredPortColor();

                // Store new hovered port info
                _hoveredPortVisual = targetPortVisual;
                _hoveredPortOriginalFill = targetPortVisual.Fill;

                // Determine if connection would be valid
                bool canConnect = _fromOutput != isOutput && targetNode.IsConnectable;
                bool isValid = canConnect && IsConnectionValid(context, targetNode, targetPort);

                // Apply validation color
                if (!canConnect)
                {
                    // Can't connect (same direction or node not connectable)
                    targetPortVisual.Fill = _theme.PortInvalidConnection;
                }
                else if (isValid)
                {
                    targetPortVisual.Fill = _theme.PortValidConnection;
                }
                else
                {
                    // Connection rejected by validator
                    targetPortVisual.Fill = _theme.PortInvalidConnection;
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
    /// Restores the hovered port to its original color.
    /// </summary>
    private void RestoreHoveredPortColor()
    {
        if (_hoveredPortVisual != null && _hoveredPortOriginalFill != null)
        {
            _hoveredPortVisual.Fill = _hoveredPortOriginalFill;
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

        // Get the validator from FlowCanvas (via a method we'll need to add to context)
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
