using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// Background pattern variants for the flow canvas.
/// </summary>
public enum BackgroundVariant
{
    /// <summary>
    /// Grid of dots (default).
    /// </summary>
    Dots,

    /// <summary>
    /// Grid of horizontal and vertical lines.
    /// </summary>
    Lines,

    /// <summary>
    /// Grid of crossing lines (+ pattern at each intersection).
    /// </summary>
    Cross
}

/// <summary>
/// A customizable background control for FlowCanvas with multiple pattern variants.
/// Inspired by React Flow's Background component.
/// </summary>
public partial class FlowBackground : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<FlowBackground, FlowCanvas?>(nameof(TargetCanvas));

    public static readonly StyledProperty<BackgroundVariant> VariantProperty =
        AvaloniaProperty.Register<FlowBackground, BackgroundVariant>(nameof(Variant), BackgroundVariant.Dots);

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<FlowBackground, double>(nameof(Gap), 20);

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<FlowBackground, double>(nameof(Size), 2);

    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<FlowBackground, Color>(nameof(Color), Colors.Gray);

    public static readonly StyledProperty<double> LineWidthProperty =
        AvaloniaProperty.Register<FlowBackground, double>(nameof(LineWidth), 1);

    #endregion

    #region Public Properties

    /// <summary>
    /// The FlowCanvas to sync viewport with.
    /// </summary>
    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    /// <summary>
    /// The background pattern variant.
    /// </summary>
    public BackgroundVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// The gap between pattern elements in canvas units.
    /// </summary>
    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    /// <summary>
    /// The size of pattern elements (dot radius for Dots, cross arm length for Cross).
    /// </summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// The color of the pattern elements.
    /// </summary>
    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// The line width for Lines and Cross variants.
    /// </summary>
    public double LineWidth
    {
        get => GetValue(LineWidthProperty);
        set => SetValue(LineWidthProperty, value);
    }

    #endregion

    private BackgroundDrawingControl? _drawingControl;

    public FlowBackground()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _drawingControl = new BackgroundDrawingControl(this);
        Content = _drawingControl;
        
        // Ensure the control fills its parent
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TargetCanvasProperty)
        {
            var oldCanvas = change.OldValue as FlowCanvas;
            var newCanvas = change.NewValue as FlowCanvas;

            if (oldCanvas != null)
            {
                oldCanvas.ViewportChanged -= OnViewportChanged;
            }

            if (newCanvas != null)
            {
                newCanvas.ViewportChanged += OnViewportChanged;
                _drawingControl?.InvalidateVisual();
            }
        }
        else if (change.Property == VariantProperty ||
                 change.Property == GapProperty ||
                 change.Property == SizeProperty ||
                 change.Property == ColorProperty ||
                 change.Property == LineWidthProperty)
        {
            // Force immediate invalidation
            InvalidateVisual();
            _drawingControl?.InvalidateVisual();
        }
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        _drawingControl?.InvalidateVisual();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _drawingControl?.InvalidateVisual();
    }
}

/// <summary>
/// Internal control that handles the actual drawing of the background pattern.
/// </summary>
internal class BackgroundDrawingControl : Control
{
    private readonly FlowBackground _parent;

    public BackgroundDrawingControl(FlowBackground parent)
    {
        _parent = parent;
        IsHitTestVisible = false;
        
        // Ensure this control fills its parent
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public override void Render(DrawingContext context)
    {
        var canvas = _parent.TargetCanvas;
        if (canvas == null)
            return;

        var viewport = canvas.Viewport;
        var bounds = Bounds;

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var gap = _parent.Gap;
        var size = _parent.Size;
        var color = _parent.Color;
        var lineWidth = _parent.LineWidth;
        var variant = _parent.Variant;
        var zoom = viewport.Zoom;
        var offsetX = viewport.OffsetX;
        var offsetY = viewport.OffsetY;

        // Calculate effective spacing in screen coordinates
        var screenGap = gap * zoom;

        // Skip rendering if pattern would be too dense
        if (screenGap < 4)
        {
            // Draw a subtle background when too zoomed out
            var fadedBrush = new SolidColorBrush(color, 0.1);
            context.FillRectangle(fadedBrush, bounds);
            return;
        }

        // Calculate visible grid range in canvas coordinates
        var startX = Math.Floor(-offsetX / zoom / gap) * gap;
        var startY = Math.Floor(-offsetY / zoom / gap) * gap;
        var endX = Math.Ceiling((bounds.Width - offsetX) / zoom / gap) * gap;
        var endY = Math.Ceiling((bounds.Height - offsetY) / zoom / gap) * gap;

        // Limit iterations for performance
        var maxIterations = 200;
        var xCount = (int)((endX - startX) / gap);
        var yCount = (int)((endY - startY) / gap);

        if (xCount > maxIterations || yCount > maxIterations)
        {
            var factor = Math.Max(xCount, yCount) / (double)maxIterations;
            gap *= Math.Ceiling(factor);
            screenGap = gap * zoom;
            startX = Math.Floor(-offsetX / zoom / gap) * gap;
            startY = Math.Floor(-offsetY / zoom / gap) * gap;
            endX = Math.Ceiling((bounds.Width - offsetX) / zoom / gap) * gap;
            endY = Math.Ceiling((bounds.Height - offsetY) / zoom / gap) * gap;
        }

        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, lineWidth * zoom);

        switch (variant)
        {
            case BackgroundVariant.Dots:
                RenderDots(context, brush, startX, startY, endX, endY, gap, size, zoom, offsetX, offsetY, bounds);
                break;

            case BackgroundVariant.Lines:
                RenderLines(context, pen, startX, startY, endX, endY, gap, zoom, offsetX, offsetY, bounds);
                break;

            case BackgroundVariant.Cross:
                RenderCross(context, pen, startX, startY, endX, endY, gap, size, zoom, offsetX, offsetY, bounds);
                break;
        }
    }

    private void RenderDots(
        DrawingContext context,
        IBrush brush,
        double startX, double startY, double endX, double endY,
        double gap, double size, double zoom, double offsetX, double offsetY,
        Rect bounds)
    {
        var dotRadius = Math.Max(size * zoom / 2, 0.5);

        for (var x = startX; x <= endX; x += gap)
        {
            for (var y = startY; y <= endY; y += gap)
            {
                var screenX = x * zoom + offsetX;
                var screenY = y * zoom + offsetY;

                if (screenX < -dotRadius || screenX > bounds.Width + dotRadius ||
                    screenY < -dotRadius || screenY > bounds.Height + dotRadius)
                    continue;

                context.DrawEllipse(brush, null, new Point(screenX, screenY), dotRadius, dotRadius);
            }
        }
    }

    private void RenderLines(
        DrawingContext context,
        Pen pen,
        double startX, double startY, double endX, double endY,
        double gap, double zoom, double offsetX, double offsetY,
        Rect bounds)
    {
        // Draw vertical lines
        for (var x = startX; x <= endX; x += gap)
        {
            var screenX = x * zoom + offsetX;
            if (screenX < 0 || screenX > bounds.Width)
                continue;

            context.DrawLine(pen, new Point(screenX, 0), new Point(screenX, bounds.Height));
        }

        // Draw horizontal lines
        for (var y = startY; y <= endY; y += gap)
        {
            var screenY = y * zoom + offsetY;
            if (screenY < 0 || screenY > bounds.Height)
                continue;

            context.DrawLine(pen, new Point(0, screenY), new Point(bounds.Width, screenY));
        }
    }

    private void RenderCross(
        DrawingContext context,
        Pen pen,
        double startX, double startY, double endX, double endY,
        double gap, double size, double zoom, double offsetX, double offsetY,
        Rect bounds)
    {
        var armLength = Math.Max(size * zoom, 2);

        for (var x = startX; x <= endX; x += gap)
        {
            for (var y = startY; y <= endY; y += gap)
            {
                var screenX = x * zoom + offsetX;
                var screenY = y * zoom + offsetY;

                if (screenX < -armLength || screenX > bounds.Width + armLength ||
                    screenY < -armLength || screenY > bounds.Height + armLength)
                    continue;

                // Draw horizontal arm of cross
                context.DrawLine(pen,
                    new Point(screenX - armLength, screenY),
                    new Point(screenX + armLength, screenY));

                // Draw vertical arm of cross
                context.DrawLine(pen,
                    new Point(screenX, screenY - armLength),
                    new Point(screenX, screenY + armLength));
            }
        }
    }
}
