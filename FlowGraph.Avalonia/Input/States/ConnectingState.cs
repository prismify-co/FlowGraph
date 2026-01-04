using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
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

    public override void Exit(InputStateContext context)
    {
        // Restore cursor
        _portVisual.Cursor = _previousCursor;

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
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        var screenPoint = GetPosition(context, e);
        var hitElement = HitTest(context, screenPoint);

        if (hitElement is Ellipse targetPortVisual && 
            targetPortVisual.Tag is (Node targetNode, Port targetPort, bool isOutput))
        {
            // Can only connect output to input (or input to output)
            if (_fromOutput != isOutput)
            {
                var sourceNode = _fromOutput ? _sourceNode : targetNode;
                var sourcePort = _fromOutput ? _sourcePort : targetPort;
                var destNode = _fromOutput ? targetNode : _sourceNode;
                var destPort = _fromOutput ? targetPort : _sourcePort;

                context.RaiseConnectionCompleted(sourceNode, sourcePort, destNode, destPort);
            }
        }

        ReleasePointer(e);
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
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
}
