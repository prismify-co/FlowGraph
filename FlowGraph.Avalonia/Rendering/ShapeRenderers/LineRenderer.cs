using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Renderer for line shape elements.
/// </summary>
public class LineRenderer : IShapeRenderer
{
  /// <inheritdoc />
  public Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not LineElement line)
      throw new ArgumentException($"Expected LineElement, got {shape.GetType().Name}");

    var canvas = new Canvas
    {
      Width = Math.Abs(line.EndX - line.Position.X) * context.Scale + line.StrokeWidth * context.Scale * 2,
      Height = Math.Abs(line.EndY - line.Position.Y) * context.Scale + line.StrokeWidth * context.Scale * 2
    };

    var avaloniaLine = CreateLine(line, context);
    canvas.Children.Add(avaloniaLine);

    return canvas;
  }

  /// <inheritdoc />
  public void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not LineElement line)
      return;

    if (visual is Canvas canvas)
    {
      canvas.Children.Clear();
      var avaloniaLine = CreateLine(line, context);
      canvas.Children.Add(avaloniaLine);
      canvas.Width = Math.Abs(line.EndX - line.Position.X) * context.Scale + line.StrokeWidth * context.Scale * 2;
      canvas.Height = Math.Abs(line.EndY - line.Position.Y) * context.Scale + line.StrokeWidth * context.Scale * 2;
    }
  }

  /// <inheritdoc />
  public void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (visual is not Canvas canvas)
      return;

    var avaloniaLine = canvas.Children.OfType<Line>().FirstOrDefault();
    if (avaloniaLine != null)
    {
      if (shape.IsSelected)
      {
        avaloniaLine.StrokeThickness = Math.Max(shape.StrokeWidth, 2) * context.Scale;
        avaloniaLine.Stroke = Brushes.DodgerBlue;
      }
      else
      {
        avaloniaLine.StrokeThickness = shape.StrokeWidth * context.Scale;
        avaloniaLine.Stroke = ShapeRenderContext.CreateBrush(shape.Stroke);
      }
    }
  }

  private Line CreateLine(LineElement line, ShapeRenderContext context)
  {
    var bounds = line.GetBounds();

    // Calculate relative positions within the canvas
    var startX = (line.Position.X - bounds.X) * context.Scale + line.StrokeWidth * context.Scale;
    var startY = (line.Position.Y - bounds.Y) * context.Scale + line.StrokeWidth * context.Scale;
    var endX = (line.EndX - bounds.X) * context.Scale + line.StrokeWidth * context.Scale;
    var endY = (line.EndY - bounds.Y) * context.Scale + line.StrokeWidth * context.Scale;

    var pen = new Pen(
        ShapeRenderContext.CreateBrush(line.Stroke) ?? Brushes.Black,
        line.StrokeWidth * context.Scale)
    {
      DashStyle = ShapeRenderContext.ParseDashStyle(line.StrokeDashArray),
      LineCap = ConvertLineCap(line.EndCap)
    };

    var avaloniaLine = new Line
    {
      StartPoint = new Point(startX, startY),
      EndPoint = new Point(endX, endY),
      Stroke = pen.Brush,
      StrokeThickness = pen.Thickness,
      StrokeDashArray = pen.DashStyle?.Dashes != null
            ? new global::Avalonia.Collections.AvaloniaList<double>(pen.DashStyle.Dashes)
            : null,
      StrokeLineCap = pen.LineCap,
      Opacity = line.Opacity
    };

    return avaloniaLine;
  }

  private PenLineCap ConvertLineCap(LineCapStyle capStyle)
  {
    return capStyle switch
    {
      LineCapStyle.Flat => PenLineCap.Square,
      LineCapStyle.Round => PenLineCap.Round,
      _ => PenLineCap.Flat
    };
  }
}
