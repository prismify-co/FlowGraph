using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for resize handles (both node and shape).
/// </summary>
/// <remarks>
/// <para>
/// This processor has the HIGHEST priority because resize handles are small
/// targets that sit on top of their parent elements. Without priority, clicks
/// intended for resize handles would be captured by the parent node/shape.
/// </para>
/// <para>
/// <b>Key Design Decision:</b>
/// Resize handles for nodes and shapes both use <see cref="HitTargetType.ResizeHandle"/>,
/// distinguished by the <see cref="GraphHitTestResult.Target"/> type:
/// <list type="bullet">
/// <item>Node resize: check <see cref="GraphHitTestResult.ResizeHandleOwner"/></item>
/// <item>Shape resize: check for shape info in Target (future enhancement)</item>
/// </list>
/// </para>
/// </remarks>
public class ResizeHandleProcessor : InputProcessorBase
{
    public override HitTargetType HandledTypes => HitTargetType.ResizeHandle;
    
    public override int Priority => InputProcessorPriority.ResizeHandle;
    
    public override string Name => "ResizeHandleProcessor";
    
    public override InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerPressedEventArgs e)
    {
        // Resize blocked in read-only mode
        if (context.Settings.IsReadOnly)
            return InputProcessorResult.NotHandled;
        
        var handlePosition = hit.ResizeHandle;
        if (!handlePosition.HasValue)
            return InputProcessorResult.NotHandled;
        
        // Check what type of resize handle (node vs shape)
        if (hit.ResizeHandleOwner != null)
        {
            return HandleNodeResize(context, e, hit.ResizeHandleOwner, handlePosition.Value, hit.CanvasPosition);
        }
        
        // TODO: Add shape resize handling when GraphHitTestResult is extended
        // if (hit.Target is ShapeResizeHandleHitInfo shapeInfo)
        // {
        //     return HandleShapeResize(context, e, shapeInfo.Shape, handlePosition.Value, hit.CanvasPosition);
        // }
        
        return InputProcessorResult.NotHandled;
    }
    
    private static InputProcessorResult HandleNodeResize(
        InputStateContext context,
        PointerPressedEventArgs e,
        Node node,
        Core.Input.ResizeHandlePosition handlePosition,
        Core.Point canvasPos)
    {
        var graph = context.Graph;
        if (graph == null) return InputProcessorResult.NotHandled;
        
        // Convert to Rendering.ResizeHandlePosition (they should match)
        var renderPosition = ConvertToRenderingPosition(handlePosition);
        
        var avaloniaCanvasPos = new AvaloniaPoint(canvasPos.X, canvasPos.Y);
        var viewportPos = context.CanvasToViewport(avaloniaCanvasPos);
        
        var resizeState = new ResizingState(
            node,
            renderPosition,
            viewportPos,
            context.Settings,
            context.Viewport,
            context.GraphRenderer);
        
        CapturePointer(e, context.RootPanel);
        return InputProcessorResult.TransitionTo(resizeState);
    }
    
    private static Rendering.ResizeHandlePosition ConvertToRenderingPosition(
        Core.Input.ResizeHandlePosition corePosition)
    {
        return corePosition switch
        {
            Core.Input.ResizeHandlePosition.TopLeft => Rendering.ResizeHandlePosition.TopLeft,
            Core.Input.ResizeHandlePosition.TopCenter => Rendering.ResizeHandlePosition.Top,
            Core.Input.ResizeHandlePosition.TopRight => Rendering.ResizeHandlePosition.TopRight,
            Core.Input.ResizeHandlePosition.MiddleLeft => Rendering.ResizeHandlePosition.Left,
            Core.Input.ResizeHandlePosition.MiddleRight => Rendering.ResizeHandlePosition.Right,
            Core.Input.ResizeHandlePosition.BottomLeft => Rendering.ResizeHandlePosition.BottomLeft,
            Core.Input.ResizeHandlePosition.BottomCenter => Rendering.ResizeHandlePosition.Bottom,
            Core.Input.ResizeHandlePosition.BottomRight => Rendering.ResizeHandlePosition.BottomRight,
            _ => Rendering.ResizeHandlePosition.BottomRight
        };
    }
}
