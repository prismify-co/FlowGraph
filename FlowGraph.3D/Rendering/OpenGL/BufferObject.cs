using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Rendering.OpenGL;

/// <summary>
/// Manages an OpenGL buffer object (VBO or EBO).
/// </summary>
/// <typeparam name="T">The type of data stored in the buffer.</typeparam>
public sealed class BufferObject<T> : IDisposable where T : unmanaged
{
  private readonly GL _gl;
  private readonly uint _handle;
  private readonly BufferTargetARB _bufferType;
  private bool _disposed;

  /// <summary>
  /// Creates a new buffer object with the specified data.
  /// </summary>
  /// <param name="gl">The OpenGL context.</param>
  /// <param name="data">The initial data to store.</param>
  /// <param name="bufferType">The buffer type (ArrayBuffer for VBO, ElementArrayBuffer for EBO).</param>
  public unsafe BufferObject(GL gl, ReadOnlySpan<T> data, BufferTargetARB bufferType)
  {
    _gl = gl;
    _bufferType = bufferType;
    _handle = _gl.GenBuffer();

    Bind();
    fixed (void* ptr = data)
    {
      _gl.BufferData(_bufferType, (nuint)(data.Length * sizeof(T)), ptr, BufferUsageARB.StaticDraw);
    }
  }

  /// <summary>
  /// Binds this buffer to its target.
  /// </summary>
  public void Bind()
  {
    _gl.BindBuffer(_bufferType, _handle);
  }

  /// <summary>
  /// Unbinds this buffer type.
  /// </summary>
  public void Unbind()
  {
    _gl.BindBuffer(_bufferType, 0);
  }

  /// <inheritdoc />
  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    _gl.DeleteBuffer(_handle);
  }
}
