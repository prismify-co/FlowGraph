using System.Numerics;
using FlowGraph.ThreeD.Abstractions;

namespace FlowGraph.ThreeD.Integration;

/// <summary>
/// Aggregates multiple inputs into a single SceneParameters instance.
/// This allows binding from multiple FlowGraph data flow ports.
/// </summary>
public sealed class SceneParametersBuilder
{
  private Vector4 _color = new(0.4f, 0.6f, 1.0f, 1.0f);
  private ShapeType _shapeType = ShapeType.Cube;
  private float _zoom = 1.0f;
  private int _shapeCount = 1;
  private bool _autoRotate = true;

  /// <summary>
  /// Gets the current color.
  /// </summary>
  public Vector4 Color => _color;

  /// <summary>
  /// Gets the current shape type.
  /// </summary>
  public ShapeType ShapeType => _shapeType;

  /// <summary>
  /// Gets the current zoom level.
  /// </summary>
  public float Zoom => _zoom;

  /// <summary>
  /// Gets the current shape count.
  /// </summary>
  public int ShapeCount => _shapeCount;

  /// <summary>
  /// Gets whether auto-rotate is enabled.
  /// </summary>
  public bool AutoRotate => _autoRotate;

  /// <summary>
  /// Sets the color from an Avalonia color.
  /// </summary>
  /// <param name="color">The Avalonia color.</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithColor(global::Avalonia.Media.Color color)
  {
    _color = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    return this;
  }

  /// <summary>
  /// Sets the color from a System.Numerics Vector4.
  /// </summary>
  /// <param name="color">The color as RGBA Vector4.</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithColor(Vector4 color)
  {
    _color = color;
    return this;
  }

  /// <summary>
  /// Sets the shape type.
  /// </summary>
  /// <param name="shapeType">The shape type.</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithShapeType(ShapeType shapeType)
  {
    _shapeType = shapeType;
    return this;
  }

  /// <summary>
  /// Sets the shape type from a string name.
  /// </summary>
  /// <param name="shapeName">The shape name (case-insensitive).</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithShapeType(string shapeName)
  {
    if (Enum.TryParse<ShapeType>(shapeName, true, out var shapeType))
    {
      _shapeType = shapeType;
    }
    return this;
  }

  /// <summary>
  /// Sets the zoom level.
  /// </summary>
  /// <param name="zoom">The zoom level (1.0 = default).</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithZoom(float zoom)
  {
    _zoom = Math.Max(0.1f, zoom);
    return this;
  }

  /// <summary>
  /// Sets the number of shapes to display.
  /// </summary>
  /// <param name="count">The shape count (1-9).</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithShapeCount(int count)
  {
    _shapeCount = Math.Clamp(count, 1, 9);
    return this;
  }

  /// <summary>
  /// Sets whether auto-rotation is enabled.
  /// </summary>
  /// <param name="autoRotate">True to enable auto-rotation.</param>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder WithAutoRotate(bool autoRotate)
  {
    _autoRotate = autoRotate;
    return this;
  }

  /// <summary>
  /// Builds the SceneParameters instance.
  /// </summary>
  /// <returns>The immutable SceneParameters.</returns>
  public SceneParameters Build()
  {
    return new SceneParameters
    {
      Color = _color,
      ShapeType = _shapeType,
      Zoom = _zoom,
      ShapeCount = _shapeCount,
      AutoRotate = _autoRotate
    };
  }

  /// <summary>
  /// Resets the builder to default values.
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  public SceneParametersBuilder Reset()
  {
    _color = new Vector4(0.4f, 0.6f, 1.0f, 1.0f);
    _shapeType = ShapeType.Cube;
    _zoom = 1.0f;
    _shapeCount = 1;
    _autoRotate = true;
    return this;
  }
}
