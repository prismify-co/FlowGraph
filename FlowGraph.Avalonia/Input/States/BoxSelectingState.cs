using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for box selection of multiple elements (nodes and shapes).
/// </summary>
public class BoxSelectingState : InputStateBase
{
    private readonly AvaloniaPoint _startCanvas;
    private readonly AvaloniaPoint _startViewport; // Store viewport coords to avoid recalculation during pan
    private AvaloniaPoint _endCanvas;
    private AvaloniaPoint _endViewport;
    private readonly Rectangle _selectionBox;
    private Panel? _container;
    private readonly FlowCanvasSettings _settings;
    private readonly ViewportState _viewport;
    private bool _isDirectRenderingMode;

    public override string Name => "BoxSelecting";

    public BoxSelectingState(AvaloniaPoint startCanvasPoint, FlowCanvasSettings settings, ViewportState viewport, ThemeResources? theme = null)
    {
        _startCanvas = startCanvasPoint;
        _startViewport = viewport.CanvasToViewport(startCanvasPoint); // Cache viewport coords
        _endCanvas = startCanvasPoint;
        _endViewport = _startViewport; // Initialize to start position
        _settings = settings;
        _viewport = viewport;

        System.Diagnostics.Debug.WriteLine($"[BoxSelect] Created: StartCanvas=({_startCanvas.X:F1},{_startCanvas.Y:F1}) StartViewport=({_startViewport.X:F1},{_startViewport.Y:F1}) Viewport: Offset=({viewport.OffsetX:F1},{viewport.OffsetY:F1}) Zoom={viewport.Zoom:F2}");


        _selectionBox = new Rectangle
        {
            Stroke = theme?.SelectionBoxStroke ?? new SolidColorBrush(Color.Parse("#0078D4")),
            StrokeThickness = 1,
            Fill = theme?.SelectionBoxFill ?? new SolidColorBrush(Color.FromArgb(40, 0, 120, 212)),
            IsHitTestVisible = false,
            // Required for Margin-based positioning in Panel (Direct Rendering mode)
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top
        };
    }

    public override void Enter(InputStateContext context)
    {
        // In Direct Rendering mode, add to RootPanel (untransformed) and use viewport coordinates
        // In Visual Tree mode, add to MainCanvas (transformed) and use canvas coordinates
        _isDirectRenderingMode = context.DirectRenderer != null;

        if (_isDirectRenderingMode && context.RootPanel != null)
        {
            _container = context.RootPanel;
        }
        else
        {
            _container = context.MainCanvas;
        }

        _container?.Children.Add(_selectionBox);
        UpdateSelectionBoxVisual();
    }

    public override void Exit(InputStateContext context)
    {
        _container?.Children.Remove(_selectionBox);
        _container = null;
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Get both viewport and canvas positions using typed coordinate system
        var typedViewport = GetTypedViewportPosition(context, e);
        var typedCanvas = GetTypedCanvasPosition(context, e);

        _endViewport = new AvaloniaPoint(typedViewport.X, typedViewport.Y);
        _endCanvas = new AvaloniaPoint(typedCanvas.X, typedCanvas.Y);

        UpdateSelectionBoxVisual();
        UpdateSelection(context, e.KeyModifiers.HasFlag(KeyModifiers.Control));

        e.Handled = true;
        return StateTransitionResult.Stay();
    }

    public override StateTransitionResult HandlePointerReleased(InputStateContext context, PointerReleasedEventArgs e)
    {
        ReleasePointer(e);
        e.Handled = true;
        return StateTransitionResult.TransitionTo(IdleState.Instance);
    }

    public override StateTransitionResult HandleKeyDown(InputStateContext context, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            return StateTransitionResult.TransitionTo(IdleState.Instance);
        }
        return StateTransitionResult.Unhandled();
    }

    private void UpdateSelectionBoxVisual()
    {
        double left, top, width, height;

        if (_isDirectRenderingMode)
        {
            // Direct Rendering mode: container is RootPanel (untransformed)
            // Use cached viewport coordinates (doesn't change during pan)
            left = Math.Min(_startViewport.X, _endViewport.X);
            top = Math.Min(_startViewport.Y, _endViewport.Y);
            width = Math.Abs(_endViewport.X - _startViewport.X);
            height = Math.Abs(_endViewport.Y - _startViewport.Y);

            // RootPanel is a Panel, not Canvas, so use Margin instead of Canvas.SetLeft/SetTop
            _selectionBox.Margin = new Thickness(left, top, 0, 0);
        }
        else
        {
            // Visual Tree mode: container is MainCanvas (transformed)
            // Use canvas coordinates - MatrixTransform will handle viewport transform
            left = Math.Min(_startCanvas.X, _endCanvas.X);
            top = Math.Min(_startCanvas.Y, _endCanvas.Y);
            width = Math.Abs(_endCanvas.X - _startCanvas.X);
            height = Math.Abs(_endCanvas.Y - _startCanvas.Y);

            // MainCanvas is a Canvas, so use Canvas attached properties
            Canvas.SetLeft(_selectionBox, left);
            Canvas.SetTop(_selectionBox, top);
        }

        _selectionBox.Width = width;
        _selectionBox.Height = height;
    }

    private void UpdateSelection(InputStateContext context, bool addToSelection)
    {
        var graph = context.Graph;
        if (graph == null) return;

        var selectionRect = new Rect(
            Math.Min(_startCanvas.X, _endCanvas.X),
            Math.Min(_startCanvas.Y, _endCanvas.Y),
            Math.Abs(_endCanvas.X - _startCanvas.X),
            Math.Abs(_endCanvas.Y - _startCanvas.Y)
        );

        // Select nodes
        foreach (var node in graph.Elements.Nodes)
        {
            // Skip non-selectable nodes
            if (!node.IsSelectable)
            {
                continue;
            }

            var nodeWidth = node.Width ?? _settings.NodeWidth;
            var nodeHeight = node.Height ?? _settings.NodeHeight;
            var nodeRect = new Rect(
                node.Position.X,
                node.Position.Y,
                nodeWidth,
                nodeHeight
            );

            bool shouldSelect = _settings.SelectionMode == SelectionMode.Full
                ? selectionRect.Contains(nodeRect)
                : selectionRect.Intersects(nodeRect);

            if (addToSelection)
            {
                // OPTIMIZED: Only change if needed (avoids property change notifications)
                if (shouldSelect && !node.IsSelected)
                    node.IsSelected = true;
            }
            else
            {
                // OPTIMIZED: Only change if different (avoids property change notifications)
                if (node.IsSelected != shouldSelect)
                    node.IsSelected = shouldSelect;
            }
        }

        // Select shapes (comments, sticky notes, etc.)
        foreach (var shape in graph.Elements.Shapes)
        {
            // Skip non-selectable or invisible shapes
            if (!shape.IsSelectable || !shape.IsVisible)
            {
                continue;
            }

            var bounds = shape.GetBounds();
            var shapeRect = new Rect(
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height
            );

            bool shouldSelect = _settings.SelectionMode == SelectionMode.Full
                ? selectionRect.Contains(shapeRect)
                : selectionRect.Intersects(shapeRect);

            if (addToSelection)
            {
                // OPTIMIZED: Only change if needed (avoids property change notifications)
                if (shouldSelect && !shape.IsSelected)
                {
                    shape.IsSelected = true;
                    context.ShapeVisualManager?.UpdateSelection(shape.Id, true);
                }
            }
            else
            {
                // OPTIMIZED: Only change if different (avoids property change notifications)
                if (shape.IsSelected != shouldSelect)
                {
                    shape.IsSelected = shouldSelect;
                    context.ShapeVisualManager?.UpdateSelection(shape.Id, shouldSelect);
                }
            }
        }

        context.RaiseBoxSelectionChanged(selectionRect);
    }
}
