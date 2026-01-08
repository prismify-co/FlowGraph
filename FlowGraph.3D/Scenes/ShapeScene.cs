using System.Numerics;
using FlowGraph.ThreeD.Abstractions;
using FlowGraph.ThreeD.Meshes;
using FlowGraph.ThreeD.Rendering.OpenGL;
using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Scenes;

/// <summary>
/// A 3D scene that renders shapes based on SceneParameters.
/// Implements IScene3D for use with the Avalonia control.
/// </summary>
public sealed class ShapeScene : IScene3D
{
  private GL? _gl;
  private ShaderProgram? _shader;
  private RenderableMesh? _cubeMesh;
  private RenderableMesh? _pyramidMesh;

  private readonly Rendering.Camera _camera = new();
  private readonly Rendering.LightingParameters _lighting = Rendering.LightingParameters.Default;

  private SceneParameters _parameters = SceneParameters.Default;
  private float _rotationAngle;
  private int _width = 1;
  private int _height = 1;
  private bool _initialized;

  /// <inheritdoc />
  public event EventHandler? InvalidateRequested;

  /// <inheritdoc />
  public void Initialize(GL gl)
  {
    if (_initialized) return;

    _gl = gl;

    // Create shader from embedded resources
    _shader = ShaderProgram.FromResources(
        gl,
        "Rendering.Shaders.basic.vert",
        "Rendering.Shaders.basic.frag");

    // Create meshes
    _cubeMesh = new RenderableMesh(gl, new CubeMesh());
    _pyramidMesh = new RenderableMesh(gl, new PyramidMesh());

    // Enable depth testing
    gl.Enable(EnableCap.DepthTest);
    gl.DepthFunc(DepthFunction.Less);

    // Enable blending for transparency
    gl.Enable(EnableCap.Blend);
    gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    // Enable backface culling
    gl.Enable(EnableCap.CullFace);
    gl.CullFace(TriangleFace.Back);

    _initialized = true;
  }

  /// <inheritdoc />
  public void Update(SceneParameters parameters, double deltaTime)
  {
    _parameters = parameters;

    // Apply zoom to camera
    _camera.ApplyZoom(parameters.Zoom);

    // Auto-rotate if enabled
    if (parameters.AutoRotate)
    {
      _rotationAngle += (float)(deltaTime * 45.0); // 45 degrees per second
      if (_rotationAngle >= 360f)
        _rotationAngle -= 360f;
    }
  }

  /// <inheritdoc />
  public void Render()
  {
    if (_gl == null || _shader == null) return;

    // Clear with transparent background
    _gl.ClearColor(0f, 0f, 0f, 0f);
    _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    // Calculate matrices
    var aspectRatio = _width / (float)Math.Max(1, _height);
    var projection = Rendering.Camera.CreateProjection(aspectRatio);
    var view = _camera.ViewMatrix;

    // Use shader and set common uniforms
    _shader.Use();
    _shader.SetUniform("uView", view);
    _shader.SetUniform("uProjection", projection);
    _shader.SetUniform("uViewPos", _camera.Position);
    _shader.SetUniform("uLightPos", _lighting.LightPosition);
    _shader.SetUniform("uLightColor", _lighting.LightColor);
    _shader.SetUniform("uAmbientStrength", _lighting.AmbientStrength);
    _shader.SetUniform("uSpecularStrength", _lighting.SpecularStrength);
    _shader.SetUniform("uShininess", _lighting.Shininess);

    // Get the correct mesh
    var mesh = _parameters.ShapeType switch
    {
      ShapeType.Cube => _cubeMesh,
      ShapeType.Pyramid => _pyramidMesh,
      _ => _cubeMesh
    };

    if (mesh == null) return;

    // Render shapes based on count
    RenderShapes(mesh, _parameters.ShapeCount, _parameters.Color);
  }

  private void RenderShapes(RenderableMesh mesh, int count, Vector4 color)
  {
    if (count == 1)
    {
      // Single centered shape
      var model = Matrix4x4.CreateRotationY(MathF.PI * _rotationAngle / 180f);
      mesh.Draw(_shader!, model, color);
    }
    else
    {
      // Arrange multiple shapes in a pattern
      var positions = GetScatteredPositions(count);
      var baseRotation = Matrix4x4.CreateRotationY(MathF.PI * _rotationAngle / 180f);

      for (int i = 0; i < Math.Min(count, positions.Length); i++)
      {
        var translation = Matrix4x4.CreateTranslation(positions[i]);
        var scale = Matrix4x4.CreateScale(0.6f); // Smaller for multiple shapes
        var model = scale * baseRotation * translation;

        // Vary colors slightly for visual interest
        var colorVariation = VaryColor(color, i);
        mesh.Draw(_shader!, model, colorVariation);
      }
    }
  }

  private static Vector3[] GetScatteredPositions(int count)
  {
    return count switch
    {
      2 => new[] { new Vector3(-0.8f, 0, 0), new Vector3(0.8f, 0, 0) },
      3 => new[] { new Vector3(-1f, 0, 0), new Vector3(0, 0, 0), new Vector3(1f, 0, 0) },
      4 => new[] { new Vector3(-0.8f, 0, -0.8f), new Vector3(0.8f, 0, -0.8f),
                         new Vector3(-0.8f, 0, 0.8f), new Vector3(0.8f, 0, 0.8f) },
      _ => Enumerable.Range(0, Math.Min(count, 9))
          .Select(i =>
          {
            var row = i / 3;
            var col = i % 3;
            return new Vector3((col - 1) * 1.2f, 0, (row - 1) * 1.2f);
          }).ToArray()
    };
  }

  private static Vector4 VaryColor(Vector4 baseColor, int index)
  {
    // Subtle brightness variation
    var factor = 0.85f + (index % 3) * 0.075f;
    return new Vector4(
        Math.Clamp(baseColor.X * factor, 0, 1),
        Math.Clamp(baseColor.Y * factor, 0, 1),
        Math.Clamp(baseColor.Z * factor, 0, 1),
        baseColor.W);
  }

  /// <inheritdoc />
  public void Resize(int width, int height)
  {
    _width = Math.Max(1, width);
    _height = Math.Max(1, height);
    _gl?.Viewport(0, 0, (uint)_width, (uint)_height);
  }

  /// <summary>
  /// Requests a redraw of the scene.
  /// </summary>
  public void RequestInvalidate()
  {
    InvalidateRequested?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Disposes OpenGL resources.
  /// </summary>
  public void Dispose()
  {
    _cubeMesh?.Dispose();
    _pyramidMesh?.Dispose();
    _shader?.Dispose();
    _initialized = false;
  }
}
