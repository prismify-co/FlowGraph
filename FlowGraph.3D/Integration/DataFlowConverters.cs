using FlowGraph.ThreeD.Abstractions;

namespace FlowGraph.ThreeD.Integration;

/// <summary>
/// Converts between FlowGraph data types and 3D scene parameters.
/// </summary>
public static class DataFlowConverters
{
  /// <summary>
  /// Converts an Avalonia Color to a SceneParameters color.
  /// </summary>
  /// <param name="color">The Avalonia color.</param>
  /// <returns>A Vector4 color for the scene.</returns>
  public static System.Numerics.Vector4 ToSceneColor(global::Avalonia.Media.Color color)
  {
    return new System.Numerics.Vector4(
        color.R / 255f,
        color.G / 255f,
        color.B / 255f,
        color.A / 255f);
  }

  /// <summary>
  /// Converts a string to a ShapeType.
  /// </summary>
  /// <param name="shapeName">The shape name.</param>
  /// <returns>The corresponding ShapeType, or Cube if not found.</returns>
  public static ShapeType ToShapeType(string? shapeName)
  {
    if (string.IsNullOrEmpty(shapeName))
      return ShapeType.Cube;

    return shapeName.ToLowerInvariant() switch
    {
      "cube" => ShapeType.Cube,
      "pyramid" => ShapeType.Pyramid,
      _ => ShapeType.Cube
    };
  }

  /// <summary>
  /// Converts a zoom percentage (0-100) to a zoom factor.
  /// </summary>
  /// <param name="percentage">The zoom percentage.</param>
  /// <returns>A zoom factor where 1.0 is normal zoom.</returns>
  public static float ToZoomFactor(double percentage)
  {
    // Map 0-100 to 0.5-2.0 zoom range
    var normalized = Math.Clamp(percentage, 0, 100) / 100.0;
    return (float)(0.5 + normalized * 1.5);
  }

  /// <summary>
  /// Creates SceneParameters from individual values (convenience method).
  /// </summary>
  /// <param name="color">Optional Avalonia color.</param>
  /// <param name="shapeType">Optional shape type string.</param>
  /// <param name="zoomPercentage">Optional zoom percentage (0-100).</param>
  /// <returns>The SceneParameters instance.</returns>
  public static SceneParameters CreateParameters(
      global::Avalonia.Media.Color? color = null,
      string? shapeType = null,
      double? zoomPercentage = null)
  {
    var builder = new SceneParametersBuilder();

    if (color.HasValue)
      builder.WithColor(color.Value);

    if (!string.IsNullOrEmpty(shapeType))
      builder.WithShapeType(shapeType);

    if (zoomPercentage.HasValue)
      builder.WithZoom(ToZoomFactor(zoomPercentage.Value));

    return builder.Build();
  }
}
