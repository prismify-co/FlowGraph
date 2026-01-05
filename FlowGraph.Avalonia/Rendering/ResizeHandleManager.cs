using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Manages rendering and tracking of resize handle visuals for nodes.
/// Responsible for creating, updating, and removing resize handle UI elements.
/// </summary>
public class ResizeHandleManager
{
    private readonly RenderContext _renderContext;
    private readonly NodeVisualManager _nodeVisualManager;

    // Visual tracking
    private readonly Dictionary<string, List<Rectangle>> _resizeHandles = new();

    /// <summary>
    /// Creates a new resize handle manager.
    /// </summary>
    /// <param name="renderContext">Shared render context.</param>
    /// <param name="nodeVisualManager">Node visual manager for dimension calculations.</param>
    public ResizeHandleManager(RenderContext renderContext, NodeVisualManager nodeVisualManager)
    {
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
        _nodeVisualManager = nodeVisualManager ?? throw new ArgumentNullException(nameof(nodeVisualManager));
    }

    /// <summary>
    /// Clears all tracked resize handles.
    /// Note: This does not remove them from the canvas.
    /// </summary>
    public void Clear()
    {
        _resizeHandles.Clear();
    }

    /// <summary>
    /// Renders resize handles for a selected, resizable node.
    /// </summary>
    /// <param name="canvas">The canvas to render to.</param>
    /// <param name="node">The node to render handles for.</param>
    /// <param name="theme">Theme resources for styling.</param>
    /// <param name="onHandleCreated">Optional callback when a handle is created.</param>
    public void RenderResizeHandles(
        Canvas canvas,
        Node node,
        ThemeResources theme,
        Action<Rectangle, Node, ResizeHandlePosition>? onHandleCreated = null)
    {
        // Remove existing handles for this node
        RemoveResizeHandles(canvas, node.Id);

        if (!node.IsSelected || !node.IsResizable)
            return;

        var scale = _renderContext.Scale;
        var handleSize = 8 * scale;
        var (nodeWidth, nodeHeight) = _nodeVisualManager.GetNodeDimensions(node);
        var screenPos = _renderContext.CanvasToScreen(node.Position.X, node.Position.Y);
        var scaledWidth = nodeWidth * scale;
        var scaledHeight = nodeHeight * scale;

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
            var handle = CreateResizeHandle(handleSize, theme, node, position);
            PositionResizeHandle(handle, screenPos, scaledWidth, scaledHeight, handleSize, position);

            canvas.Children.Add(handle);
            handles.Add(handle);

            onHandleCreated?.Invoke(handle, node, position);
        }

        _resizeHandles[node.Id] = handles;
    }

    /// <summary>
    /// Removes resize handles for a specific node.
    /// </summary>
    /// <param name="canvas">The canvas containing the handles.</param>
    /// <param name="nodeId">The ID of the node.</param>
    public void RemoveResizeHandles(Canvas canvas, string nodeId)
    {
        if (_resizeHandles.TryGetValue(nodeId, out var handles))
        {
            foreach (var handle in handles)
            {
                canvas.Children.Remove(handle);
            }
            _resizeHandles.Remove(nodeId);
        }
    }

    /// <summary>
    /// Removes all resize handles from the canvas.
    /// </summary>
    /// <param name="canvas">The canvas containing the handles.</param>
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
    /// Updates the position of resize handles for a node.
    /// </summary>
    /// <param name="node">The node whose handles need updating.</param>
    public void UpdateResizeHandlePositions(Node node)
    {
        if (!_resizeHandles.TryGetValue(node.Id, out var handles))
            return;

        var scale = _renderContext.Scale;
        var handleSize = 8 * scale;
        var (nodeWidth, nodeHeight) = _nodeVisualManager.GetNodeDimensions(node);
        var screenPos = _renderContext.CanvasToScreen(node.Position.X, node.Position.Y);
        var scaledWidth = nodeWidth * scale;
        var scaledHeight = nodeHeight * scale;

        foreach (var handle in handles)
        {
            if (handle.Tag is (Node _, ResizeHandlePosition position))
            {
                PositionResizeHandle(handle, screenPos, scaledWidth, scaledHeight, handleSize, position);
            }
        }
    }

    /// <summary>
    /// Creates a resize handle rectangle.
    /// </summary>
    private Rectangle CreateResizeHandle(double size, ThemeResources theme, Node node, ResizeHandlePosition position)
    {
        var cursor = position switch
        {
            ResizeHandlePosition.TopLeft or ResizeHandlePosition.BottomRight => StandardCursorType.TopLeftCorner,
            ResizeHandlePosition.TopRight or ResizeHandlePosition.BottomLeft => StandardCursorType.TopRightCorner,
            ResizeHandlePosition.Top or ResizeHandlePosition.Bottom => StandardCursorType.SizeNorthSouth,
            ResizeHandlePosition.Left or ResizeHandlePosition.Right => StandardCursorType.SizeWestEast,
            _ => StandardCursorType.Arrow
        };

        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = theme.NodeSelectedBorder,
            Stroke = Brushes.White,
            StrokeThickness = 1,
            Cursor = new Cursor(cursor),
            Tag = (node, position)
        };
    }

    /// <summary>
    /// Positions a resize handle on the canvas.
    /// </summary>
    private void PositionResizeHandle(
        Rectangle handle,
        AvaloniaPoint nodeScreenPos,
        double scaledWidth,
        double scaledHeight,
        double handleSize,
        ResizeHandlePosition position)
    {
        var halfHandle = handleSize / 2;

        var (left, top) = position switch
        {
            ResizeHandlePosition.TopLeft => (nodeScreenPos.X - halfHandle, nodeScreenPos.Y - halfHandle),
            ResizeHandlePosition.TopRight => (nodeScreenPos.X + scaledWidth - halfHandle, nodeScreenPos.Y - halfHandle),
            ResizeHandlePosition.BottomLeft => (nodeScreenPos.X - halfHandle, nodeScreenPos.Y + scaledHeight - halfHandle),
            ResizeHandlePosition.BottomRight => (nodeScreenPos.X + scaledWidth - halfHandle, nodeScreenPos.Y + scaledHeight - halfHandle),
            ResizeHandlePosition.Top => (nodeScreenPos.X + scaledWidth / 2 - halfHandle, nodeScreenPos.Y - halfHandle),
            ResizeHandlePosition.Bottom => (nodeScreenPos.X + scaledWidth / 2 - halfHandle, nodeScreenPos.Y + scaledHeight - halfHandle),
            ResizeHandlePosition.Left => (nodeScreenPos.X - halfHandle, nodeScreenPos.Y + scaledHeight / 2 - halfHandle),
            ResizeHandlePosition.Right => (nodeScreenPos.X + scaledWidth - halfHandle, nodeScreenPos.Y + scaledHeight / 2 - halfHandle),
            _ => (0.0, 0.0)
        };

        Canvas.SetLeft(handle, left);
        Canvas.SetTop(handle, top);
    }
}
