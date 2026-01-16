using Avalonia;
using Avalonia.Input;
using FlowGraph.Core;
using System.Diagnostics;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for dragging selected nodes.
/// </summary>
public class DraggingState : InputStateBase
{
    private readonly AvaloniaPoint _dragStartScreen;
    private readonly AvaloniaPoint _dragStartCanvas;
    private readonly Dictionary<string, Core.Point> _startPositions;
    private readonly Dictionary<string, Node> _nodeById; // OPTIMIZATION: O(1) lookup instead of O(n) FirstOrDefault
    private readonly FlowCanvasSettings _settings;
    private readonly List<string> _draggedNodeIds;
    private readonly List<Node> _draggedNodes;
    private readonly double _effectiveDragThreshold;
    private bool _dragStartRaised;
    private bool _dragThresholdMet;

    // Base minimum distance (in screen pixels) before drag actually starts
    private const double BaseDragThreshold = 4.0;
    // At low zoom, increase drag threshold to prevent accidental drags when clicking tiny nodes
    private const double MaxDragThreshold = 15.0;

    public override string Name => "Dragging";

    public DraggingState(Graph graph, AvaloniaPoint screenPosition, ViewportState viewport, FlowCanvasSettings settings)
    {
        _dragStartScreen = screenPosition;
        _dragStartCanvas = viewport.ScreenToCanvas(screenPosition);

        // Scale drag threshold inversely with zoom: at zoom 0.30, threshold = min(4/0.30, 15) = 13.3 pixels
        // This prevents accidental drags when clicking on tiny zoomed-out nodes
        _effectiveDragThreshold = Math.Min(BaseDragThreshold / viewport.Zoom, MaxDragThreshold);
        _startPositions = new Dictionary<string, Core.Point>();
        _nodeById = new Dictionary<string, Node>();
        _settings = settings;
        _draggedNodeIds = new List<string>();
        _draggedNodes = new List<Node>();
        _dragStartRaised = false;
        _dragThresholdMet = false;

        // Collect all nodes to drag: selected AND draggable nodes + children of selected groups
        var nodesToDrag = new HashSet<string>();

        foreach (var node in graph.Elements.Nodes.Where(n => n.IsSelected && n.IsDraggable))
        {
            nodesToDrag.Add(node.Id);
            _nodeById[node.Id] = node; // Cache node reference

            if (node.IsGroup)
            {
                // Include all children of dragged groups (even if not individually draggable)
                foreach (var child in graph.GetGroupChildrenRecursive(node.Id))
                {
                    nodesToDrag.Add(child.Id);
                    _nodeById[child.Id] = child; // Cache node reference
                }
            }
        }

        foreach (var nodeId in nodesToDrag)
        {
            if (_nodeById.TryGetValue(nodeId, out var node))
            {
                _startPositions[node.Id] = node.Position;
                _draggedNodeIds.Add(node.Id);
                _draggedNodes.Add(node);
            }
        }
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);
        // Don't raise drag start until threshold is met
    }

    private static long _dragMoveCount = 0;
    private static long _totalDragMoveMs = 0;

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var graph = context.Graph;
        if (graph == null) return StateTransitionResult.Unhandled();

        var currentScreen = GetScreenPosition(context, e);

        // Check if we've met the drag threshold
        if (!_dragThresholdMet)
        {
            var distX = currentScreen.X - _dragStartScreen.X;
            var distY = currentScreen.Y - _dragStartScreen.Y;
            var distance = Math.Sqrt(distX * distX + distY * distY);

            if (distance < _effectiveDragThreshold)
            {
                // Haven't moved enough yet - don't start dragging
                return StateTransitionResult.Stay();
            }

            // Threshold met - start actual drag
            _dragThresholdMet = true;

            // Set IsDragging on all nodes now
            foreach (var node in _draggedNodes)
            {
                node.IsDragging = true;
            }

            // Raise drag start event
            var startPos = new Core.Point(_dragStartCanvas.X, _dragStartCanvas.Y);
            context.RaiseNodeDragStart(_draggedNodes, startPos);
            _dragStartRaised = true;

            // Notify providers that drag has started
            context.SnapProvider?.OnDragStart(_draggedNodes, startPos);
            context.CollisionProvider?.OnDragStart(_draggedNodes, startPos);
        }

        // AutoPan: pan viewport when dragging near edges
        if (_settings.EnableAutoPan && context.RootPanel != null)
        {
            var viewBounds = context.RootPanel.Bounds;
            var edgeDist = _settings.AutoPanEdgeDistance;
            var panSpeed = _settings.AutoPanSpeed;

            double panX = 0, panY = 0;

            if (currentScreen.X < edgeDist)
                panX = panSpeed;
            else if (currentScreen.X > viewBounds.Width - edgeDist)
                panX = -panSpeed;

            if (currentScreen.Y < edgeDist)
                panY = panSpeed;
            else if (currentScreen.Y > viewBounds.Height - edgeDist)
                panY = -panSpeed;

            if (panX != 0 || panY != 0)
            {
                context.Viewport.Pan(panX, panY);
                context.ApplyViewportTransform();
            }
        }

        var currentCanvas = context.ScreenToCanvas(currentScreen);
        var deltaX = currentCanvas.X - _dragStartCanvas.X;
        var deltaY = currentCanvas.Y - _dragStartCanvas.Y;

        // OPTIMIZED: Use cached node references instead of O(n) FirstOrDefault lookups
        var posUpdateSw = System.Diagnostics.Stopwatch.StartNew();

        // Calculate proposed position for primary node (used for provider queries)
        Core.Point? snapOffset = null;
        Core.Point? collisionOffset = null;

        if (_startPositions.Count > 0)
        {
            // Use the first node's position as the reference
            var firstEntry = _startPositions.First();
            var proposedX = firstEntry.Value.X + deltaX;
            var proposedY = firstEntry.Value.Y + deltaY;

            if (_settings.SnapToGrid)
            {
                var snapSize = _settings.EffectiveSnapGridSize;
                proposedX = Math.Round(proposedX / snapSize) * snapSize;
                proposedY = Math.Round(proposedY / snapSize) * snapSize;
            }

            var proposedPosition = new Core.Point(proposedX, proposedY);

            // Query snap provider first (helper lines, guides)
            if (context.SnapProvider != null)
            {
                snapOffset = context.SnapProvider.GetSnapOffset(_draggedNodes, proposedPosition);
            }

            // Query collision provider second (applied after snap)
            // Pass the position WITH snap applied so collision sees final intended position
            if (context.CollisionProvider != null)
            {
                var positionAfterSnap = snapOffset.HasValue
                    ? new Core.Point(proposedPosition.X + snapOffset.Value.X, proposedPosition.Y + snapOffset.Value.Y)
                    : proposedPosition;
                collisionOffset = context.CollisionProvider.GetCollisionOffset(_draggedNodes, positionAfterSnap);
            }
        }

        foreach (var (nodeId, startPos) in _startPositions)
        {
            if (_nodeById.TryGetValue(nodeId, out var node))
            {
                var newX = startPos.X + deltaX;
                var newY = startPos.Y + deltaY;

                if (_settings.SnapToGrid)
                {
                    var snapSize = _settings.EffectiveSnapGridSize;
                    newX = Math.Round(newX / snapSize) * snapSize;
                    newY = Math.Round(newY / snapSize) * snapSize;
                }

                // Apply snap offset from provider (helper lines, guides, etc.)
                if (snapOffset.HasValue)
                {
                    newX += snapOffset.Value.X;
                    newY += snapOffset.Value.Y;
                }

                // Apply collision offset (blocking/push to prevent overlap)
                if (collisionOffset.HasValue)
                {
                    newX += collisionOffset.Value.X;
                    newY += collisionOffset.Value.Y;
                }

                node.Position = new Core.Point(newX, newY);
            }
        }
        posUpdateSw.Stop();
        var posUpdateMs = posUpdateSw.ElapsedMilliseconds;

        // Raise dragging event for edge routing and other subscribers
        var routingSw = System.Diagnostics.Stopwatch.StartNew();
        context.RaiseNodesDragging(_draggedNodeIds);
        routingSw.Stop();
        var routingMs = routingSw.ElapsedMilliseconds;

        sw.Stop();
        _dragMoveCount++;
        _totalDragMoveMs += sw.ElapsedMilliseconds;
        if (_dragMoveCount % 60 == 0)
        {
            Debug.WriteLine($"[DragMove] last60avg={_totalDragMoveMs}ms | Nodes={_startPositions.Count}, PosUpdate={posUpdateMs}ms, Routing={routingMs}ms");
            _totalDragMoveMs = 0;
        }

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
                context.SnapProvider?.OnDragEnd(_draggedNodes, cancelled: true);
                context.CollisionProvider?.OnDragEnd(_draggedNodes, cancelled: true);
            }
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        // If threshold was never met, this was just a click - don't record any position changes
        if (!_dragThresholdMet)
        {
            ReleasePointer(e);
            e.Handled = true;
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }

        var newPositions = new Dictionary<string, Core.Point>();
        // OPTIMIZED: Use cached node references instead of O(n) FirstOrDefault lookups
        foreach (var (nodeId, _) in _startPositions)
        {
            if (_nodeById.TryGetValue(nodeId, out var node))
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
            context.SnapProvider?.OnDragEnd(_draggedNodes, cancelled: false);
            context.CollisionProvider?.OnDragEnd(_draggedNodes, cancelled: false);
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
            // OPTIMIZED: Use cached node references instead of O(n) FirstOrDefault lookups
            foreach (var (nodeId, startPos) in _startPositions)
            {
                if (_nodeById.TryGetValue(nodeId, out var node))
                {
                    node.IsDragging = false;
                    node.Position = startPos;
                }
            }

            // Raise drag stop with cancelled = true
            if (_dragStartRaised)
            {
                context.RaiseNodeDragStop(_draggedNodes, cancelled: true);
                context.SnapProvider?.OnDragEnd(_draggedNodes, cancelled: true);
                context.CollisionProvider?.OnDragEnd(_draggedNodes, cancelled: true);
            }

            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }
}
