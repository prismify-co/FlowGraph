using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Rendering.OpenGL;

/// <summary>
/// Manages an OpenGL Vertex Array Object (VAO).
/// </summary>
public sealed class VertexArrayObject : IDisposable
{
  private readonly GL _gl;
  private readonly uint _handle;
  private bool _disposed;

  /// <summary>
  /// Creates a new Vertex Array Object.
  /// </summary>
  /// <param name="gl">The OpenGL context.</param>
  public VertexArrayObject(GL gl)
  {
    _gl = gl;
    _handle = _gl.GenVertexArray();
  }

  /// <summary>
  /// Binds this VAO for use.
  /// </summary>
  public void Bind()
  {
    _gl.BindVertexArray(_handle);
  }

  /// <summary>
  /// Unbinds this VAO.
  /// </summary>
  public void Unbind()
  {
    _gl.BindVertexArray(0);
  }

  /// <summary>
  /// Configures a vertex attribute pointer.
  /// </summary>
  /// <param name="index">The attribute index.</param>
  /// <param name="count">The number of components (1-4).</param>
  /// <param name="type">The data type.</param>
  /// <param name="vertexSize">The total vertex size in bytes.</param>
  /// <param name="offset">The offset within the vertex in bytes.</param>
  public unsafe void VertexAttributePointer(
      uint index,
      int count,
      VertexAttribPointerType type,
      uint vertexSize,
      int offset)
  {
    _gl.EnableVertexAttribArray(index);
    _gl.VertexAttribPointer(index, count, type, false, vertexSize, (void*)offset);
  }

  /// <inheritdoc />
  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    _gl.DeleteVertexArray(_handle);
  }
}
