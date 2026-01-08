using System.Numerics;
using System.Reflection;
using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Rendering.OpenGL;

/// <summary>
/// Manages an OpenGL shader program with vertex and fragment shaders.
/// </summary>
public sealed class ShaderProgram : IDisposable
{
  private readonly GL _gl;
  private readonly uint _handle;
  private readonly Dictionary<string, int> _uniformLocations = new();
  private bool _disposed;

  private ShaderProgram(GL gl, uint handle)
  {
    _gl = gl;
    _handle = handle;
  }

  /// <summary>
  /// Creates a shader program from embedded resource shaders.
  /// </summary>
  /// <param name="gl">The OpenGL context.</param>
  /// <param name="vertexResourceName">The vertex shader embedded resource name.</param>
  /// <param name="fragmentResourceName">The fragment shader embedded resource name.</param>
  /// <returns>The compiled shader program.</returns>
  public static ShaderProgram FromResources(GL gl, string vertexResourceName, string fragmentResourceName)
  {
    var vertexSource = LoadEmbeddedResource(vertexResourceName);
    var fragmentSource = LoadEmbeddedResource(fragmentResourceName);
    return FromSource(gl, vertexSource, fragmentSource);
  }

  /// <summary>
  /// Creates a shader program from source code.
  /// </summary>
  /// <param name="gl">The OpenGL context.</param>
  /// <param name="vertexSource">The vertex shader source.</param>
  /// <param name="fragmentSource">The fragment shader source.</param>
  /// <returns>The compiled shader program.</returns>
  public static ShaderProgram FromSource(GL gl, string vertexSource, string fragmentSource)
  {
    var vertexShader = CompileShader(gl, ShaderType.VertexShader, vertexSource);
    var fragmentShader = CompileShader(gl, ShaderType.FragmentShader, fragmentSource);

    var handle = gl.CreateProgram();
    gl.AttachShader(handle, vertexShader);
    gl.AttachShader(handle, fragmentShader);

    // Bind attribute locations before linking (required for GLSL 120)
    gl.BindAttribLocation(handle, 0, "aPosition");
    gl.BindAttribLocation(handle, 1, "aNormal");
    gl.BindAttribLocation(handle, 2, "aTexCoord");

    gl.LinkProgram(handle);

    gl.GetProgram(handle, GLEnum.LinkStatus, out var status);
    if (status == 0)
    {
      var infoLog = gl.GetProgramInfoLog(handle);
      gl.DeleteProgram(handle);
      gl.DeleteShader(vertexShader);
      gl.DeleteShader(fragmentShader);
      throw new InvalidOperationException($"Failed to link shader program: {infoLog}");
    }

    gl.DetachShader(handle, vertexShader);
    gl.DetachShader(handle, fragmentShader);
    gl.DeleteShader(vertexShader);
    gl.DeleteShader(fragmentShader);

    return new ShaderProgram(gl, handle);
  }

  private static uint CompileShader(GL gl, ShaderType type, string source)
  {
    var shader = gl.CreateShader(type);
    gl.ShaderSource(shader, source);
    gl.CompileShader(shader);

    gl.GetShader(shader, GLEnum.CompileStatus, out var status);
    if (status == 0)
    {
      var infoLog = gl.GetShaderInfoLog(shader);
      gl.DeleteShader(shader);
      throw new InvalidOperationException($"Failed to compile {type}: {infoLog}");
    }

    return shader;
  }

  private static string LoadEmbeddedResource(string resourceName)
  {
    var assembly = Assembly.GetExecutingAssembly();
    // Note: The assembly name is FlowGraph.3D but the namespace is FlowGraph.ThreeD
    // Embedded resources use the assembly name as prefix
    var fullName = $"FlowGraph.3D.{resourceName.Replace('/', '.')}";

    using var stream = assembly.GetManifestResourceStream(fullName);
    if (stream == null)
    {
      // List available resources for debugging
      var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
      throw new InvalidOperationException(
        $"Embedded resource not found: {fullName}. Available: {availableResources}");
    }

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }

  /// <summary>
  /// Activates this shader program for use.
  /// </summary>
  public void Use()
  {
    _gl.UseProgram(_handle);
  }

  /// <summary>
  /// Gets the location of a uniform variable, caching the result.
  /// </summary>
  private int GetUniformLocation(string name)
  {
    if (_uniformLocations.TryGetValue(name, out var location))
      return location;

    location = _gl.GetUniformLocation(_handle, name);
    _uniformLocations[name] = location;
    return location;
  }

  /// <summary>
  /// Sets a float uniform value.
  /// </summary>
  public void SetUniform(string name, float value)
  {
    var location = GetUniformLocation(name);
    if (location >= 0)
      _gl.Uniform1(location, value);
  }

  /// <summary>
  /// Sets a Vector3 uniform value.
  /// </summary>
  public void SetUniform(string name, Vector3 value)
  {
    var location = GetUniformLocation(name);
    if (location >= 0)
      _gl.Uniform3(location, value.X, value.Y, value.Z);
  }

  /// <summary>
  /// Sets a Vector4 uniform value.
  /// </summary>
  public void SetUniform(string name, Vector4 value)
  {
    var location = GetUniformLocation(name);
    if (location >= 0)
      _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
  }

  /// <summary>
  /// Sets a Matrix4x4 uniform value.
  /// </summary>
  public unsafe void SetUniform(string name, Matrix4x4 value)
  {
    var location = GetUniformLocation(name);
    if (location >= 0)
    {
      var matrix = value;
      _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
    }
  }

  /// <inheritdoc />
  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    _gl.DeleteProgram(_handle);
  }
}
