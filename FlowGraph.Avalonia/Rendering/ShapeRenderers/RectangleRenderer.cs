using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Renderer for rectangle shape elements.
/// Uses logical (unscaled) dimensions - MatrixTransform handles zoom.
/// </summary>
public class RectangleRenderer : IShapeRenderer
{
  /// <inheritdoc />
  public Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not RectangleElement rect)
      throw new ArgumentException($"Expected RectangleElement, got {shape.GetType().Name}");

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    var rectangle = new Rectangle
    {
      Width = rect.Width ?? 100,
      Height = rect.Height ?? 50,
      RadiusX = rect.CornerRadius,
      RadiusY = rect.CornerRadius,
      Fill = ShapeRenderContext.CreateBrush(rect.Fill),
      Stroke = ShapeRenderContext.CreateBrush(rect.Stroke),
      StrokeThickness = rect.StrokeWidth,
      Opacity = rect.Opacity,
    };

    // Apply rotation if specified
    if (rect.Rotation != 0)
    {
      rectangle.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
      rectangle.RenderTransform = new RotateTransform(rect.Rotation);
    }

    // Wrap in a container that can hold the label
    if (!string.IsNullOrEmpty(rect.Label))
    {
      var grid = new Grid();
      grid.Children.Add(rectangle);
      grid.Children.Add(new TextBlock
      {
        Text = rect.Label,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        FontSize = 12,
        Foreground = Brushes.Black
      });
      return grid;
    }

    return rectangle;
  }

  /// <inheritdoc />
  public void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not RectangleElement rect)
      return;

    Rectangle? rectangle = visual as Rectangle;
    if (visual is Grid grid)
    {
      rectangle = grid.Children.OfType<Rectangle>().FirstOrDefault();
      var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
      if (textBlock != null)
      {
        textBlock.Text = rect.Label ?? string.Empty;
        textBlock.FontSize = 12;
      }
    }

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    if (rectangle != null)
    {
      rectangle.Width = rect.Width ?? 100;
      rectangle.Height = rect.Height ?? 50;
      rectangle.RadiusX = rect.CornerRadius;
      rectangle.RadiusY = rect.CornerRadius;
      rectangle.Fill = ShapeRenderContext.CreateBrush(rect.Fill);
      rectangle.Stroke = ShapeRenderContext.CreateBrush(rect.Stroke);
      rectangle.StrokeThickness = rect.StrokeWidth;
      rectangle.Opacity = rect.Opacity;

      if (rect.Rotation != 0)
      {
        rectangle.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        rectangle.RenderTransform = new RotateTransform(rect.Rotation);
      }
      else
      {
        rectangle.RenderTransform = null;
      }
    }
  }

  /// <inheritdoc />
  public void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    Rectangle? rectangle = visual as Rectangle;
    if (visual is Grid grid)
    {
      rectangle = grid.Children.OfType<Rectangle>().FirstOrDefault();
    }

    // Use logical (unscaled) dimensions - MatrixTransform handles zoom
    if (rectangle != null)
    {
      if (shape.IsSelected)
      {
        // Add selection highlight
        rectangle.StrokeThickness = Math.Max(shape.StrokeWidth, 2);
        rectangle.Stroke = Brushes.DodgerBlue;
      }
      else
      {
        // Restore original stroke
        rectangle.StrokeThickness = shape.StrokeWidth;
        rectangle.Stroke = ShapeRenderContext.CreateBrush(shape.Stroke);
      }
    }
  }
}
