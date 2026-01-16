using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FlowGraph.Core.Elements.Shapes;

// Aliases to resolve ambiguous types
using CoreFontWeight = FlowGraph.Core.Elements.Shapes.FontWeight;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Renderer for comment (sticky note) elements.
/// Creates a styled container with background, border, shadow, and text.
/// </summary>
public class CommentRenderer : IShapeRenderer
{
  private const string ShadowRectTag = "ShadowRect";
  private const string BackgroundRectTag = "BackgroundRect";
  private const string TextBlockTag = "TextBlock";

  /// <inheritdoc />
  public Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not CommentElement comment)
      throw new ArgumentException($"Expected CommentElement, got {shape.GetType().Name}");

    var container = new Canvas
    {
      Width = comment.Width ?? 200,
      Height = comment.Height ?? 100,
    };

    // Shadow rectangle (offset behind the main rect)
    if (comment.ShowShadow)
    {
      var shadowRect = new Rectangle
      {
        Tag = ShadowRectTag,
        Width = comment.Width ?? 200,
        Height = comment.Height ?? 100,
        RadiusX = comment.CornerRadius,
        RadiusY = comment.CornerRadius,
        Fill = ShapeRenderContext.CreateBrush(comment.ShadowColor) ?? new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)),
      };

      // Apply blur effect for shadow
      shadowRect.Effect = new ImmutableBlurEffect(comment.ShadowBlur);

      Canvas.SetLeft(shadowRect, comment.ShadowOffsetX);
      Canvas.SetTop(shadowRect, comment.ShadowOffsetY);
      container.Children.Add(shadowRect);
    }

    // Background rectangle
    var backgroundRect = new Rectangle
    {
      Tag = BackgroundRectTag,
      Width = comment.Width ?? 200,
      Height = comment.Height ?? 100,
      RadiusX = comment.CornerRadius,
      RadiusY = comment.CornerRadius,
      Fill = ShapeRenderContext.CreateBrush(comment.BackgroundColor),
      Stroke = ShapeRenderContext.CreateBrush(comment.BorderColor),
      StrokeThickness = comment.StrokeWidth,
      Opacity = comment.Opacity,
    };
    container.Children.Add(backgroundRect);

    // Text content
    var textBlock = new TextBlock
    {
      Tag = TextBlockTag,
      Text = comment.Text,
      FontSize = comment.FontSize,
      FontFamily = new FontFamily(comment.FontFamily),
      FontWeight = ConvertFontWeight(comment.FontWeight),
      Foreground = ShapeRenderContext.CreateBrush(comment.TextColor) ?? Brushes.Black,
      TextWrapping = TextWrapping.Wrap,
      MaxWidth = (comment.Width ?? 200) - (comment.Padding * 2),
      MaxHeight = (comment.Height ?? 100) - (comment.Padding * 2),
      TextTrimming = TextTrimming.CharacterEllipsis,
    };

    Canvas.SetLeft(textBlock, comment.Padding);
    Canvas.SetTop(textBlock, comment.Padding);
    container.Children.Add(textBlock);

    return container;
  }

  /// <inheritdoc />
  public void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (shape is not CommentElement comment || visual is not Canvas container)
      return;

    container.Width = comment.Width ?? 200;
    container.Height = comment.Height ?? 100;

    // Update shadow
    var shadowRect = FindChildByTag<Rectangle>(container, ShadowRectTag);
    if (comment.ShowShadow)
    {
      if (shadowRect == null)
      {
        // Need to add shadow
        shadowRect = new Rectangle
        {
          Tag = ShadowRectTag,
          Fill = ShapeRenderContext.CreateBrush(comment.ShadowColor) ?? new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)),
          Effect = new ImmutableBlurEffect(comment.ShadowBlur),
        };
        container.Children.Insert(0, shadowRect);
      }

      shadowRect.Width = comment.Width ?? 200;
      shadowRect.Height = comment.Height ?? 100;
      shadowRect.RadiusX = comment.CornerRadius;
      shadowRect.RadiusY = comment.CornerRadius;
      shadowRect.Fill = ShapeRenderContext.CreateBrush(comment.ShadowColor) ?? new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
      shadowRect.Effect = new ImmutableBlurEffect(comment.ShadowBlur);
      Canvas.SetLeft(shadowRect, comment.ShadowOffsetX);
      Canvas.SetTop(shadowRect, comment.ShadowOffsetY);
    }
    else if (shadowRect != null)
    {
      container.Children.Remove(shadowRect);
    }

    // Update background
    var backgroundRect = FindChildByTag<Rectangle>(container, BackgroundRectTag);
    if (backgroundRect != null)
    {
      backgroundRect.Width = comment.Width ?? 200;
      backgroundRect.Height = comment.Height ?? 100;
      backgroundRect.RadiusX = comment.CornerRadius;
      backgroundRect.RadiusY = comment.CornerRadius;
      backgroundRect.Fill = ShapeRenderContext.CreateBrush(comment.BackgroundColor);
      backgroundRect.Stroke = ShapeRenderContext.CreateBrush(comment.BorderColor);
      backgroundRect.StrokeThickness = comment.StrokeWidth;
      backgroundRect.Opacity = comment.Opacity;
    }

    // Update text
    var textBlock = FindChildByTag<TextBlock>(container, TextBlockTag);
    if (textBlock != null)
    {
      textBlock.Text = comment.Text;
      textBlock.FontSize = comment.FontSize;
      textBlock.FontFamily = new FontFamily(comment.FontFamily);
      textBlock.FontWeight = ConvertFontWeight(comment.FontWeight);
      textBlock.Foreground = ShapeRenderContext.CreateBrush(comment.TextColor) ?? Brushes.Black;
      textBlock.MaxWidth = (comment.Width ?? 200) - (comment.Padding * 2);
      textBlock.MaxHeight = (comment.Height ?? 100) - (comment.Padding * 2);
      Canvas.SetLeft(textBlock, comment.Padding);
      Canvas.SetTop(textBlock, comment.Padding);
    }
  }

  /// <inheritdoc />
  public void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context)
  {
    if (visual is not Canvas container)
      return;

    var backgroundRect = FindChildByTag<Rectangle>(container, BackgroundRectTag);
    if (backgroundRect == null)
      return;

    if (shape.IsSelected)
    {
      // Highlight with selection border
      backgroundRect.StrokeThickness = Math.Max(shape.StrokeWidth, 2);
      backgroundRect.Stroke = Brushes.DodgerBlue;
    }
    else
    {
      // Restore original border
      if (shape is CommentElement comment)
      {
        backgroundRect.StrokeThickness = comment.StrokeWidth;
        backgroundRect.Stroke = ShapeRenderContext.CreateBrush(comment.BorderColor);
      }
    }
  }

  private static T? FindChildByTag<T>(Canvas container, string tag) where T : Control
  {
    foreach (var child in container.Children)
    {
      if (child is T typed && typed.Tag is string childTag && childTag == tag)
        return typed;
    }
    return null;
  }

  private static global::Avalonia.Media.FontWeight ConvertFontWeight(CoreFontWeight weight)
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
}
