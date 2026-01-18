using Avalonia;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core.Elements.Shapes;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for resizing a shape element via drag handles.
/// </summary>
public class ResizingShapeState : InputStateBase
{
    private readonly ShapeElement _shape;
    private readonly ResizeHandlePosition _handlePosition;
    private readonly AvaloniaPoint _startPoint;
    private readonly double _startWidth;
    private readonly double _startHeight;
    private readonly Core.Point _startPosition;
    private readonly FlowCanvasSettings _settings;
    private readonly ViewportState _viewport;

    public override string Name => "ResizingShape";

    public ResizingShapeState(
        ShapeElement shape,
        ResizeHandlePosition handlePosition,
        AvaloniaPoint startPoint,
        FlowCanvasSettings settings,
        ViewportState viewport)
    {
        _shape = shape;
        _handlePosition = handlePosition;
        _startPoint = startPoint;
        _startWidth = shape.Width ?? 100;
        _startHeight = shape.Height ?? 100;
        _startPosition = shape.Position;
        _settings = settings;
        _viewport = viewport;
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        var currentPoint = GetTypedViewportPosition(context, e);
        var deltaX = (currentPoint.X - _startPoint.X) / _viewport.Zoom;
        var deltaY = (currentPoint.Y - _startPoint.Y) / _viewport.Zoom;

        // Minimum sizes for shapes
        const double minWidth = 60;
        const double minHeight = 40;

        // Figma-style modifiers:
        // Alt = Resize from center (symmetrical)
        // Shift = Maintain aspect ratio (proportional)
        var symmetrical = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        var proportional = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        var (newWidth, newHeight, newX, newY) = CalculateNewDimensions(
            deltaX, deltaY, minWidth, minHeight, symmetrical, proportional);

        // Apply snap to grid
        if (_settings.SnapToGrid)
        {
            var snapSize = _settings.EffectiveSnapGridSize;
            newWidth = Math.Max(minWidth, Math.Round(newWidth / snapSize) * snapSize);
            newHeight = Math.Max(minHeight, Math.Round(newHeight / snapSize) * snapSize);
            newX = Math.Round(newX / snapSize) * snapSize;
            newY = Math.Round(newY / snapSize) * snapSize;
        }

        // Apply the resize
        _shape.Width = newWidth;
        _shape.Height = newHeight;
        _shape.Position = new Core.Point(newX, newY);

        // Raise resizing event
        context.RaiseShapeResizing(_shape, newWidth, newHeight, new Core.Point(newX, newY));
        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        var finalWidth = _shape.Width ?? _startWidth;
        var finalHeight = _shape.Height ?? _startHeight;
        var finalPosition = _shape.Position;

        bool changed = Math.Abs(finalWidth - _startWidth) > 0.1 ||
                       Math.Abs(finalHeight - _startHeight) > 0.1 ||
                       Math.Abs(finalPosition.X - _startPosition.X) > 0.1 ||
                       Math.Abs(finalPosition.Y - _startPosition.Y) > 0.1;

        if (changed)
        {
            context.RaiseShapeResized(
                _shape,
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
            _shape.Width = _startWidth;
            _shape.Height = _startHeight;
            _shape.Position = _startPosition;
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    private (double width, double height, double x, double y) CalculateNewDimensions(
        double deltaX, double deltaY, double minWidth, double minHeight,
        bool symmetrical = false, bool proportional = false)
    {
        var newWidth = _startWidth;
        var newHeight = _startHeight;
        var newX = _startPosition.X;
        var newY = _startPosition.Y;

        // For proportional resize, calculate aspect ratio
        var aspectRatio = _startWidth / _startHeight;

        // Calculate raw dimensions based on handle position
        switch (_handlePosition)
        {
            case ResizeHandlePosition.Right:
                newWidth = Math.Max(minWidth, _startWidth + deltaX);
                if (proportional)
                    newHeight = newWidth / aspectRatio;
                if (symmetrical)
                {
                    newX = _startPosition.X - (newWidth - _startWidth) / 2;
                    if (proportional)
                        newY = _startPosition.Y - (newHeight - _startHeight) / 2;
                }
                break;

            case ResizeHandlePosition.Left:
                var leftDelta = Math.Min(deltaX, _startWidth - minWidth);
                newWidth = _startWidth - leftDelta;
                if (proportional)
                    newHeight = newWidth / aspectRatio;
                if (symmetrical)
                {
                    newX = _startPosition.X + leftDelta / 2;
                    if (proportional)
                        newY = _startPosition.Y - (newHeight - _startHeight) / 2;
                }
                else
                {
                    newX = _startPosition.X + leftDelta;
                }
                break;

            case ResizeHandlePosition.Bottom:
                newHeight = Math.Max(minHeight, _startHeight + deltaY);
                if (proportional)
                    newWidth = newHeight * aspectRatio;
                if (symmetrical)
                {
                    newY = _startPosition.Y - (newHeight - _startHeight) / 2;
                    if (proportional)
                        newX = _startPosition.X - (newWidth - _startWidth) / 2;
                }
                break;

            case ResizeHandlePosition.Top:
                var topDelta = Math.Min(deltaY, _startHeight - minHeight);
                newHeight = _startHeight - topDelta;
                if (proportional)
                    newWidth = newHeight * aspectRatio;
                if (symmetrical)
                {
                    newY = _startPosition.Y + topDelta / 2;
                    if (proportional)
                        newX = _startPosition.X - (newWidth - _startWidth) / 2;
                }
                else
                {
                    newY = _startPosition.Y + topDelta;
                }
                break;

            case ResizeHandlePosition.BottomRight:
                newWidth = Math.Max(minWidth, _startWidth + deltaX);
                newHeight = Math.Max(minHeight, _startHeight + deltaY);
                if (proportional)
                {
                    // Use the larger delta to drive proportional resize
                    var widthRatio = newWidth / _startWidth;
                    var heightRatio = newHeight / _startHeight;
                    if (widthRatio > heightRatio)
                        newHeight = newWidth / aspectRatio;
                    else
                        newWidth = newHeight * aspectRatio;
                }
                if (symmetrical)
                {
                    newX = _startPosition.X - (newWidth - _startWidth) / 2;
                    newY = _startPosition.Y - (newHeight - _startHeight) / 2;
                }
                break;

            case ResizeHandlePosition.BottomLeft:
                var blLeftDelta = Math.Min(deltaX, _startWidth - minWidth);
                newWidth = _startWidth - blLeftDelta;
                newHeight = Math.Max(minHeight, _startHeight + deltaY);
                if (proportional)
                {
                    var widthRatio = newWidth / _startWidth;
                    var heightRatio = newHeight / _startHeight;
                    if (widthRatio < heightRatio)
                        newHeight = newWidth / aspectRatio;
                    else
                    {
                        var oldWidth = newWidth;
                        newWidth = newHeight * aspectRatio;
                        blLeftDelta = _startWidth - newWidth;
                    }
                }
                if (symmetrical)
                {
                    newX = _startPosition.X + blLeftDelta / 2;
                    newY = _startPosition.Y - (newHeight - _startHeight) / 2;
                }
                else
                {
                    newX = _startPosition.X + blLeftDelta;
                }
                break;

            case ResizeHandlePosition.TopRight:
                newWidth = Math.Max(minWidth, _startWidth + deltaX);
                var trTopDelta = Math.Min(deltaY, _startHeight - minHeight);
                newHeight = _startHeight - trTopDelta;
                if (proportional)
                {
                    var widthRatio = newWidth / _startWidth;
                    var heightRatio = newHeight / _startHeight;
                    if (widthRatio > heightRatio)
                    {
                        newHeight = newWidth / aspectRatio;
                        trTopDelta = _startHeight - newHeight;
                    }
                    else
                        newWidth = newHeight * aspectRatio;
                }
                if (symmetrical)
                {
                    newX = _startPosition.X - (newWidth - _startWidth) / 2;
                    newY = _startPosition.Y + trTopDelta / 2;
                }
                else
                {
                    newY = _startPosition.Y + trTopDelta;
                }
                break;

            case ResizeHandlePosition.TopLeft:
                var tlLeftDelta = Math.Min(deltaX, _startWidth - minWidth);
                newWidth = _startWidth - tlLeftDelta;
                var tlTopDelta = Math.Min(deltaY, _startHeight - minHeight);
                newHeight = _startHeight - tlTopDelta;
                if (proportional)
                {
                    var widthRatio = newWidth / _startWidth;
                    var heightRatio = newHeight / _startHeight;
                    if (widthRatio < heightRatio)
                    {
                        newHeight = newWidth / aspectRatio;
                        tlTopDelta = _startHeight - newHeight;
                    }
                    else
                    {
                        newWidth = newHeight * aspectRatio;
                        tlLeftDelta = _startWidth - newWidth;
                    }
                }
                if (symmetrical)
                {
                    newX = _startPosition.X + tlLeftDelta / 2;
                    newY = _startPosition.Y + tlTopDelta / 2;
                }
                else
                {
                    newX = _startPosition.X + tlLeftDelta;
                    newY = _startPosition.Y + tlTopDelta;
                }
                break;
        }

        // Enforce minimum sizes
        newWidth = Math.Max(minWidth, newWidth);
        newHeight = Math.Max(minHeight, newHeight);

        return (newWidth, newHeight, newX, newY);
    }
}
