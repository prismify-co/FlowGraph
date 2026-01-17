using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.States;

/// <summary>
/// State for box selection of multiple nodes.
/// </summary>
public class BoxSelectingState : InputStateBase
{
    private readonly AvaloniaPoint _startCanvas;
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
        _endCanvas = startCanvasPoint;
        _endViewport = viewport.CanvasToViewport(startCanvasPoint);
        _settings = settings;
        _viewport = viewport;

        _selectionBox = new Rectangle
        {
            Stroke = theme?.SelectionBoxStroke ?? new SolidColorBrush(Color.Parse("#0078D4")),
            StrokeThickness = 1,
            Fill = theme?.SelectionBoxFill ?? new SolidColorBrush(Color.FromArgb(40, 0, 120, 212)),
            IsHitTestVisible = false
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
            // Use viewport coordinates directly
            var startViewport = _viewport.CanvasToViewport(_startCanvas);
            left = Math.Min(startViewport.X, _endViewport.X);
            top = Math.Min(startViewport.Y, _endViewport.Y);
            width = Math.Abs(_endViewport.X - startViewport.X);
            height = Math.Abs(_endViewport.Y - startViewport.Y);
        }
        else
        {
            // Visual Tree mode: container is MainCanvas (transformed)
            // Use canvas coordinates - MatrixTransform will handle viewport transform
            left = Math.Min(_startCanvas.X, _endCanvas.X);
            top = Math.Min(_startCanvas.Y, _endCanvas.Y);
            width = Math.Abs(_endCanvas.X - _startCanvas.X);
            height = Math.Abs(_endCanvas.Y - _startCanvas.Y);
        }

        Canvas.SetLeft(_selectionBox, left);
        Canvas.SetTop(_selectionBox, top);
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

        context.RaiseBoxSelectionChanged(selectionRect);
    }
}
