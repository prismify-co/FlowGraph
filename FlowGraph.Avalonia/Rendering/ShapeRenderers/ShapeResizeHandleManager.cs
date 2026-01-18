using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core.Elements.Shapes;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Manages rendering and tracking of resize handle visuals for shape elements.
/// </summary>
public class ShapeResizeHandleManager
{
    private readonly RenderContext _renderContext;
    private readonly Dictionary<string, List<Rectangle>> _resizeHandles = new();

    /// <summary>
    /// Creates a new shape resize handle manager.
    /// </summary>
    public ShapeResizeHandleManager(RenderContext renderContext)
    {
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
    }

    /// <summary>
    /// Clears all tracked resize handles.
    /// </summary>
    public void Clear()
    {
        _resizeHandles.Clear();
    }

    /// <summary>
    /// Renders resize handles for a selected, resizable shape.
    /// </summary>
    public void RenderResizeHandles(
        Canvas canvas,
        ShapeElement shape,
        ThemeResources theme,
        Action<Rectangle, ShapeElement, ResizeHandlePosition>? onHandleCreated = null)
    {
        RemoveResizeHandles(canvas, shape.Id);

        if (!shape.IsSelected || !shape.IsResizable)
            return;

        var inverseScale = _renderContext.InverseScale;
        var handleSize = 8 * inverseScale;
        var shapeWidth = shape.Width ?? 100;
        var shapeHeight = shape.Height ?? 100;
        var canvasPos = new AvaloniaPoint(shape.Position.X, shape.Position.Y);

        var handles = new List<Rectangle>();
        var positions = new[]
        {
            ResizeHandlePosition.TopLeft,
            ResizeHandlePosition.TopRight,
            ResizeHandlePosition.BottomLeft,
            ResizeHandlePosition.BottomRight,
            ResizeHandlePosition.Top,
            ResizeHandlePosition.Bottom,
            ResizeHandlePosition.Left,
            ResizeHandlePosition.Right
        };

        foreach (var position in positions)
        {
            var handle = CreateResizeHandle(handleSize, theme, shape, position);
            PositionResizeHandle(handle, canvasPos, shapeWidth, shapeHeight, handleSize, position);

            canvas.Children.Add(handle);
            handles.Add(handle);

            onHandleCreated?.Invoke(handle, shape, position);
        }

        _resizeHandles[shape.Id] = handles;
    }

    /// <summary>
    /// Removes resize handles for a specific shape.
    /// </summary>
    public void RemoveResizeHandles(Canvas canvas, string shapeId)
    {
        if (_resizeHandles.TryGetValue(shapeId, out var handles))
        {
            foreach (var handle in handles)
            {
                canvas.Children.Remove(handle);
            }
            _resizeHandles.Remove(shapeId);
        }
    }

    /// <summary>
    /// Removes all resize handles from the canvas.
    /// </summary>
    public void RemoveAllResizeHandles(Canvas canvas)
    {
        foreach (var (_, handles) in _resizeHandles)
        {
            foreach (var handle in handles)
            {
                canvas.Children.Remove(handle);
            }
        }
        _resizeHandles.Clear();
    }

    /// <summary>
    /// Updates size and position of all tracked resize handles.
    /// </summary>
    public void UpdateAllResizeHandles()
    {
        var inverseScale = _renderContext.InverseScale;
        var handleSize = 8 * inverseScale;

        foreach (var (shapeId, handles) in _resizeHandles)
        {
            foreach (var handle in handles)
            {
                if (handle.Tag is (ShapeElement shape, ResizeHandlePosition position))
                {
                    var shapeWidth = shape.Width ?? 100;
                    var shapeHeight = shape.Height ?? 100;
                    var canvasPos = new AvaloniaPoint(shape.Position.X, shape.Position.Y);

                    handle.Width = handleSize;
                    handle.Height = handleSize;

                    PositionResizeHandle(handle, canvasPos, shapeWidth, shapeHeight, handleSize, position);
                }
            }
        }
    }

    private Rectangle CreateResizeHandle(double handleSize, ThemeResources theme, ShapeElement shape, ResizeHandlePosition position)
    {
        var handle = new Rectangle
        {
            Width = handleSize,
            Height = handleSize,
            Fill = Brushes.White,
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 1 * _renderContext.InverseScale,
            Tag = (shape, position),
            ZIndex = 10000, // Above everything
            IsHitTestVisible = true,
            Cursor = GetCursorForPosition(position)
        };

        return handle;
    }

    private void PositionResizeHandle(Rectangle handle, AvaloniaPoint canvasPos, double shapeWidth, double shapeHeight, double handleSize, ResizeHandlePosition position)
    {
        var halfHandle = handleSize / 2;
        double left, top;

        switch (position)
        {
            case ResizeHandlePosition.TopLeft:
                left = canvasPos.X - halfHandle;
                top = canvasPos.Y - halfHandle;
                break;
            case ResizeHandlePosition.TopRight:
                left = canvasPos.X + shapeWidth - halfHandle;
                top = canvasPos.Y - halfHandle;
                break;
            case ResizeHandlePosition.BottomLeft:
                left = canvasPos.X - halfHandle;
                top = canvasPos.Y + shapeHeight - halfHandle;
                break;
            case ResizeHandlePosition.BottomRight:
                left = canvasPos.X + shapeWidth - halfHandle;
                top = canvasPos.Y + shapeHeight - halfHandle;
                break;
            case ResizeHandlePosition.Top:
                left = canvasPos.X + (shapeWidth / 2) - halfHandle;
                top = canvasPos.Y - halfHandle;
                break;
            case ResizeHandlePosition.Bottom:
                left = canvasPos.X + (shapeWidth / 2) - halfHandle;
                top = canvasPos.Y + shapeHeight - halfHandle;
                break;
            case ResizeHandlePosition.Left:
                left = canvasPos.X - halfHandle;
                top = canvasPos.Y + (shapeHeight / 2) - halfHandle;
                break;
            case ResizeHandlePosition.Right:
                left = canvasPos.X + shapeWidth - halfHandle;
                top = canvasPos.Y + (shapeHeight / 2) - halfHandle;
                break;
            default:
                left = canvasPos.X;
                top = canvasPos.Y;
                break;
        }

        Canvas.SetLeft(handle, left);
        Canvas.SetTop(handle, top);
        handle.ZIndex = 10000;
    }

    private static Cursor GetCursorForPosition(ResizeHandlePosition position)
    {
        return position switch
        {
            ResizeHandlePosition.TopLeft => new Cursor(StandardCursorType.TopLeftCorner),
            ResizeHandlePosition.TopRight => new Cursor(StandardCursorType.TopRightCorner),
            ResizeHandlePosition.BottomLeft => new Cursor(StandardCursorType.BottomLeftCorner),
            ResizeHandlePosition.BottomRight => new Cursor(StandardCursorType.BottomRightCorner),
            ResizeHandlePosition.Top => new Cursor(StandardCursorType.TopSide),
            ResizeHandlePosition.Bottom => new Cursor(StandardCursorType.BottomSide),
            ResizeHandlePosition.Left => new Cursor(StandardCursorType.LeftSide),
            ResizeHandlePosition.Right => new Cursor(StandardCursorType.RightSide),
            _ => new Cursor(StandardCursorType.Arrow)
        };
    }
}
