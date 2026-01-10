using Avalonia;
using Avalonia.Media;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Context object passed to shape renderers with information needed for rendering.
/// </summary>
public class ShapeRenderContext
{
  /// <summary>
  /// Creates a new shape render context.
  /// </summary>
  /// <param name="settings">Canvas settings for sizing and styling.</param>
  /// <param name="scale">Current zoom scale.</param>
  public ShapeRenderContext(FlowCanvasSettings settings, double scale)
  {
    Settings = settings;
    Scale = scale;
  }

  /// <summary>
  /// Gets the canvas settings.
  /// </summary>
  public FlowCanvasSettings Settings { get; }

  /// <summary>
  /// Gets the current zoom scale factor.
  /// </summary>
  public double Scale { get; }

  /// <summary>
  /// Parses a color string to an Avalonia Color.
  /// Supports hex (#RRGGBB, #AARRGGBB), named colors, or returns transparent for null/invalid.
  /// </summary>
  /// <param name="colorString">The color string to parse.</param>
  /// <returns>The parsed color or Transparent if parsing fails.</returns>
  public static Color ParseColor(string? colorString)
  {
    if (string.IsNullOrWhiteSpace(colorString))
      return Colors.Transparent;

    if (Color.TryParse(colorString, out var color))
      return color;

    return Colors.Transparent;
  }

  /// <summary>
  /// Creates a brush from a color string.
  /// Returns null for null/empty strings.
  /// </summary>
  /// <param name="colorString">The color string to convert.</param>
  /// <returns>A SolidColorBrush or null.</returns>
  public static IBrush? CreateBrush(string? colorString)
  {
    if (string.IsNullOrWhiteSpace(colorString))
      return null;

    var color = ParseColor(colorString);
    if (color == Colors.Transparent)
      return null;

    return new SolidColorBrush(color);
  }

  /// <summary>
  /// Parses a dash array string into a DashStyle.
  /// </summary>
  /// <param name="dashArray">Comma-separated dash pattern, e.g., "5,3" or "10,5,2,5".</param>
  /// <returns>A DashStyle or null for solid lines.</returns>
  public static DashStyle? ParseDashStyle(string? dashArray)
  {
    if (string.IsNullOrWhiteSpace(dashArray))
      return null;

    try
    {
      var parts = dashArray.Split(',');
      var dashes = new double[parts.Length];
      for (int i = 0; i < parts.Length; i++)
      {
        if (double.TryParse(parts[i].Trim(), out var value))
          dashes[i] = value;
        else
          return null;
      }
      return new DashStyle(dashes, 0);
    }
    catch
    {
      return null;
    }
  }
}
