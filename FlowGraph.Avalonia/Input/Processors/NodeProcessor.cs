using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for nodes (selection, dragging, label editing).
/// </summary>
/// <remarks>
/// <para>
/// This processor handles all node interactions:
/// <list type="bullet">
/// <item>Single click: select (with Ctrl for multi-select)</item>
/// <item>Double click: edit label (if enabled)</item>
/// <item>Drag: move selected nodes</item>
/// <item>Group collapse button: toggle collapse</item>
/// </list>
/// </para>
/// </remarks>
public class NodeProcessor : InputProcessorBase
{
    public override HitTargetType HandledTypes => HitTargetType.Node | HitTargetType.Group;
    
    public override int Priority => InputProcessorPriority.Node;
    
    public override string Name => "NodeProcessor";
    
    public override InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerPressedEventArgs e)
    {
        var node = hit.Node;
        if (node == null) return InputProcessorResult.NotHandled;
        
        var graph = context.Graph;
        if (graph == null) return InputProcessorResult.NotHandled;
        
        var isReadOnly = context.Settings.IsReadOnly;
        var canvasPos = new AvaloniaPoint(hit.CanvasPosition.X, hit.CanvasPosition.Y);
        
        // Check for group collapse button click (always allowed, even in read-only mode)
        if (node.IsGroup)
        {
            if (TryHandleGroupCollapseButton(context, node, canvasPos))
            {
                return InputProcessorResult.HandledStay;
            }
        }
        
        // Double-click handling
        if (IsDoubleClick(e))
        {
            return HandleDoubleClick(context, node, canvasPos, isReadOnly);
        }
        
        // Handle selection
        HandleSelection(context, graph, node, IsCtrlHeld(e));
        
        // Start drag if allowed
        if (!isReadOnly && node.IsDraggable && node.IsSelected)
        {
            var viewportPos = context.CanvasToViewport(canvasPos);
            var dragState = new DraggingState(
                graph, 
                viewportPos, 
                canvasPos, 
                context.Viewport, 
                context.Settings,
                e.KeyModifiers);
            
            CapturePointer(e, context.RootPanel);
            return InputProcessorResult.TransitionTo(dragState);
        }
        
        return InputProcessorResult.HandledStay;
    }
    
    #region Private Helpers
    
    private static bool TryHandleGroupCollapseButton(
        InputStateContext context,
        Node node,
        AvaloniaPoint canvasPos)
    {
        // Calculate click position relative to the node
        var relativeX = canvasPos.X - node.Position.X;
        var relativeY = canvasPos.Y - node.Position.Y;
        
        // Button area uses the same constants as CanvasRenderModel
        var buttonX = CanvasRenderModel.GroupHeaderMarginX;
        var buttonY = CanvasRenderModel.GroupHeaderMarginY;
        var buttonSize = CanvasRenderModel.GroupCollapseButtonSize;
        
        // Add extra padding for easier clicking
        var hitPadding = 4.0;
        var hitLeft = buttonX - hitPadding;
        var hitTop = buttonY - hitPadding;
        var hitRight = buttonX + buttonSize + hitPadding;
        var hitBottom = buttonY + buttonSize + hitPadding;
        
        if (relativeX >= hitLeft && relativeX < hitRight &&
            relativeY >= hitTop && relativeY < hitBottom)
        {
            context.RaiseGroupCollapseToggle(node.Id);
            return true;
        }
        
        return false;
    }
    
    private static InputProcessorResult HandleDoubleClick(
        InputStateContext context,
        Node node,
        AvaloniaPoint canvasPos,
        bool isReadOnly)
    {
        if (node.IsGroup)
        {
            if (!isReadOnly && context.Settings.EnableGroupLabelEditing)
            {
                var screenPos = context.CanvasToViewport(canvasPos);
                context.RaiseNodeLabelEditRequested(node, screenPos);
            }
            else
            {
                // Collapse toggle always allowed
                context.RaiseGroupCollapseToggle(node.Id);
            }
        }
        else if (!isReadOnly && context.Settings.EnableNodeLabelEditing)
        {
            var screenPos = context.CanvasToViewport(canvasPos);
            context.RaiseNodeLabelEditRequested(node, screenPos);
        }
        
        return InputProcessorResult.HandledStay;
    }
    
    private static void HandleSelection(
        InputStateContext context,
        Graph graph,
        Node node,
        bool ctrlHeld)
    {
        if (!node.IsSelectable) return;
        
        if (!ctrlHeld && !node.IsSelected)
        {
            // Single select: deselect others
            foreach (var n in graph.Elements.Nodes.Where(n => n.IsSelected && n.Id != node.Id))
            {
                n.IsSelected = false;
            }
            node.IsSelected = true;
        }
        else if (ctrlHeld)
        {
            // Toggle selection
            node.IsSelected = !node.IsSelected;
        }
    }
    
    #endregion
}
