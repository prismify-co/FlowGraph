using System.Numerics;

namespace FlowGraph.ThreeD.Rendering;

/// <summary>
/// Represents a camera for 3D scene viewing.
/// Uses a perspective projection and orbits around a target point.
/// </summary>
public sealed class Camera
{
  private float _yaw = -90f;
  private float _pitch = 25f;
  private float _distance = 5f;
  private Vector3 _target = Vector3.Zero;

  /// <summary>
  /// Gets or sets the yaw angle in degrees.
  /// </summary>
  public float Yaw
  {
    get => _yaw;
    set => _yaw = value;
  }

  /// <summary>
  /// Gets or sets the pitch angle in degrees.
  /// </summary>
  public float Pitch
  {
    get => _pitch;
    set => _pitch = Math.Clamp(value, -89f, 89f);
  }

  /// <summary>
  /// Gets or sets the distance from the target.
  /// </summary>
  public float Distance
  {
    get => _distance;
    set => _distance = Math.Max(0.1f, value);
  }

  /// <summary>
  /// Gets or sets the target point the camera orbits around.
  /// </summary>
  public Vector3 Target
  {
    get => _target;
    set => _target = value;
  }

  /// <summary>
  /// Gets the camera position in world space.
  /// </summary>
  public Vector3 Position
  {
    get
    {
      var yawRad = MathF.PI * _yaw / 180f;
      var pitchRad = MathF.PI * _pitch / 180f;

      var x = MathF.Cos(pitchRad) * MathF.Cos(yawRad);
      var y = MathF.Sin(pitchRad);
      var z = MathF.Cos(pitchRad) * MathF.Sin(yawRad);

      return _target + new Vector3(x, y, z) * _distance;
    }
  }

  /// <summary>
  /// Gets the view matrix for this camera.
  /// </summary>
  public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAt(Position, _target, Vector3.UnitY);

  /// <summary>
  /// Gets a projection matrix for the specified aspect ratio.
  /// </summary>
  /// <param name="aspectRatio">The viewport aspect ratio (width / height).</param>
  /// <param name="fovDegrees">The field of view in degrees.</param>
  /// <param name="nearPlane">The near clipping plane.</param>
  /// <param name="farPlane">The far clipping plane.</param>
  /// <returns>The projection matrix.</returns>
  public static Matrix4x4 CreateProjection(
      float aspectRatio,
      float fovDegrees = 45f,
      float nearPlane = 0.1f,
      float farPlane = 100f)
  {
    var fovRad = MathF.PI * fovDegrees / 180f;
    return Matrix4x4.CreatePerspectiveFieldOfView(fovRad, aspectRatio, nearPlane, farPlane);
  }

  /// <summary>
  /// Applies zoom by adjusting the distance.
  /// </summary>
  /// <param name="zoomLevel">Zoom level where 1.0 is default, higher values zoom in.</param>
  public void ApplyZoom(float zoomLevel)
  {
    // Invert so higher zoom = closer
    Distance = 5f / Math.Max(0.1f, zoomLevel);
  }
}
