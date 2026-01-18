using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core;
using FlowGraph.Core.Elements.Shapes;
using System.Diagnostics;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for dragging selected shapes (CommentElement, etc.).
/// </summary>
public class DraggingShapesState : InputStateBase
{
    private readonly AvaloniaPoint _dragStartScreen;
    private readonly AvaloniaPoint _dragStartCanvas;
    private readonly Dictionary<string, Core.Point> _startPositions;
    private readonly List<ShapeElement> _draggedShapes;
    private readonly FlowCanvasSettings _settings;
    private readonly double _effectiveDragThreshold;
    private bool _dragThresholdMet;

    // Base minimum distance (in screen pixels) before drag actually starts
    private const double BaseDragThreshold = 4.0;
    // At low zoom, increase drag threshold to prevent accidental drags
    private const double MaxDragThreshold = 15.0;

    public override string Name => "DraggingShapes";

    public DraggingShapesState(Graph graph, AvaloniaPoint viewportPosition, AvaloniaPoint canvasPosition, ViewportState viewport, FlowCanvasSettings settings)
    {
        _dragStartScreen = viewportPosition;
        _dragStartCanvas = canvasPosition;
        _settings = settings;

        // Scale drag threshold inversely with zoom
        _effectiveDragThreshold = Math.Min(BaseDragThreshold / viewport.Zoom, MaxDragThreshold);
        _startPositions = new Dictionary<string, Core.Point>();
        _draggedShapes = new List<ShapeElement>();
        _dragThresholdMet = false;

        // Collect all selected shapes
        foreach (var shape in graph.Elements.Shapes.Where(s => s.IsSelected))
        {
            _startPositions[shape.Id] = shape.Position;
            _draggedShapes.Add(shape);
        }

        Debug.WriteLine($"[DraggingShapesState] Created with {_draggedShapes.Count} shapes, threshold={_effectiveDragThreshold:F1}px");
    }

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);
        Debug.WriteLine($"[DraggingShapesState] Enter - dragging {_draggedShapes.Count} shapes");
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        if (_draggedShapes.Count == 0)
            return StateTransitionResult.TransitionTo(IdleState.Instance);

        var viewportPos = GetTypedViewportPosition(context, e);
        var currentScreen = ToAvaloniaPoint(viewportPos);

        // Check if we've met the drag threshold
        if (!_dragThresholdMet)
        {
            var screenDelta = currentScreen - _dragStartScreen;
            var screenDistance = Math.Sqrt(screenDelta.X * screenDelta.X + screenDelta.Y * screenDelta.Y);

            if (screenDistance < _effectiveDragThreshold)
            {
                return StateTransitionResult.Stay();
            }

            _dragThresholdMet = true;
            Debug.WriteLine($"[DraggingShapesState] Drag threshold met at {screenDistance:F1}px");
        }

        // Calculate delta in canvas coordinates
        var canvasPos = GetTypedCanvasPosition(context, e);
        var currentCanvas = ToAvaloniaPoint(canvasPos);
        var deltaX = currentCanvas.X - _dragStartCanvas.X;
        var deltaY = currentCanvas.Y - _dragStartCanvas.Y;

        // Move all dragged shapes
        foreach (var shape in _draggedShapes)
        {
            if (_startPositions.TryGetValue(shape.Id, out var startPos))
            {
                var newX = startPos.X + deltaX;
                var newY = startPos.Y + deltaY;

                // Apply grid snapping if enabled
                if (_settings.SnapToGrid)
                {
                    var snapSize = _settings.EffectiveSnapGridSize;
                    newX = Math.Round(newX / snapSize) * snapSize;
                    newY = Math.Round(newY / snapSize) * snapSize;
                }

                shape.Position = new Core.Point(newX, newY);

                // Update visual position directly
                var visual = context.ShapeVisualManager?.GetVisual(shape.Id);
                if (visual != null)
                {
                    Canvas.SetLeft(visual, newX);
                    Canvas.SetTop(visual, newY);
                }
            }
        }

        // Update resize handle positions to follow the dragged shapes
        context.ShapeVisualManager?.UpdateResizeHandlePositions();

        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        Debug.WriteLine($"[DraggingShapesState] PointerReleased - returning to Idle");
        ReleasePointer(e);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel drag - restore original positions
            foreach (var shape in _draggedShapes)
            {
                if (_startPositions.TryGetValue(shape.Id, out var startPos))
                {
                    shape.Position = startPos;

                    var visual = context.ShapeVisualManager?.GetVisual(shape.Id);
                    if (visual != null)
                    {
                        Canvas.SetLeft(visual, startPos.X);
                        Canvas.SetTop(visual, startPos.Y);
                    }
                }
            }

            // Update resize handle positions after restoring original positions
            context.ShapeVisualManager?.UpdateResizeHandlePositions();

            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    public override void Exit(InputStateContext context)
    {
        base.Exit(context);
        Debug.WriteLine($"[DraggingShapesState] Exit");
    }
}
