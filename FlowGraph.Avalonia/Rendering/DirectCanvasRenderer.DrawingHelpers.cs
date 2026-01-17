// DirectCanvasRenderer.DrawingHelpers.cs
// Partial class containing drawing helper methods

using Avalonia;
using Avalonia.Media;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

public partial class DirectCanvasRenderer
{
  #region Drawing Helpers

  private static StreamGeometry CreateRoundedRectGeometry(Rect rect, double radius)
  {
    var geometry = new StreamGeometry();
    using (var ctx = geometry.Open())
    {
      ctx.BeginFigure(new AvaloniaPoint(rect.Left + radius, rect.Top), true);
      ctx.LineTo(new AvaloniaPoint(rect.Right - radius, rect.Top));
      ctx.ArcTo(new AvaloniaPoint(rect.Right, rect.Top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
      ctx.LineTo(new AvaloniaPoint(rect.Right, rect.Bottom - radius));
      ctx.ArcTo(new AvaloniaPoint(rect.Right - radius, rect.Bottom), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
      ctx.LineTo(new AvaloniaPoint(rect.Left + radius, rect.Bottom));
      ctx.ArcTo(new AvaloniaPoint(rect.Left, rect.Bottom - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
      ctx.LineTo(new AvaloniaPoint(rect.Left, rect.Top + radius));
      ctx.ArcTo(new AvaloniaPoint(rect.Left + radius, rect.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
      ctx.EndFigure(true);
    }
    return geometry;
  }

  private void DrawCenteredText(DrawingContext context, string text, Rect bounds, double fontSize, IBrush brush)
  {
    var formattedText = new FormattedText(
        text,
        System.Globalization.CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        _typeface,
        fontSize,
        brush);

    var textX = bounds.X + (bounds.Width - formattedText.Width) / 2;
    var textY = bounds.Y + (bounds.Height - formattedText.Height) / 2;
    context.DrawText(formattedText, new AvaloniaPoint(textX, textY));
  }

  #endregion
}
