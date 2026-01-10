using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Core.Elements.Shapes;

// Aliases to resolve ambiguous types
using CoreFontWeight = FlowGraph.Core.Elements.Shapes.FontWeight;
using CoreFontStyle = FlowGraph.Core.Elements.Shapes.FontStyle;
using CoreTextAlignment = FlowGraph.Core.Elements.Shapes.TextAlignment;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Renderer for text shape elements.
/// </summary>
public class TextRenderer : IShapeRenderer
{
  /// <inheritdoc />
  public Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not TextElement text)
      throw new ArgumentException($"Expected TextElement, got {shape.GetType().Name}");

    var textBlock = new TextBlock
    {
      Text = text.Text,
      FontSize = text.FontSize * context.Scale,
      FontFamily = new FontFamily(text.FontFamily),
      FontWeight = ConvertFontWeight(text.FontWeight),
      FontStyle = ConvertFontStyle(text.FontStyle),
      Foreground = ShapeRenderContext.CreateBrush(text.Fill) ?? Brushes.Black,
      TextAlignment = ConvertTextAlignment(text.TextAlignment),
      Opacity = text.Opacity,
      TextWrapping = text.MaxWidth.HasValue ? TextWrapping.Wrap : TextWrapping.NoWrap,
      MaxWidth = text.MaxWidth.HasValue ? text.MaxWidth.Value * context.Scale : double.PositiveInfinity
    };

    // Apply rotation if specified
    if (text.Rotation != 0)
    {
      textBlock.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
      textBlock.RenderTransform = new RotateTransform(text.Rotation);
    }

    return textBlock;
  }

  /// <inheritdoc />
  public void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not TextElement text)
      return;

    if (visual is TextBlock textBlock)
    {
      textBlock.Text = text.Text;
      textBlock.FontSize = text.FontSize * context.Scale;
      textBlock.FontFamily = new FontFamily(text.FontFamily);
      textBlock.FontWeight = ConvertFontWeight(text.FontWeight);
      textBlock.FontStyle = ConvertFontStyle(text.FontStyle);
      textBlock.Foreground = ShapeRenderContext.CreateBrush(text.Fill) ?? Brushes.Black;
      textBlock.TextAlignment = ConvertTextAlignment(text.TextAlignment);
      textBlock.Opacity = text.Opacity;
      textBlock.TextWrapping = text.MaxWidth.HasValue ? TextWrapping.Wrap : TextWrapping.NoWrap;
      textBlock.MaxWidth = text.MaxWidth.HasValue ? text.MaxWidth.Value * context.Scale : double.PositiveInfinity;

      if (text.Rotation != 0)
      {
        textBlock.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
        textBlock.RenderTransform = new RotateTransform(text.Rotation);
      }
      else
      {
        textBlock.RenderTransform = null;
      }
    }
  }

  /// <inheritdoc />
  public void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (visual is not TextBlock textBlock)
      return;

    if (shape.IsSelected)
    {
      // Highlight selected text with a different color
      textBlock.Foreground = Brushes.DodgerBlue;
    }
    else
    {
      // Restore original color
      textBlock.Foreground = ShapeRenderContext.CreateBrush(shape.Fill) ?? Brushes.Black;
    }
  }

  private global::Avalonia.Media.FontWeight ConvertFontWeight(CoreFontWeight weight)
  {
    return weight switch
    {
      CoreFontWeight.Thin => global::Avalonia.Media.FontWeight.Thin,
      CoreFontWeight.Light => global::Avalonia.Media.FontWeight.Light,
      CoreFontWeight.Normal => global::Avalonia.Media.FontWeight.Normal,
      CoreFontWeight.Medium => global::Avalonia.Media.FontWeight.Medium,
      CoreFontWeight.SemiBold => global::Avalonia.Media.FontWeight.SemiBold,
      CoreFontWeight.Bold => global::Avalonia.Media.FontWeight.Bold,
      CoreFontWeight.ExtraBold => global::Avalonia.Media.FontWeight.ExtraBold,
      CoreFontWeight.Black => global::Avalonia.Media.FontWeight.Black,
      _ => global::Avalonia.Media.FontWeight.Normal
    };
  }

  private global::Avalonia.Media.FontStyle ConvertFontStyle(CoreFontStyle style)
  {
    return style switch
    {
      CoreFontStyle.Italic => global::Avalonia.Media.FontStyle.Italic,
      CoreFontStyle.Oblique => global::Avalonia.Media.FontStyle.Oblique,
      _ => global::Avalonia.Media.FontStyle.Normal
    };
  }

  private global::Avalonia.Media.TextAlignment ConvertTextAlignment(CoreTextAlignment alignment)
  {
    return alignment switch
    {
      CoreTextAlignment.Left => global::Avalonia.Media.TextAlignment.Left,
      CoreTextAlignment.Center => global::Avalonia.Media.TextAlignment.Center,
      CoreTextAlignment.Right => global::Avalonia.Media.TextAlignment.Right,
      CoreTextAlignment.Justify => global::Avalonia.Media.TextAlignment.Justify,
      _ => global::Avalonia.Media.TextAlignment.Left
    };
  }
}
