using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Renderer for ellipse/circle shape elements.
/// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
/// </summary>
public class EllipseRenderer : IShapeRenderer
{
  /// <inheritdoc />
  public Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not EllipseElement ellipse)
      throw new ArgumentException($"Expected EllipseElement, got {shape.GetType().Name}");

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    var avaloniaEllipse = new Ellipse
    {
      Width = ellipse.Width ?? 50,
      Height = ellipse.Height ?? 50,
      Fill = ShapeRenderContext.CreateBrush(ellipse.Fill),
      Stroke = ShapeRenderContext.CreateBrush(ellipse.Stroke),
      StrokeThickness = ellipse.StrokeWidth,
      Opacity = ellipse.Opacity
    };

    // Apply rotation if specified
    if (ellipse.Rotation != 0)
    {
      avaloniaEllipse.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
      avaloniaEllipse.RenderTransform = new RotateTransform(ellipse.Rotation);
    }

    // Wrap in a container that can hold the label
    if (!string.IsNullOrEmpty(ellipse.Label))
    {
      var grid = new Grid();
      grid.Children.Add(avaloniaEllipse);
      grid.Children.Add(new TextBlock
      {
        Text = ellipse.Label,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        FontSize = 12,
        Foreground = Brushes.Black
      });
      return grid;
    }

    return avaloniaEllipse;
  }

  /// <inheritdoc />
  public void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not EllipseElement ellipse)
      return;

    Ellipse? avaloniaEllipse = visual as Ellipse;
    if (visual is Grid grid)
    {
      avaloniaEllipse = grid.Children.OfType<Ellipse>().FirstOrDefault();
      var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
      if (textBlock != null)
      {
        textBlock.Text = ellipse.Label ?? string.Empty;
        textBlock.FontSize = 12;
      }
    }

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    if (avaloniaEllipse != null)
    {
      avaloniaEllipse.Width = ellipse.Width ?? 50;
      avaloniaEllipse.Height = ellipse.Height ?? 50;
      avaloniaEllipse.Fill = ShapeRenderContext.CreateBrush(ellipse.Fill);
      avaloniaEllipse.Stroke = ShapeRenderContext.CreateBrush(ellipse.Stroke);
      avaloniaEllipse.StrokeThickness = ellipse.StrokeWidth;
      avaloniaEllipse.Opacity = ellipse.Opacity;

      if (ellipse.Rotation != 0)
      {
        avaloniaEllipse.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        avaloniaEllipse.RenderTransform = new RotateTransform(ellipse.Rotation);
      }
      else
      {
        avaloniaEllipse.RenderTransform = null;
      }
    }
  }

  /// <inheritdoc />
  public void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    Ellipse? avaloniaEllipse = visual as Ellipse;
    if (visual is Grid grid)
    {
      avaloniaEllipse = grid.Children.OfType<Ellipse>().FirstOrDefault();
    }

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    if (avaloniaEllipse != null)
    {
      if (shape.IsSelected)
      {
        // Add selection highlight
        avaloniaEllipse.StrokeThickness = Math.Max(shape.StrokeWidth, 2);
        avaloniaEllipse.Stroke = Brushes.DodgerBlue;
      }
      else
      {
        // Restore original stroke
        avaloniaEllipse.StrokeThickness = shape.StrokeWidth;
        avaloniaEllipse.Stroke = ShapeRenderContext.CreateBrush(shape.Stroke);
      }
    }
  }
}
