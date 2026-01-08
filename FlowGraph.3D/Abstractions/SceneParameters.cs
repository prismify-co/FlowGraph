using System.Numerics;

namespace FlowGraph.ThreeD.Abstractions;

/// <summary>
/// Parameters that define how a 3D scene should be rendered.
/// Immutable record for thread-safety and easy equality comparison.
/// </summary>
public sealed record SceneParameters
{
  /// <summary>
  /// Gets the shape color as RGBA values (0-255).
  /// </summary>
  public required Vector4 Color { get; init; }

  /// <summary>
  /// Gets the type of shape to render.
  /// </summary>
  public required ShapeType ShapeType { get; init; }

  /// <summary>
  /// Gets the zoom/scale level (1.0 = default).
  /// </summary>
  public required float Zoom { get; init; }

  /// <summary>
  /// Gets the number of shapes to render (for scattered scenes).
  /// </summary>
  public int ShapeCount { get; init; } = 1;

  /// <summary>
  /// Gets whether the scene should auto-rotate.
  /// </summary>
  public bool AutoRotate { get; init; } = true;

  /// <summary>
  /// Creates default scene parameters.
  /// </summary>
  public static SceneParameters Default => new()
  {
    Color = new Vector4(1f, 0f, 0.44f, 1f), // Hot pink #ff0071
    ShapeType = ShapeType.Cube,
    Zoom = 1.0f
  };
}
