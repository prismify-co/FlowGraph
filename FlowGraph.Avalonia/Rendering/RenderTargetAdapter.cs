using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Core.Coordinates;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Implementation of <see cref="IRenderTarget"/> that provides mode-aware temporary
/// visual rendering for interaction states.
/// 
/// <para>
/// This adapter automatically selects the correct container and coordinate space:
/// <list type="bullet">
/// <item>Visual Tree mode: MainCanvas with canvas coordinates (MatrixTransform handles viewport)</item>
/// <item>Direct Rendering mode: RootPanel with viewport coordinates (self-transformed)</item>
/// </list>
/// </para>
/// </summary>
public sealed class RenderTargetAdapter : IRenderTarget
{
    private readonly ViewportState _viewport;
    private readonly Panel? _rootPanel;
    private readonly Canvas? _mainCanvas;
    private readonly DirectCanvasRenderer? _directRenderer;

    /// <summary>
    /// Creates a new RenderTargetAdapter.
    /// </summary>
    public RenderTargetAdapter(
        ViewportState viewport,
        Panel? rootPanel,
        Canvas? mainCanvas,
        DirectCanvasRenderer? directRenderer)
    {
        _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
        _rootPanel = rootPanel;
        _mainCanvas = mainCanvas;
        _directRenderer = directRenderer;
    }

    /// <summary>
    /// Whether we're currently in Direct Rendering mode.
    /// </summary>
    public bool IsDirectRenderingMode => _directRenderer != null;

    /// <inheritdoc />
    public IConnectionPreviewHandle CreateConnectionPreview(
        CanvasPoint start,
        CanvasPoint end,
        IBrush stroke,
        double strokeThickness,
        double[]? dashArray = null)
    {
        var container = GetContainer();
        if (container == null)
            throw new InvalidOperationException("No container available for connection preview");

        return new ConnectionPreviewHandle(
            container,
            _viewport,
            IsDirectRenderingMode,
            start,
            end,
            stroke,
            strokeThickness,
            dashArray ?? [5, 3]);
    }

    /// <inheritdoc />
    public ISelectionBoxHandle CreateSelectionBox(
        CanvasRect bounds,
        IBrush? fill,
        IBrush stroke,
        double strokeThickness)
    {
        var container = GetContainer();
        if (container == null)
            throw new InvalidOperationException("No container available for selection box");

        return new SelectionBoxHandle(
            container,
            _viewport,
            IsDirectRenderingMode,
            bounds,
            fill,
            stroke,
            strokeThickness);
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        _directRenderer?.InvalidateVisual();
    }

    private Panel? GetContainer()
    {
        // In Direct Rendering mode, use RootPanel (untransformed)
        // In Visual Tree mode, use MainCanvas (transformed)
        if (IsDirectRenderingMode && _rootPanel != null)
            return _rootPanel;
        else
            return _mainCanvas;
    }
}

/// <summary>
/// Handle for a temporary connection preview line.
/// </summary>
internal sealed class ConnectionPreviewHandle : IConnectionPreviewHandle
{
    private readonly Panel _container;
    private readonly ViewportState _viewport;
    private readonly bool _isDirectRenderingMode;
    private readonly AvaloniaPath _path;
    private CanvasPoint _start;
    private CanvasPoint _end;
    private bool _disposed;

    public ConnectionPreviewHandle(
        Panel container,
        ViewportState viewport,
        bool isDirectRenderingMode,
        CanvasPoint start,
        CanvasPoint end,
        IBrush stroke,
        double strokeThickness,
        double[] dashArray)
    {
        _container = container;
        _viewport = viewport;
        _isDirectRenderingMode = isDirectRenderingMode;
        _start = start;
        _end = end;

        _path = new AvaloniaPath
        {
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            StrokeDashArray = new AvaloniaList<double>(dashArray),
            Opacity = 0.7,
            IsHitTestVisible = false
        };

        UpdateGeometry();
        _container.Children.Add(_path);
    }

    public IBrush Stroke
    {
        get => _path.Stroke!;
        set => _path.Stroke = value;
    }

    public double Opacity
    {
        get => _path.Opacity;
        set => _path.Opacity = value;
    }

    public void UpdateStart(CanvasPoint start)
    {
        _start = start;
        UpdateGeometry();
    }

    public void UpdateEnd(CanvasPoint end)
    {
        _end = end;
        UpdateGeometry();
    }

    public void Update(CanvasPoint start, CanvasPoint end)
    {
        _start = start;
        _end = end;
        UpdateGeometry();
    }

    public void SetValidTargetStyle(bool isValid)
    {
        // Could change appearance when hovering over valid target
        _path.Opacity = isValid ? 1.0 : 0.7;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _container.Children.Remove(_path);
    }

    private void UpdateGeometry()
    {
        Point startPt, endPt;

        if (_isDirectRenderingMode)
        {
            // Direct Rendering: container is RootPanel (untransformed)
            // Need to convert canvas coords to viewport coords
            startPt = _viewport.CanvasToViewport(new Point(_start.X, _start.Y));
            endPt = _viewport.CanvasToViewport(new Point(_end.X, _end.Y));
        }
        else
        {
            // Visual Tree: container is MainCanvas (transformed)
            // Use canvas coords directly - MatrixTransform handles viewport transform
            startPt = new Point(_start.X, _start.Y);
            endPt = new Point(_end.X, _end.Y);
        }

        // Create bezier curve geometry
        var geometry = CreateBezierGeometry(startPt, endPt);
        _path.Data = geometry;
    }

    private static PathGeometry CreateBezierGeometry(Point start, Point end)
    {
        // Calculate control points for smooth bezier curve
        var dx = Math.Abs(end.X - start.X);
        var controlOffset = Math.Max(50, dx * 0.5);

        var control1 = new Point(start.X + controlOffset, start.Y);
        var control2 = new Point(end.X - controlOffset, end.Y);

        var pathFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments
            {
                new BezierSegment
                {
                    Point1 = control1,
                    Point2 = control2,
                    Point3 = end
                }
            }
        };

        return new PathGeometry { Figures = new PathFigures { pathFigure } };
    }
}

/// <summary>
/// Handle for a temporary selection box.
/// </summary>
internal sealed class SelectionBoxHandle : ISelectionBoxHandle
{
    private readonly Panel _container;
    private readonly ViewportState _viewport;
    private readonly bool _isDirectRenderingMode;
    private readonly Rectangle _rectangle;
    private CanvasRect _bounds;
    private bool _disposed;

    public SelectionBoxHandle(
        Panel container,
        ViewportState viewport,
        bool isDirectRenderingMode,
        CanvasRect bounds,
        IBrush? fill,
        IBrush stroke,
        double strokeThickness)
    {
        _container = container;
        _viewport = viewport;
        _isDirectRenderingMode = isDirectRenderingMode;
        _bounds = bounds;

        _rectangle = new Rectangle
        {
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            IsHitTestVisible = false
        };

        UpdateLayout();
        _container.Children.Add(_rectangle);
    }

    public IBrush? Fill
    {
        get => _rectangle.Fill;
        set => _rectangle.Fill = value;
    }

    public IBrush Stroke
    {
        get => _rectangle.Stroke!;
        set => _rectangle.Stroke = value;
    }

    public void UpdateBounds(CanvasRect bounds)
    {
        _bounds = bounds;
        UpdateLayout();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _container.Children.Remove(_rectangle);
    }

    private void UpdateLayout()
    {
        double left, top, width, height;

        if (_isDirectRenderingMode)
        {
            // Direct Rendering: convert canvas rect to viewport coords
            var topLeft = _viewport.CanvasToViewport(new Point(_bounds.X, _bounds.Y));
            var bottomRight = _viewport.CanvasToViewport(
                new Point(_bounds.X + _bounds.Width, _bounds.Y + _bounds.Height));

            left = Math.Min(topLeft.X, bottomRight.X);
            top = Math.Min(topLeft.Y, bottomRight.Y);
            width = Math.Abs(bottomRight.X - topLeft.X);
            height = Math.Abs(bottomRight.Y - topLeft.Y);
        }
        else
        {
            // Visual Tree: use canvas coords directly
            left = _bounds.X;
            top = _bounds.Y;
            width = _bounds.Width;
            height = _bounds.Height;
        }

        Canvas.SetLeft(_rectangle, left);
        Canvas.SetTop(_rectangle, top);
        _rectangle.Width = width;
        _rectangle.Height = height;
    }
}
