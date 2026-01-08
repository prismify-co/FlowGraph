using System.Numerics;
using FlowGraph.ThreeD.Meshes;
using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Rendering.OpenGL;

/// <summary>
/// Represents a renderable mesh with OpenGL buffers.
/// </summary>
public sealed class RenderableMesh : IDisposable
{
  private readonly GL _gl;
  private readonly VertexArrayObject _vao;
  private readonly BufferObject<float> _vbo;
  private readonly BufferObject<uint> _ebo;
  private readonly int _indexCount;
  private bool _disposed;

  /// <summary>
  /// Creates a renderable mesh from mesh data.
  /// </summary>
  /// <param name="gl">The OpenGL context.</param>
  /// <param name="mesh">The mesh data.</param>
  public RenderableMesh(GL gl, IMesh mesh)
  {
    _gl = gl;
    _indexCount = mesh.IndexCount;

    _vao = new VertexArrayObject(gl);
    _vao.Bind();

    _vbo = new BufferObject<float>(gl, mesh.Vertices, BufferTargetARB.ArrayBuffer);
    _ebo = new BufferObject<uint>(gl, mesh.Indices, BufferTargetARB.ElementArrayBuffer);

    // Configure vertex attributes
    // Layout: Position (3) + Normal (3) + UV (2) = 8 floats per vertex
    var stride = (uint)(mesh.VertexSize * sizeof(float));

    // Position attribute (location = 0)
    _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, stride, 0);

    // Normal attribute (location = 1)
    _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, stride, 3 * sizeof(float));

    // UV attribute (location = 2)
    _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, stride, 6 * sizeof(float));

    _vao.Unbind();
  }

  /// <summary>
  /// Draws the mesh with the specified transformation.
  /// </summary>
  /// <param name="shader">The shader program to use.</param>
  /// <param name="modelMatrix">The model transformation matrix.</param>
  /// <param name="color">The color to render with.</param>
  public unsafe void Draw(ShaderProgram shader, Matrix4x4 modelMatrix, Vector4 color)
  {
    shader.SetUniform("uModel", modelMatrix);
    shader.SetUniform("uColor", color);

    _vao.Bind();
    _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, null);
    _vao.Unbind();
  }

  /// <inheritdoc />
  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;

    _ebo.Dispose();
    _vbo.Dispose();
    _vao.Dispose();
  }
}
