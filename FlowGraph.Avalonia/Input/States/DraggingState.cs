using Avalonia;
using Avalonia.Input;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for dragging selected nodes.
/// </summary>
public class DraggingState : InputStateBase
{
    private readonly AvaloniaPoint _dragStartCanvas;
    private readonly Dictionary<string, Core.Point> _startPositions;
    private readonly FlowCanvasSettings _settings;
    private readonly List<string> _draggedNodeIds;
    private readonly List<Node> _draggedNodes;
    private bool _dragStartRaised;

    public override string Name => "Dragging";

    public DraggingState(Graph graph, AvaloniaPoint screenPosition, ViewportState viewport, FlowCanvasSettings settings)
    {
        _dragStartCanvas = viewport.ScreenToCanvas(screenPosition);
        _startPositions = new Dictionary<string, Core.Point>();
        _settings = settings;
        _draggedNodeIds = new List<string>();
        _draggedNodes = new List<Node>();
        _dragStartRaised = false;

        // Collect all nodes to drag: selected AND draggable nodes + children of selected groups
        var nodesToDrag = new HashSet<string>();
        
        foreach (var node in graph.Nodes.Where(n => n.IsSelected && n.IsDraggable))
        {
            nodesToDrag.Add(node.Id);
            
            if (node.IsGroup)
            {
                // Include all children of dragged groups (even if not individually draggable)
                foreach (var child in graph.GetGroupChildrenRecursive(node.Id))
                {
                    nodesToDrag.Add(child.Id);
                }
            }
        }

        foreach (var nodeId in nodesToDrag)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.IsDragging = true;
                _startPositions[node.Id] = node.Position;
                _draggedNodeIds.Add(node.Id);
                _draggedNodes.Add(node);
            }
        }
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);
        
        // Raise drag start event
        var startPos = new Core.Point(_dragStartCanvas.X, _dragStartCanvas.Y);
        context.RaiseNodeDragStart(_draggedNodes, startPos);
        _dragStartRaised = true;
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        var graph = context.Graph;
        if (graph == null) return StateTransitionResult.Unhandled();

        var currentCanvas = context.ScreenToCanvas(GetPosition(context, e));
        var deltaX = currentCanvas.X - _dragStartCanvas.X;
        var deltaY = currentCanvas.Y - _dragStartCanvas.Y;

        foreach (var (nodeId, startPos) in _startPositions)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                var newX = startPos.X + deltaX;
                var newY = startPos.Y + deltaY;

                if (_settings.SnapToGrid)
                {
                    var snapSize = _settings.EffectiveSnapGridSize;
                    newX = Math.Round(newX / snapSize) * snapSize;
                    newY = Math.Round(newY / snapSize) * snapSize;
                }

                node.Position = new Core.Point(newX, newY);
            }
        }

        // Raise dragging event for edge routing and other subscribers
        context.RaiseNodesDragging(_draggedNodeIds);

        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        var graph = context.Graph;
        if (graph == null)
        {
            ReleasePointer(e);
            if (_dragStartRaised)
            {
                context.RaiseNodeDragStop(_draggedNodes, cancelled: true);
            }
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        var newPositions = new Dictionary<string, Core.Point>();
        foreach (var (nodeId, _) in _startPositions)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.IsDragging = false;
                newPositions[node.Id] = node.Position;
            }
        }

        // Check if positions actually changed
        bool positionsChanged = _startPositions.Any(kvp =>
            newPositions.TryGetValue(kvp.Key, out var newPos) &&
            (Math.Abs(kvp.Value.X - newPos.X) > 0.1 || Math.Abs(kvp.Value.Y - newPos.Y) > 0.1));

        if (positionsChanged)
        {
            context.RaiseNodesDragged(
                new Dictionary<string, Core.Point>(_startPositions),
                newPositions);
        }

        // Raise drag stop event
        if (_dragStartRaised)
        {
            context.RaiseNodeDragStop(_draggedNodes, cancelled: false);
        }

        ReleasePointer(e);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel drag - restore original positions
            var graph = context.Graph;
            if (graph != null)
            {
                foreach (var (nodeId, startPos) in _startPositions)
                {
                    var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (node != null)
                    {
                        node.IsDragging = false;
                        node.Position = startPos;
                    }
                }
            }

            // Raise drag stop with cancelled = true
            if (_dragStartRaised)
            {
                context.RaiseNodeDragStop(_draggedNodes, cancelled: true);
            }

            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }
}
