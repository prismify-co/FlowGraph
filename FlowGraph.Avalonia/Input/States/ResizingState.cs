using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for resizing a node via drag handles.
/// </summary>
public class ResizingState : InputStateBase
{
    private readonly Node _node;
    private readonly ResizeHandlePosition _handlePosition;
    private readonly AvaloniaPoint _startPoint;
    private readonly double _startWidth;
    private readonly double _startHeight;
    private readonly Core.Point _startPosition;
    private readonly FlowCanvasSettings _settings;
    private readonly ViewportState _viewport;
    private readonly GraphRenderer _graphRenderer;

    public override string Name => "Resizing";

    public ResizingState(
        Node node,
        ResizeHandlePosition handlePosition,
        AvaloniaPoint startPoint,
        FlowCanvasSettings settings,
        ViewportState viewport,
        GraphRenderer graphRenderer)
    {
        _node = node;
        _handlePosition = handlePosition;
        _startPoint = startPoint;
        _startWidth = node.Width ?? settings.NodeWidth;
        _startHeight = node.Height ?? settings.NodeHeight;
        _startPosition = node.Position;
        _settings = settings;
        _viewport = viewport;
        _graphRenderer = graphRenderer;
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        var currentPoint = GetScreenPosition(context, e);
        var deltaX = (currentPoint.X - _startPoint.X) / _viewport.Zoom;
        var deltaY = (currentPoint.Y - _startPoint.Y) / _viewport.Zoom;

        // Get minimum sizes
        var renderer = _graphRenderer.NodeRenderers.GetRenderer(_node.Type);
        var minWidth = renderer.GetMinWidth(_node, _settings) ?? 60;
        var minHeight = renderer.GetMinHeight(_node, _settings) ?? 40;

        var (newWidth, newHeight, newX, newY) = CalculateNewDimensions(deltaX, deltaY, minWidth, minHeight);

        // Apply snap to grid
        if (_settings.SnapToGrid)
        {
            var snapSize = _settings.EffectiveSnapGridSize;
            newWidth = Math.Max(minWidth, Math.Round(newWidth / snapSize) * snapSize);
            newHeight = Math.Max(minHeight, Math.Round(newHeight / snapSize) * snapSize);
            newX = Math.Round(newX / snapSize) * snapSize;
            newY = Math.Round(newY / snapSize) * snapSize;
        }

        context.RaiseNodeResizing(_node, newWidth, newHeight, new Core.Point(newX, newY));
        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        var finalWidth = _node.Width ?? _startWidth;
        var finalHeight = _node.Height ?? _startHeight;
        var finalPosition = _node.Position;

        bool changed = Math.Abs(finalWidth - _startWidth) > 0.1 ||
                       Math.Abs(finalHeight - _startHeight) > 0.1 ||
                       Math.Abs(finalPosition.X - _startPosition.X) > 0.1 ||
                       Math.Abs(finalPosition.Y - _startPosition.Y) > 0.1;

        if (changed)
        {
            context.RaiseNodeResized(
                _node,
                _startWidth, _startHeight,
                finalWidth, finalHeight,
                _startPosition, finalPosition);
        }

        ReleasePointer(e);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel resize - restore original size
            _node.Width = _startWidth;
            _node.Height = _startHeight;
            _node.Position = _startPosition;
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    private (double width, double height, double x, double y) CalculateNewDimensions(
        double deltaX, double deltaY, double minWidth, double minHeight)
    {
        var newWidth = _startWidth;
        var newHeight = _startHeight;
        var newX = _startPosition.X;
        var newY = _startPosition.Y;

        switch (_handlePosition)
        {
            case ResizeHandlePosition.Right:
                newWidth = Math.Max(minWidth, _startWidth + deltaX);
                break;

            case ResizeHandlePosition.Left:
                var leftDelta = Math.Min(deltaX, _startWidth - minWidth);
                newWidth = _startWidth - leftDelta;
                newX = _startPosition.X + leftDelta;
                break;

            case ResizeHandlePosition.Bottom:
                newHeight = Math.Max(minHeight, _startHeight + deltaY);
                break;

            case ResizeHandlePosition.Top:
                var topDelta = Math.Min(deltaY, _startHeight - minHeight);
                newHeight = _startHeight - topDelta;
                newY = _startPosition.Y + topDelta;
                break;

            case ResizeHandlePosition.BottomRight:
                newWidth = Math.Max(minWidth, _startWidth + deltaX);
                newHeight = Math.Max(minHeight, _startHeight + deltaY);
                break;

            case ResizeHandlePosition.BottomLeft:
                var blLeftDelta = Math.Min(deltaX, _startWidth - minWidth);
                newWidth = _startWidth - blLeftDelta;
                newX = _startPosition.X + blLeftDelta;
                newHeight = Math.Max(minHeight, _startHeight + deltaY);
                break;

            case ResizeHandlePosition.TopRight:
                newWidth = Math.Max(minWidth, _startWidth + deltaX);
                var trTopDelta = Math.Min(deltaY, _startHeight - minHeight);
                newHeight = _startHeight - trTopDelta;
                newY = _startPosition.Y + trTopDelta;
                break;

            case ResizeHandlePosition.TopLeft:
                var tlLeftDelta = Math.Min(deltaX, _startWidth - minWidth);
                newWidth = _startWidth - tlLeftDelta;
                newX = _startPosition.X + tlLeftDelta;
                var tlTopDelta = Math.Min(deltaY, _startHeight - minHeight);
                newHeight = _startHeight - tlTopDelta;
                newY = _startPosition.Y + tlTopDelta;
                break;
        }

        return (newWidth, newHeight, newX, newY);
    }
}
