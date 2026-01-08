using System.Numerics;

namespace FlowGraph.ThreeD.Rendering;

/// <summary>
/// Lighting parameters for 3D scene rendering.
/// </summary>
public sealed class LightingParameters
{
  /// <summary>
  /// Gets or sets the light position in world space.
  /// </summary>
  public Vector3 LightPosition { get; set; } = new(2f, 4f, 3f);

  /// <summary>
  /// Gets or sets the light color.
  /// </summary>
  public Vector3 LightColor { get; set; } = Vector3.One;

  /// <summary>
  /// Gets or sets the ambient light strength (0-1).
  /// </summary>
  public float AmbientStrength { get; set; } = 0.3f;

  /// <summary>
  /// Gets or sets the specular highlight strength (0-1).
  /// </summary>
  public float SpecularStrength { get; set; } = 0.5f;

  /// <summary>
  /// Gets or sets the shininess factor for specular highlights.
  /// </summary>
  public float Shininess { get; set; } = 32f;

  /// <summary>
  /// Creates default lighting parameters suitable for most scenes.
  /// </summary>
  public static LightingParameters Default => new();
}
