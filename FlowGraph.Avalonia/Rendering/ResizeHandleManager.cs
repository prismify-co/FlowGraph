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

        // For resize handles, we want constant screen size regardless of zoom
        // Use InverseScale to counteract the MatrixTransform zoom
        var inverseScale = _renderContext.InverseScale;
        var handleSize = 8 * inverseScale;
        var (nodeWidth, nodeHeight) = _nodeVisualManager.GetNodeDimensions(node);
        var canvasPos = new AvaloniaPoint(node.Position.X, node.Position.Y);

        System.Diagnostics.Debug.WriteLine($"[ResizeHandles] Node={node.Id}: NodeDimensions=({nodeWidth:F0}x{nodeHeight:F0}), node.Width={node.Width}, node.Height={node.Height}");
        System.Diagnostics.Debug.WriteLine($"[ResizeHandles]   InverseScale={inverseScale:F2}, CanvasPos=({canvasPos.X:F0},{canvasPos.Y:F0})");
        System.Diagnostics.Debug.WriteLine($"[ResizeHandles]   Settings: NodeWidth={_renderContext.Settings.NodeWidth}, NodeHeight={_renderContext.Settings.NodeHeight}");

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
            PositionResizeHandle(handle, canvasPos, nodeWidth, nodeHeight, handleSize, position);

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
    /// Updates size and position of all tracked resize handles.
    /// Called on zoom changes to recalculate InverseScale-based sizing.
    /// </summary>
    public void UpdateAllResizeHandles()
    {
        // Use InverseScale for constant screen size
        var inverseScale = _renderContext.InverseScale;
        var handleSize = 8 * inverseScale;

        foreach (var (nodeId, handles) in _resizeHandles)
        {
            foreach (var handle in handles)
            {
                if (handle.Tag is (Node node, ResizeHandlePosition position))
                {
                    var (nodeWidth, nodeHeight) = _nodeVisualManager.GetNodeDimensions(node);
                    var canvasPos = new AvaloniaPoint(node.Position.X, node.Position.Y);

                    // Update handle size (changed with zoom)
                    handle.Width = handleSize;
                    handle.Height = handleSize;
                    PositionResizeHandle(handle, canvasPos, nodeWidth, nodeHeight, handleSize, position);
                }
            }
        }
    }

    /// <summary>
    /// Updates the position of resize handles for a node.
    /// </summary>
    /// <param name="node">The node whose handles need updating.</param>
    public void UpdateResizeHandlePositions(Node node)
    {
        if (!_resizeHandles.TryGetValue(node.Id, out var handles))
            return;

        // Use InverseScale for constant screen size
        var inverseScale = _renderContext.InverseScale;
        var handleSize = 8 * inverseScale;
        var (nodeWidth, nodeHeight) = _nodeVisualManager.GetNodeDimensions(node);
        var canvasPos = new AvaloniaPoint(node.Position.X, node.Position.Y);

        foreach (var handle in handles)
        {
            if (handle.Tag is (Node _, ResizeHandlePosition position))
            {
                // Update handle size (may have changed with zoom)
                handle.Width = handleSize;
                handle.Height = handleSize;
                PositionResizeHandle(handle, canvasPos, nodeWidth, nodeHeight, handleSize, position);
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
        AvaloniaPoint nodeCanvasPos,
        double nodeWidth,
        double nodeHeight,
        double handleSize,
        ResizeHandlePosition position)
    {
        var halfHandle = handleSize / 2;

        var (left, top) = position switch
        {
            ResizeHandlePosition.TopLeft => (nodeCanvasPos.X - halfHandle, nodeCanvasPos.Y - halfHandle),
            ResizeHandlePosition.TopRight => (nodeCanvasPos.X + nodeWidth - halfHandle, nodeCanvasPos.Y - halfHandle),
            ResizeHandlePosition.BottomLeft => (nodeCanvasPos.X - halfHandle, nodeCanvasPos.Y + nodeHeight - halfHandle),
            ResizeHandlePosition.BottomRight => (nodeCanvasPos.X + nodeWidth - halfHandle, nodeCanvasPos.Y + nodeHeight - halfHandle),
            ResizeHandlePosition.Top => (nodeCanvasPos.X + nodeWidth / 2 - halfHandle, nodeCanvasPos.Y - halfHandle),
            ResizeHandlePosition.Bottom => (nodeCanvasPos.X + nodeWidth / 2 - halfHandle, nodeCanvasPos.Y + nodeHeight - halfHandle),
            ResizeHandlePosition.Left => (nodeCanvasPos.X - halfHandle, nodeCanvasPos.Y + nodeHeight / 2 - halfHandle),
            ResizeHandlePosition.Right => (nodeCanvasPos.X + nodeWidth - halfHandle, nodeCanvasPos.Y + nodeHeight / 2 - halfHandle),
            _ => (0.0, 0.0)
        };

        Canvas.SetLeft(handle, left);
        Canvas.SetTop(handle, top);
    }
}
