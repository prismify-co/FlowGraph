using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Renderer for line shape elements.
/// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
/// </summary>
public class LineRenderer : IShapeRenderer
{
  /// <inheritdoc />
  public Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not LineElement line)
      throw new ArgumentException($"Expected LineElement, got {shape.GetType().Name}");

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    var canvas = new Canvas
    {
      Width = Math.Abs(line.EndX - line.Position.X) + line.StrokeWidth * 2,
      Height = Math.Abs(line.EndY - line.Position.Y) + line.StrokeWidth * 2
    };

    var avaloniaLine = CreateLine(line);
    canvas.Children.Add(avaloniaLine);

    return canvas;
  }

  /// <inheritdoc />
  public void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not LineElement line)
      return;

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    if (visual is Canvas canvas)
    {
      canvas.Children.Clear();
      var avaloniaLine = CreateLine(line);
      canvas.Children.Add(avaloniaLine);
      canvas.Width = Math.Abs(line.EndX - line.Position.X) + line.StrokeWidth * 2;
      canvas.Height = Math.Abs(line.EndY - line.Position.Y) + line.StrokeWidth * 2;
    }
  }

  /// <inheritdoc />
  public void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (visual is not Canvas canvas)
      return;

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    var avaloniaLine = canvas.Children.OfType<Line>().FirstOrDefault();
    if (avaloniaLine != null)
    {
      if (shape.IsSelected)
      {
        avaloniaLine.StrokeThickness = Math.Max(shape.StrokeWidth, 2);
        avaloniaLine.Stroke = Brushes.DodgerBlue;
      }
      else
      {
        avaloniaLine.StrokeThickness = shape.StrokeWidth;
        avaloniaLine.Stroke = ShapeRenderContext.CreateBrush(shape.Stroke);
      }
    }
  }

  private Line CreateLine(LineElement line)
  {
    var bounds = line.GetBounds();

    // Calculate relative positions within the canvas (logical dimensions)
    var startX = (line.Position.X - bounds.X) + line.StrokeWidth;
    var startY = (line.Position.Y - bounds.Y) + line.StrokeWidth;
    var endX = (line.EndX - bounds.X) + line.StrokeWidth;
    var endY = (line.EndY - bounds.Y) + line.StrokeWidth;

    var pen = new Pen(
        ShapeRenderContext.CreateBrush(line.Stroke) ?? Brushes.Black,
        line.StrokeWidth)
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
